namespace GestaoCondominio.ControlePortaria.Api.Model;
public sealed record CheckInRegistro(
    Guid CheckInId,
    DateTimeOffset DataHora,
    Guid? DocumentoId,           // Obrigatório no primeiro check-in
    string UsuarioPortariaId,
    string? Observacoes
)
{
    public static CheckInRegistro Criar(Guid? documentoId, string usuarioPortariaId, string? observacoes = null) =>
        new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            documentoId,
            usuarioPortariaId,
            observacoes
        );
}

public sealed record CheckOutRegistro(
    Guid CheckOutId,
    DateTimeOffset DataHora,
    string UsuarioPortariaId,
    string? Observacoes
)
{
    public static CheckOutRegistro Criar(string usuarioPortariaId, string? observacoes = null) =>
        new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            usuarioPortariaId,
            observacoes
        );
}