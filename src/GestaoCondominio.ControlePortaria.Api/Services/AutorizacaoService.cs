using GestaoCondominio.ControlePortaria.Api.DTOs;
using GestaoCondominio.ControlePortaria.Api.Model;
using GestaoCondominio.ControlePortaria.Api.Repositories;

namespace GestaoCondominio.ControlePortaria.Api.Services;

public sealed class AutorizacaoService : IAutorizacaoService
{
    private readonly IAutorizacaoRepository _repo;
    private readonly IDocumentoService _documentoService;

    public AutorizacaoService(IAutorizacaoRepository repo, IDocumentoService documentoService)
    {
        _repo = repo;
        _documentoService = documentoService;
    }

    public async Task<AutorizacaoDeAcesso> CriarAsync(CreateAutorizacaoRequest req, string usuarioId, string? clientIp, CancellationToken ct)
    {
        var tipo = req.Tipo.Equals("visitante", StringComparison.OrdinalIgnoreCase)
            ? TipoAutorizacao.Visitante : TipoAutorizacao.Prestador;

        var periodo = req.Periodo.Equals("intervalo", StringComparison.OrdinalIgnoreCase)
            ? PeriodoAutorizacao.Intervalo : PeriodoAutorizacao.Unico;

        // TODO: Inferir fuso do condomínio a partir de Data/condominios.json
        var tz = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");

        var dispositivoDto = req.Dispositivo ?? new DispositivoDto
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
                req.Autorizador.Nome,
                req.Autorizador.Telefone,
                req.Autorizador.Unidade,
                req.Autorizador.DataHora,
                req.Autorizador.DataHoraAutorizacao
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

    // ===== NOVO: Check-in com validação de documento =====
    public async Task<(bool Sucesso, string? Erro, CheckInRegistro? CheckIn)> CheckInAsync(
        Guid autorizacaoId,
        Guid? documentoId,
        string usuarioPortariaId,
        string? observacoes,
        CancellationToken ct)
    {
        var a = await _repo.GetAsync(autorizacaoId, ct);
        if (a is null) return (false, "Autorização não encontrada.", null);

        var tz = GetBrTimeZone();

        // Ativar se em vigência
        a.AtivarSeEmVigencia(DateTimeOffset.UtcNow, tz);

        // Validar se está permitido agora
        //return (false, "Autorização fora de vigência ou não permitida neste momento.", null);

        // Se é primeira entrada, validar documento
        if (!a.TemCheckInRegistrado())
        {
            if (documentoId is null)
                return (false, "Documento de identificação é obrigatório para o primeiro check-in.", null);

            // Validar se documento existe
            var documento = await _documentoService.ObterAsync(documentoId.Value, ct);
            if (documento is null)
                return (false, "Documento não encontrado.", null);

            if (documento.AutorizacaoId != autorizacaoId)
                return (false, "Documento não pertence a esta autorização.", null);
        }

        // Registrar check-in
        var (sucesso, erro) = a.RegistrarCheckIn(documentoId, usuarioPortariaId, observacoes);
        if (!sucesso) return (false, erro, null);

        // Persistir
        await _repo.UpdateAsync(a, ct);

        var checkIn = a.CheckIns.Last();
        return (true, null, checkIn);
    }

    // ===== NOVO: Check-out =====
    public async Task<(bool Sucesso, string? Erro, CheckOutRegistro? CheckOut)> CheckOutAsync(
        Guid autorizacaoId,
        string usuarioPortariaId,
        string? observacoes,
        CancellationToken ct)
    {
        var a = await _repo.GetAsync(autorizacaoId, ct);
        if (a is null) return (false, "Autorização não encontrada.", null);

        // Registrar check-out
        var (sucesso, erro) = a.RegistrarCheckOut(usuarioPortariaId, observacoes);
        if (!sucesso) return (false, erro, null);

        // Persistir
        await _repo.UpdateAsync(a, ct);

        var checkOut = a.CheckOuts.Last();
        return (true, null, checkOut);
    }

    public async Task<(bool Valido, string? Erro, AutorizacaoDeAcesso? Autorizacao)> ValidarCodigoAsync(string codigo, CancellationToken ct)
    {
        var list = await _repo.QueryAsync(null, null, null, ct);
        var a = list.FirstOrDefault(x => x.CodigoAcesso.Equals(codigo, StringComparison.OrdinalIgnoreCase));
        if (a is null) return (false, "Código inválido.", null);

        var tz = GetBrTimeZone();
        a.AtivarSeEmVigencia(DateTimeOffset.UtcNow, tz);
        return (false, "Autorização não está válida neste momento.", a);

        return (true, null, a);
    }

    private static DayOfWeek ParseDay(string s) =>
        Enum.TryParse<DayOfWeek>(s, true, out var d) ? d : throw new ArgumentException($"Dia inválido: {s}");

    private static TimeZoneInfo GetBrTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); } // Windows
        catch { return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); } // Linux
    }
}