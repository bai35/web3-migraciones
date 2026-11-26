namespace GimnasioApi.Models;

public class Asistencia
{
    public int Id { get; set; }
    public int SocioId { get; set; }
    public int? EntrenadorId { get; set; }
    public DateTime FechaHora { get; set; }
    public string? Observacion { get; set; }

    public virtual Socio Socio { get; set; } = null!;
    public virtual Entrenador? Entrenador { get; set; }
}
