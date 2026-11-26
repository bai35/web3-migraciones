using System.ComponentModel.DataAnnotations;

namespace GimnasioApi.Dtos;

public class PlanEjercicioRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "El id del ejercicio es obligatorio.")]
    public int EjercicioId { get; set; }

    [Range(1, 20, ErrorMessage = "Las series deben estar entre 1 y 20.")]
    public int Series { get; set; }

    [Range(1, 100, ErrorMessage = "Las repeticiones deben estar entre 1 y 100.")]
    public int Repeticiones { get; set; }
}

public class PlanCrearRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "El id del socio es obligatorio.")]
    public int SocioId { get; set; }

    [Required(ErrorMessage = "El nombre del plan es obligatorio.")]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Objetivo { get; set; }

    /// <summary>Si no se envía, se usa la fecha de hoy.</summary>
    public DateTime? FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    [Required(ErrorMessage = "El plan debe tener ejercicios.")]
    [MinLength(1, ErrorMessage = "El plan debe tener al menos un ejercicio.")]
    public List<PlanEjercicioRequest> Ejercicios { get; set; } = new();
}

public class PlanEjercicioResponse
{
    public int EjercicioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string GrupoMuscular { get; set; } = string.Empty;
    public int Series { get; set; }
    public int Repeticiones { get; set; }
}

public class PlanResponse
{
    public int Id { get; set; }
    public int SocioId { get; set; }
    public string SocioNombre { get; set; } = string.Empty;
    public int EntrenadorId { get; set; }
    public string EntrenadorNombre { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Objetivo { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public bool Activo { get; set; }
    public List<PlanEjercicioResponse> Ejercicios { get; set; } = new();
}
