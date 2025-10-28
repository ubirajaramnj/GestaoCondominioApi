namespace GestaoCondominio.ControlePortaria.Api.Services;

using GestaoCondominio.ControlePortaria.Api.Model;
using global::GestaoCondominio.ControlePortaria.Api.DTOs;

public interface IUrlCurtaService
{
    /// <summary>
    /// Cria uma URL Curta com os parâmetros fornecidos.
    /// </summary>
    Task<(bool Sucesso, string? Erro, UrlCurta? UrlCurta)> CriarAsync(
        CriarUrlCurtaRequest req,
        string baseUrl,
        CancellationToken ct);

    /// <summary>
    /// Recupera os dados da URL Curta pelo ID.
    /// </summary>
    Task<(bool Sucesso, string? Erro, RecuperarUrlCurtaResponse? Dados)> RecuperarAsync(
        string id,
        CancellationToken ct);

    /// <summary>
    /// Deleta uma URL Curta.
    /// </summary>
    Task<(bool Sucesso, string? Erro)> DeletarAsync(string id, CancellationToken ct);
}