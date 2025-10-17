using GestaoCondominio.ControlePortaria.Api.Converters;
using System.Text.Json.Serialization;

namespace GestaoCondominio.ControlePortaria.Api.Model
{
    public class Proprietario
    {
        public string Nome { get; set; }
        public List<Documento> Documentos { get; set; } = new List<Documento>();
        public List<Telefone> Telefones { get; set; } = new List<Telefone>();

        public List<Email> Emails { get; set; } = new List<Email>();

        public string Observacoes { get; set; }
    }

    public class Documento {
        public string Tipo { get; set; }
        public string Numero { get; set; }
    }

    public class Telefone
    {
        public string Tipo { get; set; }

        [JsonConverter(typeof(LongFromStringConverter))]
        public long Numero { get; set; }
    }

    public class Email
    {
        public string Tipo { get; set; }
        public string Endereco { get; set; }
    }
}