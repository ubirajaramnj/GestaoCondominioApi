namespace GestaoCondominio.ControlePortaria.Api.Repositories;

using GestaoCondominio.ControlePortaria.Api.Model;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class UrlCurtaRepositoryJson : IUrlCurtaRepository
{
    private static readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public UrlCurtaRepositoryJson(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "Data");
        if (!Directory.Exists(dataDir))
            Directory.CreateDirectory(dataDir);

        _filePath = Path.Combine(dataDir, "urlsCurtas.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());

        EnsureFileInitialized();
    }

    public async Task<UrlCurta> AddAsync(UrlCurta entity, CancellationToken ct)
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

    public async Task<UrlCurta?> GetAsync(string id, CancellationToken ct)
    {
        var list = await ReadAllAsync(ct);
        return list.FirstOrDefault(x => x.Id == id);
    }

    public async Task<IReadOnlyList<UrlCurta>> QueryAsync(CancellationToken ct)
    {
        var list = await ReadAllAsync(ct);
        return list.Where(x => x.Ativo && !x.EstaExpirada()).ToList();
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct)
    {
        await _mutex.WaitAsync(ct);
        try
        {
            var list = await ReadAllAsync(ct);
            var item = list.FirstOrDefault(x => x.Id == id);
            if (item is null) return false;

            item.Ativo = false; // Soft delete
            await SaveAllAsync(list, ct);
            return true;
        }
        finally
        {
            _mutex.Release();
        }
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
                JsonSerializer.Deserialize<List<UrlCurta>>(content, _jsonOptions);
        }
        catch
        {
            File.WriteAllText(_filePath, "[]");
        }
    }

    private async Task<List<UrlCurta>> ReadAllAsync(CancellationToken ct)
    {
        try
        {
            await using var fs = File.Open(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var list = await JsonSerializer.DeserializeAsync<List<UrlCurta>>(fs, _jsonOptions, ct);
            return list ?? new List<UrlCurta>();
        }
        catch
        {
            return new List<UrlCurta>();
        }
    }

    private async Task SaveAllAsync(List<UrlCurta> list, CancellationToken ct)
    {
        var tmp = _filePath + ".tmp";
        var json = JsonSerializer.Serialize(list, _jsonOptions);

        await File.WriteAllTextAsync(tmp, json, ct);

        if (File.Exists(_filePath))
            File.Delete(_filePath);

        File.Move(tmp, _filePath);
    }
}