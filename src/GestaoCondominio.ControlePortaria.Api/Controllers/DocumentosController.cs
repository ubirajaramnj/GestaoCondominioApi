using GestaoCondominio.ControlePortaria.Api.DTOs;
using GestaoCondominio.ControlePortaria.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestaoCondominio.ControlePortaria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentosController : ControllerBase
{
    private readonly IDocumentoService _service;

    public DocumentosController(IDocumentoService service)
    {
        _service = service;
    }

    // POST api/documentos/upload
    [HttpPost("upload")]
    [ProducesResponseType(typeof(UploadDocumentoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Upload(
        [FromForm] UploadDocumentoRequest uploadDocumentoRequest,
        CancellationToken ct)
    {
        // Validações básicas
        if (uploadDocumentoRequest.AutorizacaoId == Guid.Empty)
            return BadRequest(new ProblemDetails { Title = "autorizacaoId é obrigatório e deve ser um GUID válido." });

        if (string.IsNullOrWhiteSpace(uploadDocumentoRequest.TipoDocumento))
            return BadRequest(new ProblemDetails { Title = "tipoDocumento é obrigatório." });

        if (uploadDocumentoRequest.Arquivo is null)
            return BadRequest(new ProblemDetails { Title = "arquivo é obrigatório." });

        // Fazer upload
        var (sucesso, erro, documento) = 
            await _service.UploadAsync(uploadDocumentoRequest.AutorizacaoId, 
                uploadDocumentoRequest.TipoDocumento, 
                uploadDocumentoRequest.Arquivo, ct);

        if (!sucesso)
            return BadRequest(new ProblemDetails { Title = erro });

        // Gerar link para download
        var link = Url.Action(nameof(Download), "Documentos", new { id = documento!.Id }, Request.Scheme, Request.Host.Value);

        var response = new UploadDocumentoResponse
        {
            AutorizacaoId = documento.AutorizacaoId,
            DocumentoId = documento.Id,
            Link = link ?? $"/api/documentos/{documento.Id}/download"
        };

        return CreatedAtAction(nameof(Download), new { id = documento.Id }, response);
    }

    // GET api/documentos/{id}/download
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Download([FromRoute] Guid id, CancellationToken ct)
    {
        var (sucesso, conteudo, contentType, erro) = await _service.BaixarAsync(id, ct);

        if (!sucesso)
        {
            if (erro == "Documento não encontrado.")
                return NotFound(new ProblemDetails { Title = erro });

            return StatusCode(500, new ProblemDetails { Title = erro });
        }

        var documento = await _service.ObterAsync(id, ct);
        var nomeArquivo = documento?.Nome ?? $"documento_{id}";

        return File(conteudo!, contentType ?? "application/octet-stream", nomeArquivo);
    }

    // GET api/documentos/autorizacao/{autorizacaoId}
    [HttpGet("autorizacao/{autorizacaoId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarPorAutorizacao([FromRoute] Guid autorizacaoId, CancellationToken ct)
    {
        var documentos = await _service.ListarPorAutorizacaoAsync(autorizacaoId, ct);

        var response = documentos.Select(d => new
        {
            d.Id,
            d.AutorizacaoId,
            d.Tipo,
            d.Nome,
            d.TamanhoBytes,
            d.CriadoEm,
            Link = Url.Action(nameof(Download), "Documentos", new { id = d.Id }, Request.Scheme, Request.Host.Value)
        }).ToList();

        return Ok(response);
    }

    // GET api/documentos/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter([FromRoute] Guid id, CancellationToken ct)
    {
        var documento = await _service.ObterAsync(id, ct);
        if (documento is null)
            return NotFound();

        var response = new
        {
            documento.Id,
            documento.AutorizacaoId,
            documento.Tipo,
            documento.Nome,
            documento.TamanhoBytes,
            documento.CriadoEm,
            Link = Url.Action(nameof(Download), "Documentos", new { id = documento.Id }, Request.Scheme, Request.Host.Value)
        };

        return Ok(response);
    }

    // DELETE api/documentos/{id}
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Deletar([FromRoute] Guid id, CancellationToken ct)
    {
        var (sucesso, erro) = await _service.DeletarAsync(id, ct);

        if (!sucesso)
        {
            if (erro == "Documento não encontrado.")
                return NotFound();

            return StatusCode(500, new ProblemDetails { Title = erro });
        }

        return NoContent();
    }
}