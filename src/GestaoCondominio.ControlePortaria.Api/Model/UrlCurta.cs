namespace GestaoCondominio.ControlePortaria.Api.Model;

public sealed class UrlCurta
{
    public string Id { get; init; } = default!;
    public string Nome { get; init; } = default!;
    public string Telefone { get; init; } = default!;
    public string CodigoDaUnidade { get; init; } = default!;
    public string PalavraChave { get; init; } = default!;
    public DateTimeOffset CriadoEm { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiracaoEm { get; init; }
    public bool Ativo { get; set; } = true;

    public static UrlCurta Criar(
        string nome,
        string telefone,
        string codigoDaUnidade,
        string palavraChave,
        TimeSpan duracao)
    {
        var agora = DateTimeOffset.UtcNow;
        return new UrlCurta
        {
            Id = GerarIdCurto(),
            Nome = nome,
            Telefone = telefone,
            CodigoDaUnidade = codigoDaUnidade,
            PalavraChave = palavraChave,
            CriadoEm = agora,
            ExpiracaoEm = agora.Add(duracao),
            Ativo = true
        };
    }

    public bool EstaExpirada() => DateTimeOffset.UtcNow > ExpiracaoEm;

    private static string GerarIdCurto()
    {
        const string caracteres = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var resultado = new char[8];

        for (int i = 0; i < resultado.Length; i++)
            resultado[i] = caracteres[random.Next(caracteres.Length)];

        return new string(resultado);
    }
}