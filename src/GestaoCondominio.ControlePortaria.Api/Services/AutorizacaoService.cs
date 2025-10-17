using GestaoCondominio.ControlePortaria.Api.DTOs;
using GestaoCondominio.ControlePortaria.Api.Model;
using GestaoCondominio.ControlePortaria.Api.Repositories;

namespace GestaoCondominio.ControlePortaria.Api.Services;

public sealed class AutorizacaoService : IAutorizacaoService
{
    private readonly IAutorizacaoRepository _repo;

    public AutorizacaoService(IAutorizacaoRepository repo)
    {
        _repo = repo;
    }

    public async Task<AutorizacaoDeAcesso> CriarAsync(CreateAutorizacaoRequest req, string usuarioId, string? clientIp, CancellationToken ct)
    {
        var tipo = req.Tipo.Equals("visitante", StringComparison.OrdinalIgnoreCase)
            ? TipoAutorizacao.Visitante : TipoAutorizacao.Prestador;

        var periodo = req.Periodo.Equals("recorrente", StringComparison.OrdinalIgnoreCase)
            ? PeriodoAutorizacao.Recorrente : PeriodoAutorizacao.Unico;

        // TODO: Inferir fuso do condomínio a partir de Data/condominios.json
        var tz = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");

        var dispositivoDto = req.InformacoesDispositivo ?? new InformacoesDispositivoDto
        {
            DataHora = DateTimeOffset.UtcNow,
            Dispositivo = "unknown",
            Navegador = "unknown",
            Linguagem = "pt-BR",
            Plataforma = "unknown",
            Ip = null
        };

        var entity = new AutorizacaoDeAcesso
        {
            Id = string.IsNullOrWhiteSpace(req.Id) ? Guid.NewGuid() : Guid.Parse(req.Id),
            CondominioId = req.CondominioId,

            Tipo = tipo,
            Periodo = periodo,

            Nome = req.Nome,
            Email = req.Email,
            Telefone = req.Telefone,
            Cpf = req.Cpf,
            Rg = req.Rg,
            Empresa = req.Empresa,
            Cnpj = req.Cnpj,

            DataInicio = req.DataInicio,
            DataFim = req.DataFim.HasValue ? req.DataFim.Value : default,

            DiasSemanaPermitidos = req.DiasSemanaPermitidos?.Select(ParseDay).ToList(),
            JanelaHorarioInicio = req.JanelaHorarioInicio is null ? null : TimeOnly.FromTimeSpan(req.JanelaHorarioInicio.Value),
            JanelaHorarioFim = req.JanelaHorarioFim is null ? null : TimeOnly.FromTimeSpan(req.JanelaHorarioFim.Value),

            Veiculo = req.Veiculo is null ? null : new Veiculo(req.Veiculo.Placa, req.Veiculo.Marca, req.Veiculo.Modelo),

            Autorizador = new Autorizador(
                req.Autorizacao.Nome,
                req.Autorizacao.Telefone,
                req.Autorizacao.CodigoDaUnidade,
                req.Autorizacao.DataHora,
                req.Autorizacao.DataHoraAutorizacao
            ),

            InformacoesDispositivo = new InformacoesDispositivo(
                dispositivoDto.DataHora,
                dispositivoDto.Dispositivo,
                dispositivoDto.Navegador,
                dispositivoDto.Linguagem,
                dispositivoDto.Plataforma,
                dispositivoDto.Ip ?? clientIp ?? "N/D"
            ),

            QrCodePayload = null,
            Status = (req.Status?.Equals("autorizado", StringComparison.OrdinalIgnoreCase) ?? false)
                     ? StatusAutorizacao.Autorizado
                     : StatusAutorizacao.Pendente,
            CreatedAt = req.CreatedAt ?? DateTimeOffset.UtcNow,
            UpdatedAt = req.UpdatedAt ?? DateTimeOffset.UtcNow,
            CriadoPorUsuarioId = usuarioId
        };

        entity.AtivarSeEmVigencia(DateTimeOffset.UtcNow, tz);
        entity.Logs.Add(LogEventoAutorizacao.Criacao(usuarioId, "Autorização criada"));

        return await _repo.AddAsync(entity, ct);
    }

    public Task<AutorizacaoDeAcesso?> ObterAsync(Guid id, CancellationToken ct) => _repo.GetAsync(id, ct);

    public Task<IReadOnlyList<AutorizacaoDeAcesso>> ListarAsync(string? condominioId, string? unidadeCodigo, string? status, CancellationToken ct)
        => _repo.QueryAsync(condominioId, unidadeCodigo, status, ct);

    public async Task<(bool Sucesso, string? Erro)> CancelarAsync(Guid id, string usuarioId, CancellationToken ct)
    {
        var a = await _repo.GetAsync(id, ct);
        if (a is null) return (false, "Autorização não encontrada.");

        if (a.Status is StatusAutorizacao.Cancelado or StatusAutorizacao.Utilizado or StatusAutorizacao.Expirado)
            return (false, "Não é possível cancelar no status atual.");

        a.Status = StatusAutorizacao.Cancelado;
        a.UpdatedAt = DateTimeOffset.UtcNow;
        a.Logs.Add(new(DateTimeOffset.UtcNow, usuarioId, "Cancelamento", "Cancelada pelo solicitante/portaria"));

        await _repo.UpdateAsync(a, ct);
        return (true, null);
    }

    public async Task<(bool Sucesso, string? Erro)> CheckInAsync(Guid id, string usuarioId, CancellationToken ct)
    {
        var a = await _repo.GetAsync(id, ct);
        if (a is null) return (false, "Autorização não encontrada.");

        var tz = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");

        a.AtivarSeEmVigencia(DateTimeOffset.UtcNow, tz);
        return (false, "Autorização fora de vigência ou não permitida agora.");

        a.Status = StatusAutorizacao.Utilizado;
        a.UpdatedAt = DateTimeOffset.UtcNow;
        a.Logs.Add(new(DateTimeOffset.UtcNow, usuarioId, "CheckIn", "Entrada registrada"));
        await _repo.UpdateAsync(a, ct);
        return (true, null);
    }

    public async Task<(bool Sucesso, string? Erro)> CheckOutAsync(Guid id, string usuarioId, CancellationToken ct)
    {
        var a = await _repo.GetAsync(id, ct);
        if (a is null) return (false, "Autorização não encontrada.");

        if (a.Status != StatusAutorizacao.Utilizado)
            return (false, "Não é possível efetuar checkout sem check-in.");

        a.UpdatedAt = DateTimeOffset.UtcNow;
        a.Logs.Add(new(DateTimeOffset.UtcNow, usuarioId, "CheckOut", "Saída registrada"));
        // Opcional: não mudamos Status; ou criar um "Finalizado"
        await _repo.UpdateAsync(a, ct);
        return (true, null);
    }

    public async Task<(bool Valido, string? Erro, AutorizacaoDeAcesso? Autorizacao)> ValidarCodigoAsync(string codigo, CancellationToken ct)
    {
        var list = await _repo.QueryAsync(null, null, null, ct);
        var a = list.FirstOrDefault(x => x.CodigoAcesso.Equals(codigo, StringComparison.OrdinalIgnoreCase));
        if (a is null) return (false, "Código inválido.", null);

        var tz = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        a.AtivarSeEmVigencia(DateTimeOffset.UtcNow, tz);
        return (false, "Autorização não está válida neste momento.", a);

        return (true, null, a);
    }

    private static DayOfWeek ParseDay(string s) =>
        Enum.TryParse<DayOfWeek>(s, true, out var d) ? d : throw new ArgumentException($"Dia inválido: {s}");
}