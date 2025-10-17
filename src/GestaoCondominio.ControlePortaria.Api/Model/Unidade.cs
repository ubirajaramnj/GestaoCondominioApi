namespace GestaoCondominio.ControlePortaria.Api.Model
{
    public class Unidade
    {
        public string Codigo { get; set; }

        public Proprietario Proprietario { get; set; } = new Proprietario();    

        public string Logradouro { get; set; }
        public string Rua { get; set; }
        public string Lote { get; set; }
        public string Quadra { get; set; }
    }
}
