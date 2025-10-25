using GestaoCondominio.ControlePortaria.Api.Model;

namespace GestaoCondominio.ControlePortaria.Api.Repositories;

public interface IComprovanteRepository
{
    Task<ArquivoDeComprovante> AddAsync(ArquivoDeComprovante entity, CancellationToken ct);
    Task<ArquivoDeComprovante?> GetAsync(Guid comprovanteId, CancellationToken ct);
    Task<IReadOnlyList<ArquivoDeComprovante>> QueryByAutorizacaoAsync(Guid autorizacaoId, CancellationToken ct);
    Task<IReadOnlyList<ArquivoDeComprovante>> QueryByTipoAsync(string tipoComprovante, CancellationToken ct);
}