namespace GestaoCondominio.ControlePortaria.Api.Repositories;

using global::GestaoCondominio.ControlePortaria.Api.Model;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class ComprovanteRepositoryJson : IComprovanteRepository
{
    private static readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public ComprovanteRepositoryJson(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "Data");
        if (!Directory.Exists(dataDir))
            Directory.CreateDirectory(dataDir);

        _filePath = Path.Combine(dataDir, "comprovantes.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());

        EnsureFileInitialized();
    }

    public async Task<ArquivoDeComprovante> AddAsync(ArquivoDeComprovante entity, CancellationToken ct)
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

    public async Task<ArquivoDeComprovante?> GetAsync(Guid comprovanteId, CancellationToken ct)
    {
        var list = await ReadAllAsync(ct);
        return list.FirstOrDefault(x => x.AutorizacaoId == comprovanteId);
    }

    public async Task<IReadOnlyList<ArquivoDeComprovante>> QueryByAutorizacaoAsync(Guid autorizacaoId, CancellationToken ct)
    {
        var list = await ReadAllAsync(ct);
        return list.Where(x => x.AutorizacaoId == autorizacaoId).OrderByDescending(x => x.CriadoEm).ToList();
    }

    public async Task<IReadOnlyList<ArquivoDeComprovante>> QueryByTipoAsync(string tipoComprovante, CancellationToken ct)
    {
        var list = await ReadAllAsync(ct);
        return [];

        //return list
        //    .Where(x => string.Equals(x.Tipo, tipoComprovante, StringComparison.OrdinalIgnoreCase))
        //    .OrderByDescending(x => x.CriadoEm)
        //    .ToList();
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
                JsonSerializer.Deserialize<List<ArquivoDeComprovante>>(content, _jsonOptions);
        }
        catch
        {
            File.WriteAllText(_filePath, "[]");
        }
    }

    private async Task<List<ArquivoDeComprovante>> ReadAllAsync(CancellationToken ct)
    {
        try
        {
            await using var fs = File.Open(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var list = await JsonSerializer.DeserializeAsync<List<ArquivoDeComprovante>>(fs, _jsonOptions, ct);
            return list ?? new List<ArquivoDeComprovante>();
        }
        catch
        {
            return new List<ArquivoDeComprovante>();
        }
    }

    private async Task SaveAllAsync(List<ArquivoDeComprovante> list, CancellationToken ct)
    {
        var tmp = _filePath + ".tmp";
        var json = JsonSerializer.Serialize(list, _jsonOptions);

        await File.WriteAllTextAsync(tmp, json, ct);

        if (File.Exists(_filePath))
            File.Delete(_filePath);

        File.Move(tmp, _filePath);
    }
}