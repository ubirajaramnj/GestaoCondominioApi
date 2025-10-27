namespace GestaoCondominio.ControlePortaria.Api.Infrastructure.Extensions;

using System.Text.Json;
using System.Text.Json.Serialization;

public static class HttpClientExtensions
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        //PropertyNamingPolicy = new LowerCaseNamingPolicy(),
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    /// <summary>
    /// POST com JSON serializado em camelCase e ignorando nulls.
    /// </summary>
    public static async Task<HttpResponseMessage> PostAsJsonCamelCaseAsync<T>(
        this HttpClient client,
        string requestUri,
        T value,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value, DefaultJsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        return await client.PostAsync(requestUri, content, cancellationToken);
    }

    /// <summary>
    /// GET com suporte a camelCase na desserialização.
    /// </summary>
    public static async Task<T?> GetFromJsonCamelCaseAsync<T>(
        this HttpClient client,
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<T>(json, DefaultJsonOptions);
    }
}

/// <summary>
/// JsonNamingPolicy que converte nomes de propriedades para lowercase completo.
/// Exemplo: MediaType → mediatype (não mediaType)
/// </summary>
public sealed class LowerCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        return name.ToLowerInvariant();
    }
}