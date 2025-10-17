using System.Text.Json;
using GestaoCondominio.ControlePortaria.Api.Model;
using Microsoft.Extensions.Logging;

namespace GestaoCondominio.ControlePortaria.Api.Services
{
    public class UnidadeService : IUnidadeService
    {
        private readonly string _jsonFilePath;
        private readonly ILogger<UnidadeService> _logger;

        private List<Condominio> _condominiosCache = new();
        private DateTime _lastLoadTime;
        private DateTime _lastFileWriteTime;
        private readonly object _lock = new();

        public UnidadeService(string jsonFilePath, ILogger<UnidadeService> logger)
        {
            _jsonFilePath = jsonFilePath ?? throw new ArgumentNullException(nameof(jsonFilePath));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (!File.Exists(_jsonFilePath))
            {
                _logger.LogError("Arquivo JSON não encontrado: {Path}", _jsonFilePath);
                throw new FileNotFoundException($"Arquivo JSON não encontrado: {_jsonFilePath}");
            }

            LoadData(forceReload: true);
        }

        /// <summary>
        /// Carrega os dados do arquivo JSON apenas se ele foi modificado desde o último carregamento.
        /// </summary>
        private void LoadData(bool forceReload = false)
        {
            lock (_lock)
            {
                var fileInfo = new FileInfo(_jsonFilePath);

                bool precisaRecarregar = forceReload ||
                                         !_condominiosCache.Any() ||
                                         fileInfo.LastWriteTime != _lastFileWriteTime;

                if (!precisaRecarregar)
                    return;

                _logger.LogInformation("Recarregando dados do arquivo JSON: {Path}", _jsonFilePath);

                try
                {
                    var json = File.ReadAllText(_jsonFilePath);
                    _condominiosCache = JsonSerializer.Deserialize<List<Condominio>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Condominio>();

                    _lastFileWriteTime = fileInfo.LastWriteTime;
                    _lastLoadTime = DateTime.UtcNow;

                    _logger.LogInformation(
                        "Arquivo JSON carregado com sucesso ({Count} condomínios) em {Time}.",
                        _condominiosCache.Count, _lastLoadTime);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao carregar o arquivo JSON de condomínios: {Path}", _jsonFilePath);
                }
            }
        }

        /// <summary>
        /// Busca a unidade por código de condomínio e número de telefone.
        /// </summary>
        public Unidade? GetUnidadesByTelefone(string codCondominio, long telefone)
        {
            LoadData();

            _logger.LogDebug("Buscando unidade - Condomínio: {CodCondominio}, Telefone: {Telefone}",
                codCondominio, telefone);

            var condominio = _condominiosCache.FirstOrDefault(c =>
                c.Codigo.Equals(codCondominio, StringComparison.OrdinalIgnoreCase));

            if (condominio == null)
            {
                _logger.LogWarning("Condomínio {CodCondominio} não encontrado no cache.", codCondominio);
                return null;
            }

            var unidade = condominio.Unidades
                .FirstOrDefault(u => u.Proprietario.Telefones.Any(t => t.Numero == telefone) == true);

            if (unidade == null)
            {
                _logger.LogWarning("Nenhuma unidade encontrada para telefone {Telefone} no condomínio {CodCondominio}.",
                    telefone, codCondominio);
            }

            return unidade;
        }

        /// <summary>
        /// Busca uma unidade por telefone em todos os condomínios (sem informar o código).
        /// </summary>
        public Unidade? GetUnidadesByTelefone(long telefone)
        {
            LoadData();

            _logger.LogDebug("Buscando unidade globalmente pelo telefone {Telefone}", telefone);

            foreach (var condominio in _condominiosCache)
            {
                var unidade = condominio.Unidades
                    .FirstOrDefault(u => u.Proprietario?.Telefones?.Any(t => t.Numero == telefone) == true);

                if (unidade != null)
                {
                    _logger.LogInformation("Unidade encontrada para telefone {Telefone} no condomínio {CodCondominio}.",
                        telefone, condominio.Codigo);
                    return unidade;
                }
            }

            _logger.LogWarning("Nenhuma unidade encontrada para telefone {Telefone} em nenhum condomínio.", telefone);
            return null;
        }

        public void ReloadCache()
        {
            _logger.LogInformation("Forçando recarregamento manual do cache de condomínios.");
            LoadData(forceReload: true);
        }
    }
}
