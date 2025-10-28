using GestaoCondominio.ControlePortaria.Api.DTOs;
using GestaoCondominio.ControlePortaria.Api.Model;
using GestaoCondominio.ControlePortaria.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestaoCondominio.ControlePortaria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComprovantesController : ControllerBase
{
    private readonly IComprovanteService _service;
    private readonly IAutorizacaoService _autorizacaoService;
    private readonly IMensageriaService _mensageriaService;
    private readonly ILogger<ComprovantesController> _logger;

    public ComprovantesController(IComprovanteService service, 
        IAutorizacaoService autorizacaoService, 
        IMensageriaService mensageriaService,
        ILogger<ComprovantesController> logger)
    {
        _service = service;
        _autorizacaoService = autorizacaoService;
        _mensageriaService = mensageriaService;
        _logger = logger;
    }

    // POST api/comprovantes/upload
    [HttpPost("upload")]
    [ProducesResponseType(typeof(UploadComprovanteRequest), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Upload(
        [FromForm] UploadComprovanteRequest uploadComprovanteRequest,
        CancellationToken ct)
    {
        // Validações básicas
        if (uploadComprovanteRequest.AutorizacaoId == Guid.Empty)
            return BadRequest(new ProblemDetails { Title = "autorizacaoId é obrigatório e deve ser um GUID válido." });

        if (uploadComprovanteRequest.Arquivo is null)
            return BadRequest(new ProblemDetails { Title = "arquivo é obrigatório." });

        // Fazer upload
        var (sucesso, erro, comprovante) = 
            await _service.UploadAsync(uploadComprovanteRequest.AutorizacaoId,
                uploadComprovanteRequest.Arquivo, ct);

        if (!sucesso)
            return BadRequest(new ProblemDetails { Title = erro });

        // Gerar link para download
        var link = Url.Action(nameof(Download), "Comprovantes", new { id = comprovante!.AutorizacaoId }, Request.Scheme, Request.Host.Value);

        var response = new UploadComprovanteResponse
        {
            AutorizacaoId = comprovante.AutorizacaoId,
            Link = link ?? $"/api/comprovantes/{comprovante.AutorizacaoId}/download"
        };

        // Obter autorização para envio
        var autorizacao = await _autorizacaoService.ObterAsync(uploadComprovanteRequest.AutorizacaoId, ct);
        if (autorizacao != null)
            //Envio da autorização
            await EnviarComprovanteAsync(autorizacao, link ?? response.Link, ct);

        return CreatedAtAction(nameof(Download), new { id = comprovante.AutorizacaoId }, response);
    }

    // GET api/comprovantes/{id}/download
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Download([FromRoute] Guid id, CancellationToken ct)
    {
        var (sucesso, conteudo, contentType, erro) = await _service.BaixarAsync(id, ct);

        if (!sucesso)
        {
            if (erro == "Comprovante não encontrado.")
                return NotFound(new ProblemDetails { Title = erro });

            return StatusCode(500, new ProblemDetails { Title = erro });
        }

        var comprovante = await _service.ObterAsync(id, ct);
        var nomeArquivo = $"comprovante_{id}";

        return File(conteudo!, contentType ?? "application/octet-stream", nomeArquivo);
    }

    // GET api/comprovantes/autorizacao/{autorizacaoId}
    [HttpGet("autorizacao/{autorizacaoId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarPorAutorizacao([FromRoute] Guid autorizacaoId, CancellationToken ct)
    {
        var comprovantes = await _service.ListarPorAutorizacaoAsync(autorizacaoId, ct);

        var response = comprovantes.Select(d => new
        {
            d.Id,
            d.AutorizacaoId,
            //d.Tipo,
            d.Nome,
            d.TamanhoBytes,
            d.CriadoEm,
            Link = Url.Action(nameof(Download), "Comprovantes", new { id = d.Id }, Request.Scheme, Request.Host.Value)
        }).ToList();

        return Ok(response);
    }

    // GET api/comprovantes/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter([FromRoute] Guid id, CancellationToken ct)
    {
        var comprovante = await _service.ObterAsync(id, ct);
        if (comprovante is null)
            return NotFound();

        var response = new
        {
            comprovante.Id,
            comprovante.AutorizacaoId,
            //comprovante.Tipo,
            comprovante.Nome,
            comprovante.TamanhoBytes,
            comprovante.CriadoEm,
            Link = Url.Action(nameof(Download), "Comprovantes", new { id = comprovante.Id }, Request.Scheme, Request.Host.Value)
        };

        return Ok(response);
    }

    // DELETE api/comprovantes/{id}
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Deletar([FromRoute] Guid id, CancellationToken ct)
    {
        var (sucesso, erro) = await _service.DeletarAsync(id, ct);

        if (!sucesso)
        {
            if (erro == "Comprovantes não encontrado.")
                return NotFound();

            return StatusCode(500, new ProblemDetails { Title = erro });
        }

        return NoContent();
    }

    /// <summary>
    /// Envia o comprovante de autorização via WhatsApp (fire-and-forget).
    /// </summary>
    private async Task EnviarComprovanteAsync(AutorizacaoDeAcesso autorizacao, string linkComprovante, CancellationToken ct)
    {
        try
        {
            // Validar telefone
            if (string.IsNullOrWhiteSpace(autorizacao.Telefone))
            {
                _logger.LogWarning("Telefone não informado para autorização {AutorizacaoId}", autorizacao.Id);
                return;
            }

            // Gerar link do comprovante (ajuste conforme sua URL base)
            var caption = $@"✅ *Autorização de Acesso Aprovada* 
*Autorizador:* {autorizacao.Autorizador.Nome}
*Unidade:* {autorizacao.Autorizador.CodigoDaUnidade}

*Tipo:* {autorizacao.Tipo}
*Nome:* {autorizacao.Nome}
*Período:* {autorizacao.DataInicio:dd/MM/yyyy} a {autorizacao.DataFim:dd/MM/yyyy}
*Código de Acesso:* {autorizacao.CodigoAcesso}

Clique no link abaixo para acessar o comprovante completo:
{linkComprovante}";

            var (sucesso, messageId, erro) = await _mensageriaService.EnviarComprovanteAsync(
                autorizacao.Telefone,
                linkComprovante,
                $"{autorizacao.Id}.pdf",
                caption,
                ct);

            (sucesso, messageId, erro) = await _mensageriaService.EnviarComprovanteAsync(
                autorizacao.Autorizador.Telefone,
                linkComprovante,
                $"{autorizacao.Id}.pdf",
                caption,
                ct);

            if (sucesso)
            {
                _logger.LogInformation(
                    "Comprovante enviado com sucesso para {Telefone}. MessageId: {MessageId}",
                    autorizacao.Telefone, messageId);

                autorizacao.Logs.Add(new(
                    DateTimeOffset.UtcNow,
                    "SISTEMA",
                    "EnvioComprovante",
                    $"Comprovante enviado via WhatsApp. MessageId: {messageId}"));

                //await _repo.UpdateAsync(autorizacao, ct);
            }
            else
            {
                _logger.LogWarning(
                    "Falha ao enviar comprovante para {Telefone}. Erro: {Erro}",
                    autorizacao.Telefone, erro);

                autorizacao.Logs.Add(new(
                    DateTimeOffset.UtcNow,
                    "SISTEMA",
                    "EnvioComprovanteErro",
                    $"Falha ao enviar comprovante: {erro}"));

                //await _repo.UpdateAsync(autorizacao, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao enviar comprovante para autorização {AutorizacaoId}",
                autorizacao.Id);
        }
    }
}