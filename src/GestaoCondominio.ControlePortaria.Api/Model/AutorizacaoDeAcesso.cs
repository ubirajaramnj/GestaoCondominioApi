using GestaoCondominio.ControlePortaria.Api.Infrastructure.Configuration;

namespace GestaoCondominio.ControlePortaria.Api.Model
{
    public sealed class AutorizacaoDeAcesso
    {
        // Identificação
        public Guid Id { get; init; } = Guid.NewGuid();
        public string CondominioId { get; init; } = default!;

        // Classificação
        public TipoAutorizacao Tipo { get; init; } // visitante | prestador
        public PeriodoAutorizacao Periodo { get; init; } // unico | recorrente

        // Dados do visitante/prestador (payload na raiz)
        public string Nome { get; init; } = default!;
        public string? Email { get; init; }
        public string Telefone { get; init; } = default!;
        public string? Cpf { get; init; }
        public string? Rg { get; init; }
        public string? Empresa { get; init; }
        public string? Cnpj { get; init; }

        // Vigência (payload usa apenas data, sem hora)
        public DateOnly DataInicio { get; init; }
        public DateOnly DataFim { get; init; }

        // Recorrência (válido quando Periodo = Recorrente)
        public List<DayOfWeek>? DiasSemanaPermitidos { get; set; }
        public TimeOnly? JanelaHorarioInicio { get; init; }
        public TimeOnly? JanelaHorarioFim { get; init; }

        // Veículo (opcional)
        public Veiculo? Veiculo { get; init; }

        // Detalhes de quem autorizou + unidade
        public Autorizador Autorizador { get; init; } = default!;

        // Informações do dispositivo (capturadas no backend quando possível)
        public InformacoesDispositivo InformacoesDispositivo { get; set; } = default!;

        // Operacional
        public string CodigoAcesso { get; init; } = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        public string? QrCodePayload { get; init; }

        // Backing field para status
        private StatusAutorizacao _status = StatusAutorizacao.Autorizado;  // ← ALTERAR para Autorizado
        private TimeZoneInfo? _timeZone;

        /// <summary>
        /// Status da autorização. Calcula dinamicamente baseado nas regras de negócio.
        /// </summary>
        public StatusAutorizacao Status
        {
            get => CalcularStatusAtual();
            set => _status = value;
        }

        // Auditoria
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CriadoPorUsuarioId { get; init; } = default!;

        // ===== NOVO: Histórico de Check-ins e Check-outs =====
        public List<CheckInRegistro> CheckIns { get; set; } = new();
        public List<CheckOutRegistro> CheckOuts { get; set; } = new();

        public List<LogEventoAutorizacao> Logs { get; set; } = new();

        /// <summary>
        /// Ativa automaticamente quando a data atual está dentro da vigência.
        /// Nota: Agora apenas valida se está em vigência, pois o status é calculado dinamicamente.
        /// </summary>
        public bool EstaEmVigencia(DateTimeOffset agoraUtc, TimeZoneInfo? tz = null)
        {
            var timeZone = tz ?? TimeZoneConfiguration.BrasilTimeZone;
            var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(agoraUtc, timeZone).DateTime);

            return hoje >= DataInicio && hoje <= DataFim;
        }

        // Verifica se a autorização permite acesso no momento atual (considera período e recorrência).
        public bool EstaPermitidoAgora(DateTimeOffset agoraUtc, TimeZoneInfo? tz = null)
        {
            var timeZone = tz ?? TimeZoneConfiguration.BrasilTimeZone;
            var local = TimeZoneInfo.ConvertTime(agoraUtc, timeZone).DateTime;
            var hoje = DateOnly.FromDateTime(local);

            if (Periodo == PeriodoAutorizacao.Intervalo)
            {
                var horaLocal = TimeOnly.FromDateTime(local);
                return horaLocal >= JanelaHorarioInicio && horaLocal <= JanelaHorarioFim;
            }

            return true;
        }

        // ===== NOVO: Métodos para gerenciar check-ins =====

        /// <summary>
        /// Verifica se já existe check-in registrado para esta autorização.
        /// </summary>
        public bool TemCheckInRegistrado() => CheckIns.Count > 0;

        /// <summary>
        /// Verifica se há check-in sem check-out correspondente (visitante ainda dentro).
        /// </summary>
        public bool TemCheckInAberto()
        {
            if (CheckIns.Count == 0) return false;
            var ultimoCheckIn = CheckIns.OrderByDescending(x => x.DataHora).First();
            var checkOutCorrespondente = CheckOuts.FirstOrDefault(x => x.DataHora > ultimoCheckIn.DataHora);
            return checkOutCorrespondente is null;
        }

        /// <summary>
        /// Registra um check-in. Valida se documento é obrigatório (primeira entrada).
        /// </summary>
        public (bool Sucesso, string? Erro) RegistrarCheckIn(Guid? documentoId, string usuarioPortariaId, string? observacoes = null)
        {
            // Se é a primeira entrada e período é "Unico", documento é obrigatório
            if (!TemCheckInRegistrado() && Periodo == PeriodoAutorizacao.Unico && documentoId is null)
                return (false, "Documento de identificação é obrigatório para o primeiro check-in.");

            // Se é a primeira entrada e período é "Recorrente", documento é obrigatório
            if (!TemCheckInRegistrado() && Periodo == PeriodoAutorizacao.Intervalo && documentoId is null)
                return (false, "Documento de identificação é obrigatório para o primeiro check-in.");

            // Se já tem check-in aberto, não pode fazer novo check-in sem checkout
            if (TemCheckInAberto())
                return (false, "Existe um check-in aberto. Faça o check-out antes de um novo check-in.");

            var checkIn = CheckInRegistro.Criar(documentoId, usuarioPortariaId, observacoes);
            CheckIns.Add(checkIn);

            _status = StatusAutorizacao.Utilizado;
            UpdatedAt = DateTimeOffset.UtcNow;
            Logs.Add(new(DateTimeOffset.UtcNow, usuarioPortariaId, "CheckIn", $"Check-in registrado. DocumentoId: {documentoId?.ToString() ?? "N/A"}"));

            return (true, null);
        }

