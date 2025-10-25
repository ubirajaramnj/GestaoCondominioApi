namespace GestaoCondominio.ControlePortaria.Api.DTOs;
public sealed class UploadComprovanteRequest
{
    public Guid AutorizacaoId { get; set; }
    public IFormFile Arquivo { get; set; } = default!;
}

public sealed class UploadComprovanteResponse
{
    public Guid AutorizacaoId { get; set; }
    public string Link { get; set; } = default!;
}