namespace GimnasioApi.Seguridad;

public class JwtOpciones
{
    public string Clave { get; set; } = string.Empty;
    public string Emisor { get; set; } = string.Empty;
    public string Audiencia { get; set; } = string.Empty;
    public int MinutosAcceso { get; set; } = 15;
    public int DiasRefresh { get; set; } = 7;
}
