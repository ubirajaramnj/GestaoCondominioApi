using GestaoCondominio.ControlePortaria.Api.Model;

namespace GestaoCondominio.ControlePortaria.Api.Services
{
    public interface IUnidadeService
    {
        Unidade GetUnidadesByTelefone(string codCondominio, long telefone);
        Unidade? GetUnidadesByTelefone(long telefone);
        void ReloadCache();
    }
}