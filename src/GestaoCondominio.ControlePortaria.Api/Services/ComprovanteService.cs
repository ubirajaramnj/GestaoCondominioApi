namespace GestaoCondominio.ControlePortaria.Api.Services;
using global::GestaoCondominio.ControlePortaria.Api.Model;
using global::GestaoCondominio.ControlePortaria.Api.Repositories;

public sealed class ComprovanteService : IComprovanteService
{
    private readonly IComprovanteRepository _repo;
    private readonly IWebHostEnvironment _env;
    private readonly string _pastaComprovantes;

    // Extensões permitidas
    private static readonly HashSet<string> ExtensõesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".doc", ".docx", ".xls", ".xlsx", ".txt"
    };

    // Tamanho máximo: 10 MB
    private const long TamanhoMaximoBytes = 10 * 1024 * 1024;

    public ComprovanteService(IComprovanteRepository repo, IWebHostEnvironment env)
    {
        _repo = repo;
        _env = env;
        _pastaComprovantes = Path.Combine(env.ContentRootPath, "comprovantes");

        // Criar pasta se não existir
        if (!Directory.Exists(_pastaComprovantes))
            Directory.CreateDirectory(_pastaComprovantes);
    }

    public async Task<(bool Sucesso, string? Erro, ArquivoDeComprovante? Comprovante)> UploadAsync(
        Guid autorizacaoId,
        IFormFile arquivo,
        CancellationToken ct)
    {
        if (arquivo.Length > TamanhoMaximoBytes)
            return (false, $"Arquivo excede o tamanho máximo de {TamanhoMaximoBytes / (1024 * 1024)} MB.", null);

        //var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        //if (!ExtensõesPermitidas.Contains(extensao))
        //    return (false, $"Tipo de arquivo não permitido. Extensões aceitas: {string.Join(", ", ExtensõesPermitidas)}", null);

        //if (string.IsNullOrWhiteSpace(tipoComprovante))
        //    return (false, "tipoComprovante é obrigatório.", null);

        try
        {
            // Gerar nome único para o arquivo
            var comprovanteId = Guid.NewGuid();
            var nomeArmazenado = $"{autorizacaoId}.pdf";
            var caminhoCompleto = Path.Combine(_pastaComprovantes, nomeArmazenado);

            // Salvar arquivo no disco
            await using (var stream = new FileStream(caminhoCompleto, FileMode.Create, FileAccess.Write))
            {
                await arquivo.CopyToAsync(stream, ct);
            }

            // Criar entidade de comprovante
            var comprovante = new ArquivoDeComprovante
            {
                Id = comprovanteId,
                AutorizacaoId = autorizacaoId,
                //Tipo = tipoComprovante,
                Nome = arquivo.FileName,
                CaminhoArmazenado = nomeArmazenado,
                TamanhoBytes = arquivo.Length,
                ContentType = arquivo.ContentType ?? "application/octet-stream"
            };

            // Persistir metadados
            await _repo.AddAsync(comprovante, ct);

            return (true, null, comprovante);
        }
        catch (Exception ex)
        {
            return (false, $"Erro ao salvar arquivo: {ex.Message}", null);
        }
    }

    public Task<ArquivoDeComprovante?> ObterAsync(Guid comprovanteId, CancellationToken ct) =>
        _repo.GetAsync(comprovanteId, ct);

    public Task<IReadOnlyList<ArquivoDeComprovante>> ListarPorAutorizacaoAsync(Guid autorizacaoId, CancellationToken ct) =>
        _repo.QueryByAutorizacaoAsync(autorizacaoId, ct);

    public async Task<(bool Sucesso, byte[]? Conteudo, string? ContentType, string? Erro)> BaixarAsync(
        Guid comprovanteId,
        CancellationToken ct)
    {
        var comprovante = await _repo.GetAsync(comprovanteId, ct);
        if (comprovante is null)
            return (false, null, null, "Comprovante não encontrado.");

        var caminhoCompleto = Path.Combine(_pastaComprovantes, comprovante.CaminhoArmazenado);
        if (!File.Exists(caminhoCompleto))
            return (false, null, null, "Arquivo não encontrado no armazenamento.");

        try
        {
            var conteudo = await File.ReadAllBytesAsync(caminhoCompleto, ct);
            return (true, conteudo, comprovante.ContentType, null);
        }
        catch (Exception ex)
        {
            return (false, null, null, $"Erro ao ler arquivo: {ex.Message}");
        }
    }

    public async Task<(bool Sucesso, string? Erro)> DeletarAsync(Guid comprovanteId, CancellationToken ct)
    {
        var comprovante = await _repo.GetAsync(comprovanteId, ct);
        if (comprovante is null)
            return (false, "Comprovante não encontrado.");

        try
        {
            var caminhoCompleto = Path.Combine(_pastaComprovantes, comprovante.CaminhoArmazenado);
            if (File.Exists(caminhoCompleto))
                File.Delete(caminhoCompleto);

            // Aqui você poderia remover também do JSON, mas por enquanto apenas deletamos o arquivo
            // Para manter auditoria, podemos deixar o registro no JSON

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Erro ao deletar arquivo: {ex.Message}");
        }
    }
}