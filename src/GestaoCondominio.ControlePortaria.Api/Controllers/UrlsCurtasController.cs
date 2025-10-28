using GestaoCondominio.ControlePortaria.Api.DTOs;
using GestaoCondominio.ControlePortaria.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestaoCondominio.ControlePortaria.Api.Controllers;

[ApiController]
[Route("api/urls-encurtadas")]
public class UrlsCurtasController : ControllerBase
{
    private readonly IUrlCurtaService _service;

    public UrlsCurtasController(IUrlCurtaService service)
    {
        _service = service;
    }

    // POST api/urls-encurtadas
    [HttpPost]
    [ProducesResponseType(typeof(CriarUrlCurtaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Criar(
        [FromBody] CriarUrlCurtaRequest req,
        CancellationToken ct)
    {
        if (req is null)
            return BadRequest(new ProblemDetails { Title = "Corpo da requisição é obrigatório." });

        var baseUrl = $"{Request.Scheme}://{Request.Host}/formulario-visitante";

        var (sucesso, erro, urlCurta) = await _service.CriarAsync(req, baseUrl, ct);

        if (!sucesso)
            return BadRequest(new ProblemDetails { Title = erro });

        var urlCompleta = $"{baseUrl}/{urlCurta!.Id}";

        var response = new CriarUrlCurtaResponse
        {
            Id = urlCurta.Id,
            UrlCurta = $"/formulario-visitante/{urlCurta.Id}",
            UrlCompleta = urlCompleta,
            CriadoEm = urlCurta.CriadoEm,
            ExpiracaoEm = urlCurta.ExpiracaoEm
        };

        return CreatedAtAction(nameof(Recuperar), new { id = urlCurta.Id }, response);
    }

    // GET api/urls-encurtadas/{id}
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(RecuperarUrlCurtaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public async Task<IActionResult> Recuperar([FromRoute] string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(new ProblemDetails { Title = "ID é obrigatório." });

        var (sucesso, erro, dados) = await _service.RecuperarAsync(id, ct);

        if (!sucesso)
        {
            if (erro == "URL encurtada expirou.")
                return StatusCode(410, new ProblemDetails { Title = erro }); // 410 Gone

            if (erro == "URL encurtada não encontrada.")
                return NotFound(new ProblemDetails { Title = erro });

            return BadRequest(new ProblemDetails { Title = erro });
        }

        return Ok(dados);
    }

    // DELETE api/urls-encurtadas/{id}
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Deletar([FromRoute] string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(new ProblemDetails { Title = "ID é obrigatório." });

        var (sucesso, erro) = await _service.DeletarAsync(id, ct);

        if (!sucesso)
        {
            if (erro == "URL encurtada não encontrada.")
                return NotFound();

            return StatusCode(500, new ProblemDetails { Title = erro });
        }

        return NoContent();
    }
}