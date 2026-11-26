using System.ComponentModel.DataAnnotations;

namespace GimnasioApi.Dtos;

public class EjercicioRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El grupo muscular es obligatorio.")]
    [StringLength(50)]
    public string GrupoMuscular { get; set; } = string.Empty;
}

public class EjercicioResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string GrupoMuscular { get; set; } = string.Empty;
}
