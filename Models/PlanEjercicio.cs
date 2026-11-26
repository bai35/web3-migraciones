namespace GimnasioApi.Models;

// Tabla puente de la relación muchos a muchos Plan <-> Ejercicio (con series y repeticiones).
public class PlanEjercicio
{
    public int PlanEntrenamientoId { get; set; }
    public int EjercicioId { get; set; }
    public int Series { get; set; }
    public int Repeticiones { get; set; }

    public virtual PlanEntrenamiento PlanEntrenamiento { get; set; } = null!;
    public virtual Ejercicio Ejercicio { get; set; } = null!;
}
