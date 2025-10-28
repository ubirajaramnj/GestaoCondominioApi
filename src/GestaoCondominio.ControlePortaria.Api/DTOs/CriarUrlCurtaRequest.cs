namespace GestaoCondominio.ControlePortaria.Api.DTOs;
using System.Text.Json.Serialization;

public sealed class CriarUrlCurtaRequest
{
    [JsonPropertyName("nome")]
    public string Nome { get; set; } = default!;

    [JsonPropertyName("telefone")]
    public string Telefone { get; set; } = default!;

    [JsonPropertyName("codigoDaUnidade")]
    public string CodigoDaUnidade { get; set; } = default!;

    [JsonPropertyName("palavraChave")]
    public string PalavraChave { get; set; } = default!;
}

public sealed class CriarUrlCurtaResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("urlCurta")]
    public string UrlCurta { get; set; } = default!;

    [JsonPropertyName("urlCompleta")]
    public string UrlCompleta { get; set; } = default!;

    [JsonPropertyName("criadoEm")]
    public DateTimeOffset CriadoEm { get; set; }

    [JsonPropertyName("expiracaoEm")]
    public DateTimeOffset ExpiracaoEm { get; set; }
}

public sealed class RecuperarUrlCurtaResponse
{
    [JsonPropertyName("nome")]
    public string Nome { get; set; } = default!;

    [JsonPropertyName("telefone")]
    public string Telefone { get; set; } = default!;

    [JsonPropertyName("codigoDaUnidade")]
    public string CodigoDaUnidade { get; set; } = default!;

    [JsonPropertyName("palavraChave")]
    public string PalavraChave { get; set; } = default!;

    [JsonPropertyName("criadoEm")]
    public DateTimeOffset CriadoEm { get; set; }

    [JsonPropertyName("expiracaoEm")]
    public DateTimeOffset ExpiracaoEm { get; set; }
}