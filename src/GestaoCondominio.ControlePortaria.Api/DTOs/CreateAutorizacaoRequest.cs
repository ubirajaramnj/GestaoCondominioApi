namespace GestaoCondominio.ControlePortaria.Api.DTOs
{
    public sealed class CreateAutorizacaoRequest
    {
        public string? Id { get; set; }               // opcional na criação
        public string CondominioId { get; set; } = default!;
        public string Tipo { get; set; } = default!;  // "visitante" | "prestador"

        // Pessoa (na raiz)
        public string Nome { get; set; } = default!;
        public string? Email { get; set; }
        public string Telefone { get; set; } = default!;
        public string Cpf { get; set; } = default!;
        public string Rg { get; set; } = default!;
        public string? Empresa { get; set; }
        public string? Cnpj { get; set; }

        public string Periodo { get; set; } = "unico"; // "unico" | "intervalo"
        public DateOnly DataInicio { get; set; }
        public DateOnly? DataFim { get; set; }

        public VeiculoDto? Veiculo { get; set; }

        public AutorizadorDto Autorizador { get; set; } = default!;

        public DispositivoDto? Dispositivo { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public string? Status { get; set; } // "autorizado", etc.

        // Recorrência (quando Periodo = "intervalo")
        public List<string>? DiasSemanaPermitidos { get; set; }
        public TimeSpan? JanelaHorarioInicio { get; set; }
        public TimeSpan? JanelaHorarioFim { get; set; }
    }

    public sealed class VeiculoDto
    {
        public string Placa { get; set; } = default!;
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
    }

    public sealed class AutorizadorDto
    {
        public string Nome { get; set; } = default!;
        public string Telefone { get; set; } = default!;
        public string Unidade { get; set; } = default!;
        public DateTimeOffset DataHora { get; set; }
        public DateTimeOffset DataHoraAutorizacao { get; set; }
    }

    public sealed class DispositivoDto
    {
        public DateTimeOffset DataHora { get; set; }
        public string Dispositivo { get; set; } = default!;
        public string Navegador { get; set; } = default!;
        public string Linguagem { get; set; } = default!;
        public string Plataforma { get; set; } = default!;
        public string? Ip { get; set; } // se não vier, backend preenche
    }
}