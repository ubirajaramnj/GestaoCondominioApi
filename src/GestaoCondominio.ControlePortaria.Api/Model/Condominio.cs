namespace GestaoCondominio.ControlePortaria.Api.Model
{
    public class Condominio
    {
        public string Codigo { get; set; }
        public string Nome { get; set; }
        public string Endereco { get; set; }

        public virtual List<Unidade> Unidades { get; set; } = [];
    }
}