        /// <summary>
        /// Registra um check-out.
        /// </summary>
        public (bool Sucesso, string? Erro) RegistrarCheckOut(string usuarioPortariaId, string? observacoes = null)
        {
            if (!TemCheckInAberto())
                return (false, "Não há check-in aberto para fazer check-out.");

            var checkOut = CheckOutRegistro.Criar(usuarioPortariaId, observacoes);
            CheckOuts.Add(checkOut);

            UpdatedAt = DateTimeOffset.UtcNow;
            Logs.Add(new(DateTimeOffset.UtcNow, usuarioPortariaId, "CheckOut", "Check-out registrado."));

            return (true, null);
        }

        /// <summary>
        /// Calcula o status atual da autorização baseado nas regras de negócio.
        /// 
        /// Regras:
        /// 1. Se Cancelado, permanece Cancelado.
        /// 2. Se dataInicio < hoje e sem CheckIn/CheckOut, é Expirado.
        /// 3. Se com CheckIn e CheckOut:
        ///    - Se dataFim <= hoje, é Finalizado.
        ///    - Se dataFim > hoje, permanece Utilizado.
        /// 4. Se Periodo=Unico, dataInicio < hoje, com CheckIn mas sem CheckOut, é Utilizado.
        /// 5. Se dataInicio <= hoje e sem CheckIn:
        ///    - Se dataFim < hoje, é Expirado.
        ///    - Caso contrário, é Autorizado.
        /// 6. Se dataInicio > hoje, é Autorizado (ainda não começou).
        /// </summary>
        private StatusAutorizacao CalcularStatusAtual()
        {
            var tz = _timeZone ?? TimeZoneConfiguration.BrasilTimeZone;
            var local = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz).DateTime;
            var hoje = DateOnly.FromDateTime(local);

            // Regra 1: Se Cancelado, permanece Cancelado
            if (_status == StatusAutorizacao.Cancelado)
                return StatusAutorizacao.Cancelado;

            // Regra 2: Se dataInicio < hoje e sem CheckIn/CheckOut, é Expirado
            if (hoje > DataInicio && !TemCheckInRegistrado() && CheckOuts.Count == 0)
                return StatusAutorizacao.Expirado;

            // Regra 3: Se com CheckIn e CheckOut
            if (TemCheckInRegistrado() && !TemCheckInAberto())
            {
                // Se dataFim <= hoje, é Finalizado
                if (hoje >= DataFim)
                    return StatusAutorizacao.Finalizado;

                // Se dataFim > hoje, permanece Utilizado
                return StatusAutorizacao.Utilizado;
            }

            // Regra 4: Se Periodo=Unico, dataInicio < hoje, com CheckIn mas sem CheckOut, é Utilizado
            if (Periodo == PeriodoAutorizacao.Unico && hoje >= DataInicio && TemCheckInRegistrado() && TemCheckInAberto())
                return StatusAutorizacao.Utilizado;

            // Regra 5: Se dataInicio <= hoje e sem CheckIn
            if (hoje >= DataInicio)
            {
                // Se passou a dataFim, é Expirado
                if (hoje > DataFim)
                    return StatusAutorizacao.Expirado;

                // Caso contrário, é Autorizado
                return StatusAutorizacao.Autorizado;
            }

            // Regra 6: Se dataInicio > hoje, é Autorizado (ainda não começou)
            return StatusAutorizacao.Autorizado;
        }
    }

    public sealed record Autorizador(
        string Nome,                 // Nome de quem autorizou (ex.: "Bira")
        string Telefone,             // Telefone do autorizador
        string CodigoDaUnidade,      // Ex.: "R01-QDJ-26"
        DateTimeOffset DataHora,     // Momento da requisição/autorização
        DateTimeOffset DataHoraAutorizacao // Momento da confirmação/efetivação
    );

    public sealed record Veiculo(string Placa, string? Marca, string? Modelo);

    public sealed record InformacoesDispositivo(
        DateTimeOffset DataHora,
        string Dispositivo,
        string Navegador,
        string Linguagem,
        string Plataforma,
        string Ip // “A ser coletado pelo backend” no fluxo de criação
    );

    public enum TipoAutorizacao
    {
        Visitante = 0,
        Prestador = 1
    }

    public enum PeriodoAutorizacao
    {
        Unico = 0,
        Intervalo = 1
    }

    public enum StatusAutorizacao
    {
        Autorizado = 0,
        Utilizado = 1,
        Finalizado = 2,
        Expirado = 3,
        Cancelado = 4
    }
    public sealed record LogEventoAutorizacao(
    DateTimeOffset Quando, string UsuarioId, string Tipo, string Mensagem)
    {
        public static LogEventoAutorizacao Criacao(string usuarioId, string mensagem) =>
            new(DateTimeOffset.UtcNow, usuarioId, "Criacao", mensagem);
    }

}
