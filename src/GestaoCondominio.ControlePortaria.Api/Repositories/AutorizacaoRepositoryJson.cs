using System.Text.Json;
using System.Text.Json.Serialization;
using global::GestaoCondominio.ControlePortaria.Api.Model;
using Microsoft.AspNetCore.Hosting;

namespace GestaoCondominio.ControlePortaria.Api.Repositories;

public sealed class AutorizacaoRepositoryJson : IAutorizacaoRepository
{
    private static readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public AutorizacaoRepositoryJson(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "Data");
        if (!Directory.Exists(dataDir))
            Directory.CreateDirectory(dataDir);

        _filePath = Path.Combine(dataDir, "autorizacoes.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        // .NET 8 já serializa DateOnly/TimeOnly nativamente

        EnsureFileInitialized();
    }

    public async Task<AutorizacaoDeAcesso> AddAsync(AutorizacaoDeAcesso entity, CancellationToken ct)
    {
        await _mutex.WaitAsync(ct);
        try
        {
            var list = await ReadAllAsync(ct);
            // Evitar duplicidade por Id
            if (list.Any(x => x.Id == entity.Id))
            {
                // Caso venha um Id pré-definido, geramos novo para não colidir
                entity = CloneWithNewId(entity);
            }

            list.Add(entity);
            await SaveAllAsync(list, ct);
            return entity;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<AutorizacaoDeAcesso?> GetAsync(Guid id, CancellationToken ct)
    {
        var list = await ReadAllAsync(ct);
        return list.FirstOrDefault(x => x.Id == id);
    }

    public async Task<IReadOnlyList<AutorizacaoDeAcesso>> QueryAsync(string? condominioId, string? unidadeCodigo, string? status, CancellationToken ct)
    {
        var list = await ReadAllAsync(ct);
        IEnumerable<AutorizacaoDeAcesso> q = list;

        if (!string.IsNullOrWhiteSpace(condominioId))
            q = q.Where(x => string.Equals(x.CondominioId, condominioId, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(unidadeCodigo))
            q = q.Where(x => string.Equals(x.Autorizador.CodigoDaUnidade, unidadeCodigo, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (TryParseStatus(status, out var parsed))
            {
                q = q.Where(x => x.Status == parsed);
            }
            else
            {
                // fallback: tenta comparar string do enum
                q = q.Where(x => string.Equals(x.Status.ToString(), status, StringComparison.OrdinalIgnoreCase));
            }
        }

        // Ordena por CreatedAt desc para navegação mais conveniente
        q = q.OrderByDescending(x => x.CreatedAt);
        return q.ToList();
    }

    public async Task UpdateAsync(AutorizacaoDeAcesso entity, CancellationToken ct)
    {
        await _mutex.WaitAsync(ct);
        try
        {
            var list = await ReadAllAsync(ct);
            var idx = list.FindIndex(x => x.Id == entity.Id);
            if (idx < 0)
            {
                // Se não existir, adiciona (opção pragmática) — alternativamente poderíamos lançar exceção
                list.Add(entity);
            }
            else
            {
                list[idx] = entity;
            }

            await SaveAllAsync(list, ct);
        }
        finally
        {
            _mutex.Release();
        }
    }

    // ----------------- Helpers -----------------

    private void EnsureFileInitialized()
    {
        if (!File.Exists(_filePath))
        {
            File.WriteAllText(_filePath, "[]");
            return;
        }

        // Se estiver vazio ou corrompido, reescreve como []
        try
        {
            var content = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(content))
                File.WriteAllText(_filePath, "[]");
            else
                JsonSerializer.Deserialize<List<AutorizacaoDeAcesso>>(content, _jsonOptions);
        }
        catch
        {
            File.WriteAllText(_filePath, "[]");
        }
    }

    private async Task<List<AutorizacaoDeAcesso>> ReadAllAsync(CancellationToken ct)
    {
        try
        {
            await using var fs = File.Open(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var list = await JsonSerializer.DeserializeAsync<List<AutorizacaoDeAcesso>>(fs, _jsonOptions, ct);
            return list ?? new List<AutorizacaoDeAcesso>();
        }
        catch
        {
            // Em caso de erro inesperado, devolve lista vazia (robustez)
            return new List<AutorizacaoDeAcesso>();
        }
    }

    private async Task SaveAllAsync(List<AutorizacaoDeAcesso> list, CancellationToken ct)
    {
        var tmp = _filePath + ".tmp";
        var json = JsonSerializer.Serialize(list, _jsonOptions);

        // Escrita atômica: grava em .tmp e move
        await File.WriteAllTextAsync(tmp, json, ct);

        // Substitui o arquivo original
        if (File.Exists(_filePath))
            File.Delete(_filePath);
        File.Move(tmp, _filePath);
    }

    private static AutorizacaoDeAcesso CloneWithNewId(AutorizacaoDeAcesso entity)
    {
        // Cria uma cópia com novo Guid. Como é uma classe com setters init em vários campos,
        // criamos uma nova instância copiando os valores principais.
        return new AutorizacaoDeAcesso
        {
            // Novo Id
            Id = Guid.NewGuid(),

            // Identificação
            CondominioId = entity.CondominioId,

            // Classificação
            Tipo = entity.Tipo,
            Periodo = entity.Periodo,

            // Dados pessoais
            Nome = entity.Nome,
            Email = entity.Email,
            Telefone = entity.Telefone,
            Cpf = entity.Cpf,
            Rg = entity.Rg,
            Empresa = entity.Empresa,
            Cnpj = entity.Cnpj,

            // Vigência
            DataInicio = entity.DataInicio,
            DataFim = entity.DataFim,

            // Recorrência
            DiasSemanaPermitidos = entity.DiasSemanaPermitidos is null ? null : new List<DayOfWeek>(entity.DiasSemanaPermitidos),
            JanelaHorarioInicio = entity.JanelaHorarioInicio,
            JanelaHorarioFim = entity.JanelaHorarioFim,

            // Veículo
            Veiculo = entity.Veiculo is null ? null : new Veiculo(entity.Veiculo.Placa, entity.Veiculo.Marca, entity.Veiculo.Modelo),

            // Detalhes
            Autorizador = entity.Autorizador,
            InformacoesDispositivo = entity.InformacoesDispositivo,

            // Operacional
            CodigoAcesso = entity.CodigoAcesso,
            QrCodePayload = entity.QrCodePayload,
            Status = entity.Status,

            // Auditoria
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CriadoPorUsuarioId = entity.CriadoPorUsuarioId
        };
    }

    private static bool TryParseStatus(string input, out StatusAutorizacao status)
    {
        // Aceita várias formas (ex.: "autorizado", "Autorizado", "AUTORIZADO")
        var norm = (input ?? string.Empty).Trim().ToLowerInvariant();

        // Map por nomes "humanos"
        switch (norm)
        {
            case "pendente": status = StatusAutorizacao.Pendente; return true;
            case "autorizado":
            case "ativa":
            case "ativo":
                status = StatusAutorizacao.Autorizado; return true;
            case "utilizado":
            case "utilizada":
            case "checkin":
                status = StatusAutorizacao.Utilizado; return true;
            case "expirado":
            case "expirada":
                status = StatusAutorizacao.Expirado; return true;
            case "cancelado":
            case "cancelada":
                status = StatusAutorizacao.Cancelado; return true;
        }

        // Tenta enum direto (e.g. "Autorizado")
        if (Enum.TryParse<StatusAutorizacao>(input, ignoreCase: true, out var parsed))
        {
            status = parsed;
            return true;
        }

        status = default;
        return false;
    }
}
