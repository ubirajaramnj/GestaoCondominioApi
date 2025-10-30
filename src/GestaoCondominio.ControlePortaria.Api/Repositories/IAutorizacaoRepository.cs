using GestaoCondominio.ControlePortaria.Api.Model;

namespace GestaoCondominio.ControlePortaria.Api.Repositories;

public interface IAutorizacaoRepository
{
    Task<AutorizacaoDeAcesso> AddAsync(AutorizacaoDeAcesso entity, CancellationToken ct);
    Task<AutorizacaoDeAcesso?> GetAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<AutorizacaoDeAcesso>> QueryAsync(string? condominioId, CancellationToken ct);
    Task<IReadOnlyList<AutorizacaoDeAcesso>> QueryAsync(string? condominioId, DateOnly? dataInicio, CancellationToken ct);
    Task<IReadOnlyList<AutorizacaoDeAcesso>> QueryAsync(string? condominioId, string? status, CancellationToken ct);
    Task<IReadOnlyList<AutorizacaoDeAcesso>> QueryAsync(string? condominioId, string? unidadeCodigo, string? status, CancellationToken ct);
    
    Task UpdateAsync(AutorizacaoDeAcesso entity, CancellationToken ct);
}
