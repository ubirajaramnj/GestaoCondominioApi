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

    public AutorizacoesController(IAutorizacaoService service)
    {
        _service = service;
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
            entity.Nome,
            entity.Tipo,
            entity.Cpf,
            entity.Rg,
            entity.Periodo,
            entity.Empresa,
            entity.Cnpj,
            entity.DataInicio,
            entity.DataFim,
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

    // ===== NOVO: Check-in com documento obrigatório na primeira entrada =====
    [HttpPost("{id:guid}/checkin")]
    [ProducesResponseType(typeof(CheckInResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckIn(
        [FromRoute] Guid id,
        [FromBody] CheckInRequest req,
        CancellationToken ct)
    {
        if (req is null)
            return BadRequest(new ProblemDetails { Title = "Corpo da requisição é obrigatório." });

        var usuarioPortariaId = "PORTARIA:dummy"; // substituir por claims/role Portaria

        var (ok, erro, checkIn) = await _service.CheckInAsync(
            id,
            req.DocumentoId,
            usuarioPortariaId,
            req.Observacoes,
            ct);

        if (!ok && erro == "Autorização não encontrada.") return NotFound();
        if (!ok) return BadRequest(new ProblemDetails { Title = erro });

        var response = new CheckInResponse
        {
            AutorizacaoId = id,
            CheckInId = checkIn!.CheckInId,
            DataHora = checkIn.DataHora,
            DocumentoId = checkIn.DocumentoId,
            Mensagem = checkIn.DocumentoId is not null
                ? "Check-in registrado com documento."
                : "Check-in registrado. Documento não obrigatório (entrada subsequente)."
        };

        return Ok(response);
    }

    // ===== NOVO: Check-out =====
    [HttpPost("{id:guid}/checkout")]
    [ProducesResponseType(typeof(CheckOutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckOut(
        [FromRoute] Guid id,
        [FromBody] CheckOutRequest req,
        CancellationToken ct)
    {
        var usuarioPortariaId = "PORTARIA:dummy"; // substituir por claims/role Portaria

        var (ok, erro, checkOut) = await _service.CheckOutAsync(
            id,
            usuarioPortariaId,
            req?.Observacoes,
            ct);

        if (!ok && erro == "Autorização não encontrada.") return NotFound();
        if (!ok) return BadRequest(new ProblemDetails { Title = erro });

        var response = new CheckOutResponse
        {
            AutorizacaoId = id,
            CheckOutId = checkOut!.CheckOutId,
            DataHora = checkOut.DataHora,
            Mensagem = "Check-out registrado com sucesso."
        };

        return Ok(response);
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

    private static List<string> ValidarRequisicao(CreateAutorizacaoRequest x)
    {
        var erros = new List<string>();

        if (x is null)
        {
            erros.Add("Payload ausente.");
            return erros;
        }

        if (string.IsNullOrWhiteSpace(x.CondominioId)) erros.Add("condominioId é obrigatório.");
        if (string.IsNullOrWhiteSpace(x.Tipo)) erros.Add("tipo é obrigatório ('visitante' ou 'prestador').");
        if (string.IsNullOrWhiteSpace(x.Nome)) erros.Add("nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(x.Telefone)) erros.Add("telefone é obrigatório.");
        if (string.IsNullOrWhiteSpace(x.Periodo)) erros.Add("periodo é obrigatório ('unico' ou 'recorrente').");
        if (x.Autorizador is null) erros.Add("autorizador é obrigatório.");
        else
        {
            if (string.IsNullOrWhiteSpace(x.Autorizador.Nome)) erros.Add("Autorizador.nome é obrigatório.");
            if (string.IsNullOrWhiteSpace(x.Autorizador.Telefone)) erros.Add("Autorizador.telefone é obrigatório.");
            if (string.IsNullOrWhiteSpace(x.Autorizador.Unidade)) erros.Add("Autorizador.codigoDaUnidade é obrigatório.");
        }

        var tipoOk = x.Tipo?.Equals("visitante", StringComparison.OrdinalIgnoreCase) == true;
        if (!tipoOk) erros.Add("tipo deve ser 'visitante' ou 'prestador'.");

        var periodoOk = x.Periodo?.Equals("unico", StringComparison.OrdinalIgnoreCase) == true;
        if (!periodoOk) erros.Add("periodo deve ser 'unico' ou 'recorrente'.");

        if (x.DataInicio > x.DataFim) erros.Add("dataInicio não pode ser maior que dataFim.");

        if (x.Periodo?.Equals("recorrente", StringComparison.OrdinalIgnoreCase) == true)
        {
            erros.Add("diasSemanaPermitidos é obrigatório quando periodo = 'recorrente'.");
            erros.Add("janelaHorarioInicio e janelaHorarioFim são obrigatórios quando periodo = 'recorrente'.");
        }
        else if (x.JanelaHorarioInicio > x.JanelaHorarioFim)
            erros.Add("janela de horário inválida (inicio > fim).");

        return erros;
    }
}