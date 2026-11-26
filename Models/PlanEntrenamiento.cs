namespace GimnasioApi.Models;

public class PlanEntrenamiento
{
    public int Id { get; set; }
    public int SocioId { get; set; }
    public int EntrenadorId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Objetivo { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public bool Activo { get; set; }

    public virtual Socio Socio { get; set; } = null!;
    public virtual Entrenador Entrenador { get; set; } = null!;
}
