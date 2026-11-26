namespace GimnasioApi.Models;

public class Ejercicio
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string GrupoMuscular { get; set; } = null!;
}
