namespace GimnasioApi.Repositorios;

/// <summary>Fila que devuelve el procedimiento almacenado usp_ResumenAsistenciasSocio.</summary>
public class ResumenAsistenciaFila
{
    public int TotalAsistencias { get; set; }
    public DateTime? UltimaAsistencia { get; set; }
    public int AsistenciasMesActual { get; set; }
}
