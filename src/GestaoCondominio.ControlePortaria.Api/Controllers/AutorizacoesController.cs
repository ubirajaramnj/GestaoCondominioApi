using FluentValidation;
using GestaoCondominio.ControlePortaria.Api.DTOs;
using GestaoCondominio.ControlePortaria.Api.Model;
using GestaoCondominio.ControlePortaria.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestaoCondominio.ControlePortaria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AutorizacoesController : ControllerBase
{
    private readonly IAutorizacaoService _service;
    private readonly IValidator<CreateAutorizacaoRequest> _validator;

    public AutorizacoesController(IAutorizacaoService service) //, IValidator<CreateAutorizacaoRequest> validator
    {
        _service = service;
        //_validator = validator;
    }

    // POST api/v2/autorizacoes
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    //[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar([FromBody] CreateAutorizacaoRequest req, CancellationToken ct)
    {
        //var result = await _validator.ValidateAsync(req, ct);
        // With the following code:
        //if (!result.IsValid)
        //{
        //    var validationProblemDetails = new ValidationProblemDetails(result.ToDictionary())
        //    {
        //        Title = "One or more validation errors occurred.",
        //        Status = StatusCodes.Status400BadRequest
        //    };
        //    return BadRequest(validationProblemDetails);
        //}

        var usuarioId = User?.Identity?.Name ?? "MORADOR:dummy";
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString()
                       ?? Request.Headers["X-Forwarded-For"].FirstOrDefault();

        AutorizacaoDeAcesso entity = await _service.CriarAsync(req, usuarioId, clientIp, ct);

        // Link absoluto para o recurso criado(usa o nome da rota do GET)
        var link = Url.Link(nameof(ObterPorId), new { id = entity.Id });

        return CreatedAtAction(nameof(ObterPorId), new { id = entity.Id }, new
        {
            entity.Id,
            entity.CodigoAcesso,
            entity.Status,
            Link = link
        });
    }

    // GET api/v2/autorizacoes/{id}
    [HttpGet("{id:guid}", Name = nameof(ObterPorId))]
    [ProducesResponseType(typeof(AutorizacaoDeAcesso), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId([FromRoute] Guid id, CancellationToken ct)
    {
        var a = await _service.ObterAsync(id, ct);
        if (a is null) return NotFound();
        return Ok(a);
    }

    // GET api/v2/autorizacoes?condominioId=...&codigoUnidade=...&status=...
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AutorizacaoDeAcesso>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] string? condominioId, [FromQuery] string? codigoUnidade, [FromQuery] string? status, CancellationToken ct)
    {
        var list = await _service.ListarAsync(condominioId, codigoUnidade, status, ct);
        return Ok(list);
    }

    // POST api/v2/autorizacoes/{id}/cancelar
    [HttpPost("{id:guid}/cancelar")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancelar([FromRoute] Guid id, CancellationToken ct)
    {
        var usuarioId = User?.Identity?.Name ?? "MORADOR:dummy";
        var (ok, erro) = await _service.CancelarAsync(id, usuarioId, ct);
        if (!ok && erro == "Autorização não encontrada.") return NotFound();
        if (!ok) return BadRequest(new ProblemDetails { Title = erro });
        return Ok(new { Id = id, Status = "Cancelado" });
    }

    // POST api/v2/autorizacoes/{id}/checkin
    [HttpPost("{id:guid}/checkin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckIn([FromRoute] Guid id, CancellationToken ct)
    {
        var usuarioId = "PORTARIA:dummy"; // substituir por claims de role Portaria
        var (ok, erro) = await _service.CheckInAsync(id, usuarioId, ct);
        if (!ok && erro == "Autorização não encontrada.") return NotFound();
        if (!ok) return BadRequest(new ProblemDetails { Title = erro });
        return Ok(new { Id = id, Status = "Utilizado" });
    }

    // POST api/v2/autorizacoes/{id}/checkout
    [HttpPost("{id:guid}/checkout")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckOut([FromRoute] Guid id, CancellationToken ct)
    {
        var usuarioId = "PORTARIA:dummy"; // substituir por claims de role Portaria
        var (ok, erro) = await _service.CheckOutAsync(id, usuarioId, ct);
        if (!ok && erro == "Autorização não encontrada.") return NotFound();
        if (!ok) return BadRequest(new ProblemDetails { Title = erro });
        return Ok(new { Id = id, Status = "Finalizado" });
    }

    // POST api/v2/autorizacoes/validar-codigo
    public sealed record ValidarCodigoRequest(string Codigo);

    [HttpPost("validar-codigo")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidarCodigo([FromBody] ValidarCodigoRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Codigo))
            return BadRequest(new ProblemDetails { Title = "Código é obrigatório." });

        var (valido, erro, a) = await _service.ValidarCodigoAsync(body.Codigo, ct);
        if (!valido) return BadRequest(new ProblemDetails { Title = erro });

        return Ok(new
        {
            AutorizacaoId = a!.Id,
            a.Nome,
            a.Tipo,
            a.Autorizador.CodigoDaUnidade,
            a.DataInicio,
            a.DataFim,
            a.Status
        });
    }
}