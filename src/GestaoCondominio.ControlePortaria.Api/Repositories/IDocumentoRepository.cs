using GestaoCondominio.ControlePortaria.Api.Model;

namespace GestaoCondominio.ControlePortaria.Api.Repositories;

public interface IDocumentoRepository
{
    Task<ArquivoDeDocumento> AddAsync(ArquivoDeDocumento entity, CancellationToken ct);
    Task<ArquivoDeDocumento?> GetAsync(Guid documentoId, CancellationToken ct);
    Task<IReadOnlyList<ArquivoDeDocumento>> QueryByAutorizacaoAsync(Guid autorizacaoId, CancellationToken ct);
    Task<IReadOnlyList<ArquivoDeDocumento>> QueryByTipoAsync(string tipoDocumento, CancellationToken ct);
}