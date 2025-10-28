namespace GestaoCondominio.ControlePortaria.Api.Services;

using GestaoCondominio.ControlePortaria.Api.DTOs;
using GestaoCondominio.ControlePortaria.Api.Model;
using GestaoCondominio.ControlePortaria.Api.Repositories;
using Microsoft.Extensions.Logging;

public sealed class UrlCurtaService : IUrlCurtaService
{
    private readonly IUrlCurtaRepository _repo;
    private readonly ILogger<UrlCurtaService> _logger;
    private readonly TimeSpan _duracaoPadrao = TimeSpan.FromDays(7); // 7 dias

    public UrlCurtaService(
        IUrlCurtaRepository repo,
        ILogger<UrlCurtaService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<(bool Sucesso, string? Erro, UrlCurta? UrlCurta)> CriarAsync(
        CriarUrlCurtaRequest req,
        string baseUrl,
        CancellationToken ct)
    {
        // Validações
        if (string.IsNullOrWhiteSpace(req.Nome))
            return (false, "Nome é obrigatório.", null);

        if (string.IsNullOrWhiteSpace(req.Telefone))
            return (false, "Telefone é obrigatório.", null);

        if (string.IsNullOrWhiteSpace(req.CodigoDaUnidade))
            return (false, "Código da unidade é obrigatório.", null);

        if (string.IsNullOrWhiteSpace(req.PalavraChave))
            return (false, "Palavra-chave é obrigatória.", null);

        try
        {
            // Criar URL Curta
            var urlCurta = UrlCurta.Criar(
                req.Nome,
                req.Telefone,
                req.CodigoDaUnidade,
                req.PalavraChave,
                _duracaoPadrao);

            // Persistir
            await _repo.AddAsync(urlCurta, ct);

            _logger.LogInformation(
                "URL Curta criada: {Id} para {Nome} ({Telefone})",
                urlCurta.Id, req.Nome, req.Telefone);

            return (true, null, urlCurta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar URL Curta");
            return (false, $"Erro ao criar URL Curta: {ex.Message}", null);
        }
    }

    public async Task<(bool Sucesso, string? Erro, RecuperarUrlCurtaResponse? Dados)> RecuperarAsync(
        string id,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
            return (false, "ID da URL Curta é obrigatório.", null);

        try
        {
            var urlCurta = await _repo.GetAsync(id, ct);

            if (urlCurta is null)
                return (false, "URL Curta não encontrada.", null);

            if (!urlCurta.Ativo)
                return (false, "URL Curta foi desativada.", null);

            if (urlCurta.EstaExpirada())
                return (false, "URL Curta expirou.", null);

            var resposta = new RecuperarUrlCurtaResponse
            {
                Nome = urlCurta.Nome,
                Telefone = urlCurta.Telefone,
                CodigoDaUnidade = urlCurta.CodigoDaUnidade,
                PalavraChave = urlCurta.PalavraChave,
                CriadoEm = urlCurta.CriadoEm,
                ExpiracaoEm = urlCurta.ExpiracaoEm
            };

            _logger.LogInformation("URL Curta recuperada: {Id}", id);

            return (true, null, resposta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao recuperar URL Curta {Id}", id);
            return (false, $"Erro ao recuperar URL Curta: {ex.Message}", null);
        }
    }

    public async Task<(bool Sucesso, string? Erro)> DeletarAsync(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
            return (false, "ID da URL Curta é obrigatório.");

        try
        {
            var deletado = await _repo.DeleteAsync(id, ct);

            if (!deletado)
                return (false, "URL Curta não encontrada.");

            _logger.LogInformation("URL Curta deletada: {Id}", id);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar URL Curta {Id}", id);
            return (false, $"Erro ao deletar URL Curta: {ex.Message}");
        }
    }
}