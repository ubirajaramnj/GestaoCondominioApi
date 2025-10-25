namespace GestaoCondominio.ControlePortaria.Api.Services;
using global::GestaoCondominio.ControlePortaria.Api.Model;

public interface IComprovanteService
{
    Task<(bool Sucesso, string? Erro, ArquivoDeComprovante? Comprovante)> UploadAsync(
        Guid autorizacaoId,
        IFormFile arquivo,
        CancellationToken ct);

    Task<ArquivoDeComprovante?> ObterAsync(Guid documentoId, CancellationToken ct);

    Task<IReadOnlyList<ArquivoDeComprovante>> ListarPorAutorizacaoAsync(Guid autorizacaoId, CancellationToken ct);

    Task<(bool Sucesso, byte[]? Conteudo, string? ContentType, string? Erro)> BaixarAsync(
        Guid comprovanteId,
        CancellationToken ct);

    Task<(bool Sucesso, string? Erro)> DeletarAsync(Guid comprovanteId, CancellationToken ct);
}