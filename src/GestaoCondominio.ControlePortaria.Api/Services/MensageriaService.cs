namespace GestaoCondominio.ControlePortaria.Api.Services;

using System.Net.Http.Json;
using System.Text.Json;
using GestaoCondominio.ControlePortaria.Api.DTOs;
using GestaoCondominio.ControlePortaria.Api.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class MensageriaService : IMensageriaService
{
    private readonly HttpClient _httpClient;
    private readonly ConfiguracaoMensageria _config;
    private readonly ILogger<MensageriaService> _logger;

    public MensageriaService(
        HttpClient httpClient,
        IOptions<ConfiguracaoMensageria> options,
        ILogger<MensageriaService> logger)
    {
        _httpClient = httpClient;
        _config = options.Value;
        _logger = logger;
    }

    public async Task<(bool Sucesso, string? MessageId, string? Erro)> EnviarComprovanteAsync(
        string numeroTelefone,
        string urlComprovante,
        string nomeArquivo,
        string caption,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(numeroTelefone))
            return (false, null, "Número de telefone é obrigatório.");

        if (string.IsNullOrWhiteSpace(urlComprovante))
            return (false, null, "URL do comprovante é obrigatória.");

        if (string.IsNullOrWhiteSpace(nomeArquivo))
            nomeArquivo = "comprovante.pdf";

        // Normalizar número (remover caracteres especiais, manter apenas dígitos)
        var numeroNormalizado = NormalizarNumeroTelefone(numeroTelefone);
        if (string.IsNullOrWhiteSpace(numeroNormalizado))
            return (false, null, "Número de telefone inválido.");

        try
        {
            var payload = new EnviarMensagemMediaRequest
            {
                Number = numeroNormalizado,
                MediaType = "document",
                MimeType = "application/pdf",
                Caption = caption,
                Media = urlComprovante,
                FileName = nomeArquivo
            };

            var resultado = await EnviarMensagemInternalAsync(payload, ct);
            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar comprovante para {Telefone}", numeroTelefone);
            return (false, null, $"Erro ao enviar comprovante: {ex.Message}");
        }
    }

    public async Task<(bool Sucesso, string? MessageId, string? Erro)> EnviarMensagemAsync(
        string numeroTelefone,
        string mensagem,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(numeroTelefone))
            return (false, null, "Número de telefone é obrigatório.");

        if (string.IsNullOrWhiteSpace(mensagem))
            return (false, null, "Mensagem é obrigatória.");

        var numeroNormalizado = NormalizarNumeroTelefone(numeroTelefone);
        if (string.IsNullOrWhiteSpace(numeroNormalizado))
            return (false, null, "Número de telefone inválido.");

        try
        {
            // Para mensagens de texto, usamos um endpoint diferente ou o mesmo com tipo "text"
            // Aqui vou usar um payload simplificado
            var url = $"{_config.BaseUrl}/message/sendMedia/{_config.Instance}";

            var payload = new { number = numeroNormalizado, text = mensagem };

            // ← ALTERE ESTA LINHA: de PostAsJsonAsync para PostAsJsonCamelCaseAsync
            var response = await _httpClient.PostAsJsonCamelCaseAsync(url, payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                var erro = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Erro ao enviar mensagem para {Telefone}: {StatusCode} - {Erro}",
                    numeroTelefone, response.StatusCode, erro);
                return (false, null, $"Erro na API: {response.StatusCode}");
            }

            // With this updated code:
            var conteudo = await response.Content.ReadAsStringAsync(ct);
            var jsonDoc = JsonDocument.Parse(conteudo);
            var messageId = jsonDoc.RootElement.TryGetProperty("messageId", out var prop)
                ? prop.GetString()
                : jsonDoc.RootElement.TryGetProperty("id", out var prop2)
                    ? prop2.GetString()
                    : null;

            _logger.LogInformation($"Mensagem enviada com sucesso para {numeroTelefone}. MessageId: {messageId}");

            return (true, messageId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar mensagem para {Telefone}", numeroTelefone);
            return (false, null, $"Erro ao enviar mensagem: {ex.Message}");
        }
    }

    // --------- Helpers ---------

    private async Task<(bool Sucesso, string? MessageId, string? Erro)> EnviarMensagemInternalAsync(
        EnviarMensagemMediaRequest payload,
        CancellationToken ct)
    {
        var url = $"{_config.BaseUrl}/message/sendMedia/{_config.Instance}";
        _logger.LogInformation("Enviando mídia para {Numero} via {Url}", payload.Number, url);

        var payloadAsJson = JsonSerializer.Serialize(payload);
        _logger.LogCritical("Payload de envio de mídia: {Payload}", payloadAsJson);
        
        try
        {
            // ← ALTERE ESTA LINHA: de PostAsJsonAsync para PostAsJsonCamelCaseAsync
            var response = await _httpClient.PostAsJsonCamelCaseAsync(url, payload, ct);


            if (!response.IsSuccessStatusCode)
            {
                var conteudoErro = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Erro ao enviar mídia: {StatusCode} - {Conteudo}",
                    response.StatusCode, conteudoErro);
                return (false, null, $"Erro na API: {response.StatusCode} - {conteudoErro}");
            }

            // Tentar extrair messageId da resposta
            var conteudo = await response.Content.ReadAsStringAsync(ct);
            var jsonDoc = JsonDocument.Parse(conteudo);
            var messageId = jsonDoc.RootElement.TryGetProperty("messageId", out var prop)
                ? prop.GetString()
                : jsonDoc.RootElement.TryGetProperty("id", out var prop2)
                    ? prop2.GetString()
                    : null;

            _logger.LogInformation("Mídia enviada com sucesso. MessageId: {MessageId}", messageId);
            return (true, messageId, null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro de conexão ao enviar mídia");
            return (false, null, $"Erro de conexão: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao enviar mídia");
            return (false, null, $"Erro inesperado: {ex.Message}");
        }
    }

    /// <summary>
    /// Normaliza número de telefone para formato internacional (55 + DDD + número).
    /// Exemplo: "21993901365" -> "5521993901365"
    /// </summary>
    private static string NormalizarNumeroTelefone(string numero)
    {
        if (string.IsNullOrWhiteSpace(numero))
            return string.Empty;

        // Remover caracteres especiais
        var apenasDigitos = System.Text.RegularExpressions.Regex.Replace(numero, @"\D", "");

        // Se já começa com 55, retornar como está
        if (apenasDigitos.StartsWith("55"))
            return apenasDigitos;

        // Se tem 11 dígitos (DDD + 9 dígitos), adicionar 55
        if (apenasDigitos.Length == 11)
            return "55" + apenasDigitos;

        // Se tem 10 dígitos (DDD + 8 dígitos), adicionar 55
        if (apenasDigitos.Length == 10)
            return "55" + apenasDigitos;

        // Caso contrário, retornar como está (pode estar em outro formato)
        return apenasDigitos;
    }
}