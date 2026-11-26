using System.ComponentModel.DataAnnotations;

namespace GimnasioApi.Dtos;

public class MembresiaRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [Range(1, 3650, ErrorMessage = "La duración debe estar entre 1 y 3650 días.")]
    public int DuracionDias { get; set; }

    [Range(0.01, 100000, ErrorMessage = "El precio debe ser mayor a 0.")]
    public decimal Precio { get; set; }
}

public class MembresiaResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int DuracionDias { get; set; }
    public decimal Precio { get; set; }
    public bool Activa { get; set; }
}
