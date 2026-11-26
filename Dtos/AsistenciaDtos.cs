using System.ComponentModel.DataAnnotations;

namespace GimnasioApi.Dtos;

public class AsistenciaCrearRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "El id del socio es obligatorio.")]
    public int SocioId { get; set; }

    [StringLength(250)]
    public string? Observacion { get; set; }
}

public class AsistenciaResponse
{
    public int Id { get; set; }
    public int SocioId { get; set; }
    public string SocioNombre { get; set; } = string.Empty;
    public int? EntrenadorId { get; set; }
    public string? EntrenadorNombre { get; set; }
    public DateTime FechaHora { get; set; }
    public string? Observacion { get; set; }
}

public class ResumenAsistenciaResponse
{
    public int SocioId { get; set; }
    public string SocioNombre { get; set; } = string.Empty;
    public int TotalAsistencias { get; set; }
    public int AsistenciasMesActual { get; set; }
    public DateTime? UltimaAsistencia { get; set; }
}
