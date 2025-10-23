namespace GestaoCondominio.ControlePortaria.Api.Model;

public sealed class ArquivoDeDocumento
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AutorizacaoId { get; init; }
    public string Tipo { get; init; } = default!; // "identificacao", "comprovante", etc.
    public string Nome { get; init; } = default!; // nome original do arquivo
    public string CaminhoArmazenado { get; init; } = default!; // guid.ext
    public long TamanhoBytes { get; init; }
    public string ContentType { get; init; } = default!; // "application/pdf", "image/jpeg", etc.
    public DateTimeOffset CriadoEm { get; init; } = DateTimeOffset.UtcNow;
}