using Microsoft.EntityFrameworkCore;

namespace GimnasioApi.Data;

public static class InicializadorBD
{
    // Procedimiento almacenado que usa GET /api/socios/{id}/asistencias/resumen
    private const string ProcedimientoResumen = @"
CREATE OR ALTER PROCEDURE dbo.usp_ResumenAsistenciasSocio
    @SocioId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        COUNT(*) AS TotalAsistencias,
        MAX(FechaHora) AS UltimaAsistencia,
        ISNULL(SUM(CASE WHEN YEAR(FechaHora) = YEAR(GETDATE()) AND MONTH(FechaHora) = MONTH(GETDATE())
                        THEN 1 ELSE 0 END), 0) AS AsistenciasMesActual
    FROM dbo.Asistencia
    WHERE SocioId = @SocioId;
END";

    public static async Task EjecutarAsync(IServiceProvider servicios, IConfiguration config)
    {
        using var scope = servicios.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GimnasioDbContext>();

        if (!db.Database.GetMigrations().Any())
            throw new InvalidOperationException(
                "No hay migraciones en el proyecto. Ejecuta primero: dotnet ef migrations add InitialCreate");

        // Crea la base de datos (si no existe) y aplica las migraciones pendientes.
        await db.Database.MigrateAsync();

        // Los procedimientos almacenados no forman parte del modelo de EF, se crean aparte.
        await db.Database.ExecuteSqlRawAsync(ProcedimientoResumen);

        await DatosIniciales.SembrarAsync(db, config);
    }
}
