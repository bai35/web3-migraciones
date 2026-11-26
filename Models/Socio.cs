namespace GimnasioApi.Models;

public class Socio
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int? EntrenadorId { get; set; }
    public int? MembresiaId { get; set; }
    public string Nombre { get; set; } = null!;
    public string Documento { get; set; } = null!;
    public string? Telefono { get; set; }
    public DateTime FechaRegistro { get; set; }
    public DateTime? MembresiaInicio { get; set; }
    public DateTime? MembresiaFin { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
    public virtual Entrenador? Entrenador { get; set; }
    public virtual Membresia? Membresia { get; set; }
}
