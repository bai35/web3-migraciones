namespace GimnasioApi.Models;

public class Membresia
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public int DuracionDias { get; set; }
    public decimal Precio { get; set; }
    public bool Activa { get; set; }
}
