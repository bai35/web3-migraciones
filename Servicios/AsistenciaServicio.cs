using System.Security.Claims;
using GimnasioApi.Dtos;
using GimnasioApi.Models;
using GimnasioApi.Repositorios;
using GimnasioApi.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace GimnasioApi.Servicios;

public interface IAsistenciaServicio
{
    Task<AsistenciaResponse> RegistrarAsync(AsistenciaCrearRequest request, ClaimsPrincipal usuario);
    Task<PaginadoResponse<AsistenciaResponse>> HistorialAsync(
        int socioId, ClaimsPrincipal usuario, DateTime? desde, DateTime? hasta, int pagina, int tamanio);
    Task<ResumenAsistenciaResponse> ResumenAsync(int socioId, ClaimsPrincipal usuario);
}

public class AsistenciaServicio : IAsistenciaServicio
{
    private readonly IRepositorio<Asistencia> _asistencias;
    private readonly IAccesoSocioServicio _acceso;
    private readonly IReportesRepositorio _reportes;
    private readonly IUnidadDeTrabajo _uow;

    public AsistenciaServicio(
        IRepositorio<Asistencia> asistencias,
        IAccesoSocioServicio acceso,
        IReportesRepositorio reportes,
        IUnidadDeTrabajo uow)
    {
        _asistencias = asistencias;
        _acceso = acceso;
        _reportes = reportes;
        _uow = uow;
    }

    public async Task<AsistenciaResponse> RegistrarAsync(AsistenciaCrearRequest request, ClaimsPrincipal usuario)
    {
        // Un ENTRENADOR solo puede registrar asistencia a sus socios; el ADMIN a cualquiera.
        var socio = await _acceso.ObtenerSocioAutorizadoAsync(request.SocioId, usuario);

        if (!socio.Usuario.Activo)
            throw new InvalidOperationException("El socio está dado de baja.");

        var hoy = DateTime.Today;
        if (socio.MembresiaFin is null || socio.MembresiaFin.Value < hoy)
            throw new InvalidOperationException("La membresía del socio no está vigente. Debe renovarla para ingresar.");

        var manana = hoy.AddDays(1);
        var yaAsistio = await _asistencias.Consultar()
            .AnyAsync(a => a.SocioId == socio.Id && a.FechaHora >= hoy && a.FechaHora < manana);
        if (yaAsistio)
            throw new InvalidOperationException("El socio ya registró su asistencia el día de hoy.");

        var entrenadorId = usuario.ObtenerRol() == Roles.Entrenador
            ? usuario.ObtenerPerfilId()
            : socio.EntrenadorId;

        var asistencia = new Asistencia
        {
            SocioId = socio.Id,
            EntrenadorId = entrenadorId,
            FechaHora = DateTime.Now,
            Observacion = request.Observacion
        };
        await _asistencias.AgregarAsync(asistencia);
        await _uow.GuardarAsync();

        return await _asistencias.Consultar().AsNoTracking()
            .Where(a => a.Id == asistencia.Id)
            .Select(a => new AsistenciaResponse
            {
                Id = a.Id,
                SocioId = a.SocioId,
                SocioNombre = a.Socio.Nombre,
                EntrenadorId = a.EntrenadorId,
                EntrenadorNombre = a.Entrenador == null ? null : a.Entrenador.Nombre,
                FechaHora = a.FechaHora,
                Observacion = a.Observacion
            })
            .FirstAsync();
    }

    public async Task<PaginadoResponse<AsistenciaResponse>> HistorialAsync(
        int socioId, ClaimsPrincipal usuario, DateTime? desde, DateTime? hasta, int pagina, int tamanio)
    {
        await _acceso.ObtenerSocioAutorizadoAsync(socioId, usuario);

        if (desde.HasValue && hasta.HasValue && hasta.Value < desde.Value)
            throw new ArgumentException("La fecha 'hasta' no puede ser anterior a 'desde'.");

        var consulta = _asistencias.Consultar().AsNoTracking().Where(a => a.SocioId == socioId);

        if (desde.HasValue)
        {
            var d = desde.Value.Date;
            consulta = consulta.Where(a => a.FechaHora >= d);
        }
        if (hasta.HasValue)
        {
            var h = hasta.Value.Date.AddDays(1);
            consulta = consulta.Where(a => a.FechaHora < h);
        }

        return await consulta
            .OrderByDescending(a => a.FechaHora)
            .Select(a => new AsistenciaResponse
            {
                Id = a.Id,
                SocioId = a.SocioId,
                SocioNombre = a.Socio.Nombre,
                EntrenadorId = a.EntrenadorId,
                EntrenadorNombre = a.Entrenador == null ? null : a.Entrenador.Nombre,
                FechaHora = a.FechaHora,
                Observacion = a.Observacion
            })
            .PaginarAsync(pagina, tamanio);
    }

    public async Task<ResumenAsistenciaResponse> ResumenAsync(int socioId, ClaimsPrincipal usuario)
    {
        var socio = await _acceso.ObtenerSocioAutorizadoAsync(socioId, usuario);

        // Datos calculados por el procedimiento almacenado usp_ResumenAsistenciasSocio.
        var fila = await _reportes.ObtenerResumenAsistenciasAsync(socioId);

        return new ResumenAsistenciaResponse
        {
            SocioId = socio.Id,
            SocioNombre = socio.Nombre,
            TotalAsistencias = fila.TotalAsistencias,
            AsistenciasMesActual = fila.AsistenciasMesActual,
            UltimaAsistencia = fila.UltimaAsistencia
        };
    }
}
