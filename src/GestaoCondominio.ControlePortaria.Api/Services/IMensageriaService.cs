namespace GestaoCondominio.ControlePortaria.Api.Services;

public interface IMensageriaService
{
    /// <summary>
    /// Envia uma mensagem com mídia (documento, imagem, vídeo) via WhatsApp/API.
    /// </summary>
    Task<(bool Sucesso, string? MessageId, string? Erro)> EnviarComprovanteAsync(
        string numeroTelefone,
        string urlComprovante,
        string nomeArquivo,
        string caption,
        CancellationToken ct);

    /// <summary>
    /// Envia uma mensagem de texto simples.
    /// </summary>
    Task<(bool Sucesso, string? MessageId, string? Erro)> EnviarMensagemAsync(
        string numeroTelefone,
        string mensagem,
        CancellationToken ct);
}