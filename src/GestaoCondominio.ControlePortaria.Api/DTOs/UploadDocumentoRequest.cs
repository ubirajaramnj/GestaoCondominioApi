namespace GestaoCondominio.ControlePortaria.Api.DTOs;
public sealed class UploadDocumentoRequest
{
    public Guid AutorizacaoId { get; set; }
    public string TipoDocumento { get; set; } = default!;
    public IFormFile Arquivo { get; set; } = default!;
}

public sealed class UploadDocumentoResponse
{
    public Guid AutorizacaoId { get; set; }
    public Guid DocumentoId { get; set; }
    public string Link { get; set; } = default!;
}