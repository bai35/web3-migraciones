namespace GimnasioApi.Models;

public class Entrenador
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = null!;
    public string Documento { get; set; } = null!;
    public string? Telefono { get; set; }
    public string Especialidad { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
