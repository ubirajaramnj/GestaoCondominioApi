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
        public List<DayOfWeek>? DiasSemanaPermitidos { get; init; }
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

        public StatusAutorizacao Status { get; set; } = StatusAutorizacao.Pendente;

        // Auditoria
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CriadoPorUsuarioId { get; init; } = default!;

        public List<LogEventoAutorizacao> Logs { get; } = new();

        // Ativa automaticamente quando a data atual está dentro da vigência.
        public void AtivarSeEmVigencia(DateTimeOffset agoraUtc, TimeZoneInfo tz)
        {
            var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(agoraUtc, tz).DateTime);
            if (Status is StatusAutorizacao.Pendente &&
                hoje >= DataInicio && hoje <= DataFim)
            {
                Status = StatusAutorizacao.Autorizado;
                Logs.Add(LogEventoAutorizacao.Criacao("Sistema", "Autorização ativada automaticamente"));
            }
        }

        // Verifica se a autorização permite acesso no momento atual (considera período e recorrência).
        public bool EstaPermitidoAgora(DateTimeOffset agoraUtc, TimeZoneInfo tz)
        {
            var local = TimeZoneInfo.ConvertTime(agoraUtc, tz).DateTime;
            var hoje = DateOnly.FromDateTime(local);

            if (Periodo == PeriodoAutorizacao.Intervalo)
            {
                if (!DiasSemanaPermitidos.Contains(local.DayOfWeek)) return false;


                var horaLocal = TimeOnly.FromDateTime(local);
                return horaLocal >= JanelaHorarioInicio && horaLocal <= JanelaHorarioFim;
            }

            // Periodo = Unico (qualquer horário do(s) dia(s) de vigência)
            return true;
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
        Pendente = 0,
        Autorizado = 1,
        Utilizado = 2,
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
