namespace GestaoCondominio.ControlePortaria.Api.Repositories;

using global::GestaoCondominio.ControlePortaria.Api.Model;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class DocumentoRepositoryJson : IDocumentoRepository
{
    private static readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public DocumentoRepositoryJson(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "Data");
        if (!Directory.Exists(dataDir))
            Directory.CreateDirectory(dataDir);

        _filePath = Path.Combine(dataDir, "documentos.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());

        EnsureFileInitialized();
    }

    public async Task<ArquivoDeDocumento> AddAsync(ArquivoDeDocumento entity, CancellationToken ct)
    {
        await _mutex.WaitAsync(ct);
        try
        {
            var list = await ReadAllAsync(ct);
            list.Add(entity);
            await SaveAllAsync(list, ct);
            return entity;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<ArquivoDeDocumento?> GetAsync(Guid documentoId, CancellationToken ct)
    {
        var list = await ReadAllAsync(ct);
        return list.FirstOrDefault(x => x.Id == documentoId);
    }

    public async Task<IReadOnlyList<ArquivoDeDocumento>> QueryByAutorizacaoAsync(Guid autorizacaoId, CancellationToken ct)
    {
        var list = await ReadAllAsync(ct);
        return list.Where(x => x.AutorizacaoId == autorizacaoId).OrderByDescending(x => x.CriadoEm).ToList();
    }

    public async Task<IReadOnlyList<ArquivoDeDocumento>> QueryByTipoAsync(string tipoDocumento, CancellationToken ct)
    {
        var list = await ReadAllAsync(ct);
        return list
            .Where(x => string.Equals(x.Tipo, tipoDocumento, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.CriadoEm)
            .ToList();
    }

    // --------- Helpers ---------

    private void EnsureFileInitialized()
    {
        if (!File.Exists(_filePath))
        {
            File.WriteAllText(_filePath, "[]");
            return;
        }

        try
        {
            var content = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(content))
                File.WriteAllText(_filePath, "[]");
            else
                JsonSerializer.Deserialize<List<ArquivoDeDocumento>>(content, _jsonOptions);
        }
        catch
        {
            File.WriteAllText(_filePath, "[]");
        }
    }

    private async Task<List<ArquivoDeDocumento>> ReadAllAsync(CancellationToken ct)
    {
        try
        {
            await using var fs = File.Open(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var list = await JsonSerializer.DeserializeAsync<List<ArquivoDeDocumento>>(fs, _jsonOptions, ct);
            return list ?? new List<ArquivoDeDocumento>();
        }
        catch
        {
            return new List<ArquivoDeDocumento>();
        }
    }

    private async Task SaveAllAsync(List<ArquivoDeDocumento> list, CancellationToken ct)
    {
        var tmp = _filePath + ".tmp";
        var json = JsonSerializer.Serialize(list, _jsonOptions);

        await File.WriteAllTextAsync(tmp, json, ct);

        if (File.Exists(_filePath))
            File.Delete(_filePath);

        File.Move(tmp, _filePath);
    }
}