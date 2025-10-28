using GestaoCondominio.ControlePortaria.Api.Model;

namespace GestaoCondominio.ControlePortaria.Api.Repositories
{
    public interface IUrlCurtaRepository
    {
        Task<UrlCurta> AddAsync(UrlCurta entity, CancellationToken ct);
        Task<bool> DeleteAsync(string id, CancellationToken ct);
        Task<UrlCurta?> GetAsync(string id, CancellationToken ct);
        Task<IReadOnlyList<UrlCurta>> QueryAsync(CancellationToken ct);
    }
}