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
        /// Retorna a unidade de um condom�nio pelo telefone informado.
        /// </summary>
        [HttpGet("Condominio/{codCondominio}/Telefone/{telefone}")]
        public ActionResult<Unidade> GetCondominoByPhone(string codCondominio, long telefone)
        {
            var unidade = _unidadeService.GetUnidadesByTelefone(codCondominio, telefone);
            if (unidade == null)
            {
                _logger.LogWarning("Unidade n�o encontrada para telefone {Telefone} no condom�nio {CodCondominio}",
                    telefone, codCondominio);
                return NotFound(new { Message = "Unidade n�o encontrada." });
            }

            return Ok(unidade);
        }

        /// <summary>
        /// For�a a atualiza��o do cache de condom�nios a partir do arquivo JSON.
        /// </summary>
        [HttpPost("reload-cache")]
        public IActionResult ReloadCache()
        {
            try
            {
                _logger.LogInformation("Solicita��o recebida para recarregar o cache de condom�nios.");
                _unidadeService.ReloadCache();

                _logger.LogInformation("Cache recarregado com sucesso via endpoint administrativo.");
                return Ok(new { Message = "Cache recarregado com sucesso." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao recarregar o cache de condom�nios.");
                return StatusCode(500, new { Message = "Erro ao recarregar o cache.", Details = ex.Message });
            }
        }
    }
}
