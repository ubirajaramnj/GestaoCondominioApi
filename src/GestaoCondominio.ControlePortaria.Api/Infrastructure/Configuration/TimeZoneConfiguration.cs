namespace GestaoCondominio.ControlePortaria.Api.Infrastructure.Configuration;
public sealed class TimeZoneConfiguration
{
    public static TimeZoneInfo BrasilTimeZone { get; private set; } = default!;

    public static void Inicializar()
    {
        try
        {
            BrasilTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); // Windows
        }
        catch
        {
            BrasilTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); // Linux
        }
    }
}