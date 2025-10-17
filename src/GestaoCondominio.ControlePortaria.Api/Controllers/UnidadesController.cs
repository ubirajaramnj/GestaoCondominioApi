using GestaoCondominio.ControlePortaria.Api.Model;
using GestaoCondominio.ControlePortaria.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestaoCondominio.ControlePortaria.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UnidadesController : ControllerBase
    {
        private readonly ILogger<UnidadesController> _logger;
        private readonly IUnidadeService _unidadeService;

        public UnidadesController(ILogger<UnidadesController> logger, IUnidadeService unidadeService)
        {
            _logger = logger;
            _unidadeService = unidadeService;
        }

        /// <summary>
        /// Retorna a unidade de um condomínio pelo telefone informado.
        /// </summary>
        [HttpGet("Condominio/{codCondominio}/Telefone/{telefone}")]
        public ActionResult<Unidade> GetCondominoByPhone(string codCondominio, long telefone)
        {
            var unidade = _unidadeService.GetUnidadesByTelefone(codCondominio, telefone);
            if (unidade == null)
            {
                _logger.LogWarning("Unidade não encontrada para telefone {Telefone} no condomínio {CodCondominio}",
                    telefone, codCondominio);
                return NotFound(new { Message = "Unidade não encontrada." });
            }

            return Ok(unidade);
        }

        /// <summary>
        /// Força a atualização do cache de condomínios a partir do arquivo JSON.
        /// </summary>
        [HttpPost("reload-cache")]
        public IActionResult ReloadCache()
        {
            try
            {
                _logger.LogInformation("Solicitação recebida para recarregar o cache de condomínios.");
                _unidadeService.ReloadCache();

                _logger.LogInformation("Cache recarregado com sucesso via endpoint administrativo.");
                return Ok(new { Message = "Cache recarregado com sucesso." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao recarregar o cache de condomínios.");
                return StatusCode(500, new { Message = "Erro ao recarregar o cache.", Details = ex.Message });
            }
        }
    }
}
