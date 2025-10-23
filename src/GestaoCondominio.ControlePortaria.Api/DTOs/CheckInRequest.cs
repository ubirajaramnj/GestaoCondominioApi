namespace GestaoCondominio.ControlePortaria.Api.DTOs;
public sealed class CheckInRequest
{
    public Guid AutorizacaoId { get; set; }
    public Guid? DocumentoId { get; set; }  // Obrigatório no primeiro check-in
    public string? Observacoes { get; set; }
}

public sealed class CheckInResponse
{
    public Guid AutorizacaoId { get; set; }
    public Guid CheckInId { get; set; }
    public DateTimeOffset DataHora { get; set; }
    public Guid? DocumentoId { get; set; }
    public string Mensagem { get; set; } = default!;
}

public sealed class CheckOutRequest
{
    public Guid AutorizacaoId { get; set; }
    public string? Observacoes { get; set; }
}

public sealed class CheckOutResponse
{
    public Guid AutorizacaoId { get; set; }
    public Guid CheckOutId { get; set; }
    public DateTimeOffset DataHora { get; set; }
    public string Mensagem { get; set; } = default!;
}