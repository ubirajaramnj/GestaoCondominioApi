using GestaoCondominio.ControlePortaria.Api.DTOs;
using GestaoCondominio.ControlePortaria.Api.Model;

namespace GestaoCondominio.ControlePortaria.Api.Services
{
    public interface IAutorizacaoService
    {
        Task<AutorizacaoDeAcesso> CriarAsync(CreateAutorizacaoRequest req, string usuarioId, string? clientIp, CancellationToken ct);
        Task<AutorizacaoDeAcesso?> ObterAsync(Guid id, CancellationToken ct);
        Task<IReadOnlyList<AutorizacaoDeAcesso>> ListarAsync(string? condominioId, string? unidadeCodigo, string? status, CancellationToken ct);
        Task<(bool Sucesso, string? Erro)> CancelarAsync(Guid id, string usuarioId, CancellationToken ct);
        Task<(bool Sucesso, string? Erro)> CheckInAsync(Guid id, string usuarioId, CancellationToken ct);
        Task<(bool Sucesso, string? Erro)> CheckOutAsync(Guid id, string usuarioId, CancellationToken ct);
        Task<(bool Valido, string? Erro, AutorizacaoDeAcesso? Autorizacao)> ValidarCodigoAsync(string codigo, CancellationToken ct);
    }
}