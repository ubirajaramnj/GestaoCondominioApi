using System.Text.Json.Serialization;

namespace GestaoCondominio.ControlePortaria.Api.DTOs;
public sealed class EnviarMensagemMediaRequest
{
    [JsonPropertyName("number")]
    public string Number { get; set; } = default!;
    [JsonPropertyName("mediatype")]
    public string MediaType { get; set; } = "document"; // image, video, document
    [JsonPropertyName("mimetype")]
    public string MimeType { get; set; } = "application/pdf";
    [JsonPropertyName("caption")]
    public string Caption { get; set; } = default!;
    [JsonPropertyName("media")]
    public string Media { get; set; } = default!; // URL ou base64
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = default!;

    [JsonPropertyName("delay")]
    public int? Delay { get; set; }
    [JsonPropertyName("mentionsEveryOne")]
    public bool? MentionsEveryOne { get; set; }
    [JsonPropertyName("mentioned")]
    public List<string>? Mentioned { get; set; }
}

public sealed class EnviarMensagemMediaResponse
{
    [JsonPropertyName("sucesso")]
    public bool Sucesso { get; set; }
    [JsonPropertyName("messageId")]
    public string? MessageId { get; set; }
    [JsonPropertyName("erro")]
    public string? Erro { get; set; }
    [JsonPropertyName("enviadoEm")]
    public DateTimeOffset EnviadoEm { get; set; }
}

public sealed class ConfiguracaoMensageria
{
    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; } = default!;
    [JsonPropertyName("instance")]
    public string Instance { get; set; } = default!;
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = default!;
}