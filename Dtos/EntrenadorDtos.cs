using System.ComponentModel.DataAnnotations;

namespace GimnasioApi.Dtos;

public class EntrenadorCrearRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El documento es obligatorio.")]
    [StringLength(20)]
    public string Documento { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "La especialidad es obligatoria.")]
    [StringLength(100)]
    public string Especialidad { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string Password { get; set; } = string.Empty;
}

public class EntrenadorActualizarRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "La especialidad es obligatoria.")]
    [StringLength(100)]
    public string Especialidad { get; set; } = string.Empty;
}

public class EntrenadorResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string Especialidad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Activo { get; set; }
}
