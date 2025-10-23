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
        
        // ===== NOVO: Métodos de Check-in/Check-out =====
        Task<(bool Sucesso, string? Erro, CheckInRegistro? CheckIn)> CheckInAsync(
            Guid autorizacaoId,
            Guid? documentoId,
            string usuarioPortariaId,
            string? observacoes,
            CancellationToken ct);

        Task<(bool Sucesso, string? Erro, CheckOutRegistro? CheckOut)> CheckOutAsync(
            Guid autorizacaoId,
            string usuarioPortariaId,
            string? observacoes,
            CancellationToken ct);

        Task<(bool Valido, string? Erro, AutorizacaoDeAcesso? Autorizacao)> ValidarCodigoAsync(string codigo, CancellationToken ct);
    }
}