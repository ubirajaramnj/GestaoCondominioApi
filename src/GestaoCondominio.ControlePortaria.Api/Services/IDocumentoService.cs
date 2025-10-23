namespace GestaoCondominio.ControlePortaria.Api.Services;
using global::GestaoCondominio.ControlePortaria.Api.Model;

public interface IDocumentoService
{
    Task<(bool Sucesso, string? Erro, ArquivoDeDocumento? Documento)> UploadAsync(
        Guid autorizacaoId,
        string tipoDocumento,
        IFormFile arquivo,
        CancellationToken ct);

    Task<ArquivoDeDocumento?> ObterAsync(Guid documentoId, CancellationToken ct);

    Task<IReadOnlyList<ArquivoDeDocumento>> ListarPorAutorizacaoAsync(Guid autorizacaoId, CancellationToken ct);

    Task<(bool Sucesso, byte[]? Conteudo, string? ContentType, string? Erro)> BaixarAsync(
        Guid documentoId,
        CancellationToken ct);

    Task<(bool Sucesso, string? Erro)> DeletarAsync(Guid documentoId, CancellationToken ct);
}