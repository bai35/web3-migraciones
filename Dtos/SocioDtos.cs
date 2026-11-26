using System.ComponentModel.DataAnnotations;

namespace GimnasioApi.Dtos;

public class SocioCrearRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El documento es obligatorio.")]
    [StringLength(20)]
    public string Documento { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>Opcional: si se envía, el socio queda con esa membresía vigente desde hoy.</summary>
    public int? MembresiaId { get; set; }

    /// <summary>Opcional: entrenador asignado.</summary>
    public int? EntrenadorId { get; set; }
}

public class SocioActualizarRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Telefono { get; set; }
}

public class AsignarEntrenadorRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "El id del entrenador es obligatorio.")]
    public int EntrenadorId { get; set; }
}

public class RenovarMembresiaRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "El id de la membresía es obligatorio.")]
    public int MembresiaId { get; set; }
}

public class SocioResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime FechaRegistro { get; set; }
    public int? EntrenadorId { get; set; }
    public string? EntrenadorNombre { get; set; }
    public int? MembresiaId { get; set; }
    public string? MembresiaNombre { get; set; }
    public DateTime? MembresiaInicio { get; set; }
    public DateTime? MembresiaFin { get; set; }

    public bool MembresiaVigente => MembresiaFin.HasValue && MembresiaFin.Value >= DateTime.Today;
}
