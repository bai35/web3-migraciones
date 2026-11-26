namespace GimnasioApi.Repositorios;

public interface IReportesRepositorio
{
    Task<ResumenAsistenciaFila> ObtenerResumenAsistenciasAsync(int socioId);
}
