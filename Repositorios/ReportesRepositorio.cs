using GimnasioApi.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GimnasioApi.Repositorios;

/// <summary>Consultas que usan procedimientos almacenados.</summary>
public class ReportesRepositorio : IReportesRepositorio
{
    private readonly GimnasioDbContext _db;

    public ReportesRepositorio(GimnasioDbContext db)
    {
        _db = db;
    }

    public async Task<ResumenAsistenciaFila> ObtenerResumenAsistenciasAsync(int socioId)
    {
        var filas = await _db.Database
            .SqlQueryRaw<ResumenAsistenciaFila>(
                "EXEC dbo.usp_ResumenAsistenciasSocio @SocioId",
                new SqlParameter("@SocioId", socioId))
            .ToListAsync();

        return filas.FirstOrDefault() ?? new ResumenAsistenciaFila();
    }
}
