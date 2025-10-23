namespace GestaoCondominio.ControlePortaria.Api.Services;
using global::GestaoCondominio.ControlePortaria.Api.Model;
using global::GestaoCondominio.ControlePortaria.Api.Repositories;

public sealed class DocumentoService : IDocumentoService
{
    private readonly IDocumentoRepository _repo;
    private readonly IWebHostEnvironment _env;
    private readonly string _pastaDocumentos;

    // Extensões permitidas
    private static readonly HashSet<string> ExtensõesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".doc", ".docx", ".xls", ".xlsx", ".txt"
    };

    // Tamanho máximo: 10 MB
    private const long TamanhoMaximoBytes = 10 * 1024 * 1024;

    public DocumentoService(IDocumentoRepository repo, IWebHostEnvironment env)
    {
        _repo = repo;
        _env = env;
        _pastaDocumentos = Path.Combine(env.ContentRootPath, "documentos");

        // Criar pasta se não existir
        if (!Directory.Exists(_pastaDocumentos))
            Directory.CreateDirectory(_pastaDocumentos);
    }

    public async Task<(bool Sucesso, string? Erro, ArquivoDeDocumento? Documento)> UploadAsync(
        Guid autorizacaoId,
        string tipoDocumento,
        IFormFile arquivo,
        CancellationToken ct)
    {
        if (arquivo.Length > TamanhoMaximoBytes)
            return (false, $"Arquivo excede o tamanho máximo de {TamanhoMaximoBytes / (1024 * 1024)} MB.", null);

        var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        if (!ExtensõesPermitidas.Contains(extensao))
            return (false, $"Tipo de arquivo não permitido. Extensões aceitas: {string.Join(", ", ExtensõesPermitidas)}", null);

        if (string.IsNullOrWhiteSpace(tipoDocumento))
            return (false, "tipoDocumento é obrigatório.", null);

        try
        {
            // Gerar nome único para o arquivo
            var documentoId = Guid.NewGuid();
            var nomeArmazenado = $"{documentoId}{extensao}";
            var caminhoCompleto = Path.Combine(_pastaDocumentos, nomeArmazenado);

            // Salvar arquivo no disco
            await using (var stream = new FileStream(caminhoCompleto, FileMode.Create, FileAccess.Write))
            {
                await arquivo.CopyToAsync(stream, ct);
            }

            // Criar entidade de documento
            var documento = new ArquivoDeDocumento
            {
                Id = documentoId,
                AutorizacaoId = autorizacaoId,
                Tipo = tipoDocumento,
                Nome = arquivo.FileName,
                CaminhoArmazenado = nomeArmazenado,
                TamanhoBytes = arquivo.Length,
                ContentType = arquivo.ContentType ?? "application/octet-stream"
            };

            // Persistir metadados
            await _repo.AddAsync(documento, ct);

            return (true, null, documento);
        }
        catch (Exception ex)
        {
            return (false, $"Erro ao salvar arquivo: {ex.Message}", null);
        }
    }

    public Task<ArquivoDeDocumento?> ObterAsync(Guid documentoId, CancellationToken ct) =>
        _repo.GetAsync(documentoId, ct);

    public Task<IReadOnlyList<ArquivoDeDocumento>> ListarPorAutorizacaoAsync(Guid autorizacaoId, CancellationToken ct) =>
        _repo.QueryByAutorizacaoAsync(autorizacaoId, ct);

    public async Task<(bool Sucesso, byte[]? Conteudo, string? ContentType, string? Erro)> BaixarAsync(
        Guid documentoId,
        CancellationToken ct)
    {
        var documento = await _repo.GetAsync(documentoId, ct);
        if (documento is null)
            return (false, null, null, "Documento não encontrado.");

        var caminhoCompleto = Path.Combine(_pastaDocumentos, documento.CaminhoArmazenado);
        if (!File.Exists(caminhoCompleto))
            return (false, null, null, "Arquivo não encontrado no armazenamento.");

        try
        {
            var conteudo = await File.ReadAllBytesAsync(caminhoCompleto, ct);
            return (true, conteudo, documento.ContentType, null);
        }
        catch (Exception ex)
        {
            return (false, null, null, $"Erro ao ler arquivo: {ex.Message}");
        }
    }

    public async Task<(bool Sucesso, string? Erro)> DeletarAsync(Guid documentoId, CancellationToken ct)
    {
        var documento = await _repo.GetAsync(documentoId, ct);
        if (documento is null)
            return (false, "Documento não encontrado.");

        try
        {
            var caminhoCompleto = Path.Combine(_pastaDocumentos, documento.CaminhoArmazenado);
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