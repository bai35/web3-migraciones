using System.Security.Claims;
using GimnasioApi.Dtos;
using GimnasioApi.Models;
using GimnasioApi.Repositorios;
using GimnasioApi.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace GimnasioApi.Servicios;

public interface IPlanServicio
{
    Task<PlanResponse> CrearAsync(PlanCrearRequest request, ClaimsPrincipal usuario);
    Task<PaginadoResponse<PlanResponse>> ListarPorSocioAsync(int socioId, ClaimsPrincipal usuario, int pagina, int tamanio);
    Task<PlanResponse> ObtenerPlanActivoAsync(int socioId, ClaimsPrincipal usuario);
}

public class PlanServicio : IPlanServicio
{
    private readonly IRepositorio<PlanEntrenamiento> _planes;
    private readonly IRepositorio<PlanEjercicio> _planEjercicios;
    private readonly IRepositorio<Ejercicio> _ejercicios;
    private readonly IAccesoSocioServicio _acceso;
    private readonly IUnidadDeTrabajo _uow;

    public PlanServicio(
        IRepositorio<PlanEntrenamiento> planes,
        IRepositorio<PlanEjercicio> planEjercicios,
        IRepositorio<Ejercicio> ejercicios,
        IAccesoSocioServicio acceso,
        IUnidadDeTrabajo uow)
    {
        _planes = planes;
        _planEjercicios = planEjercicios;
        _ejercicios = ejercicios;
        _acceso = acceso;
        _uow = uow;
    }

    public async Task<PlanResponse> CrearAsync(PlanCrearRequest request, ClaimsPrincipal usuario)
    {
        var socio = await _acceso.ObtenerSocioAutorizadoAsync(request.SocioId, usuario);

        int entrenadorId;
        if (usuario.ObtenerRol() == Roles.Entrenador)
            entrenadorId = usuario.ObtenerPerfilId();
        else
            entrenadorId = socio.EntrenadorId
                ?? throw new ArgumentException("El socio no tiene entrenador asignado. Asígnale uno antes de crear su plan.");

        var inicio = (request.FechaInicio ?? DateTime.Today).Date;
        if (request.FechaFin.HasValue && request.FechaFin.Value.Date < inicio)
            throw new ArgumentException("La fecha de fin no puede ser anterior a la fecha de inicio.");

        var idsEjercicios = request.Ejercicios.Select(e => e.EjercicioId).ToList();
        if (idsEjercicios.Distinct().Count() != idsEjercicios.Count)
            throw new ArgumentException("Hay ejercicios repetidos en el plan.");

        var existentes = await _ejercicios.Consultar().CountAsync(e => idsEjercicios.Contains(e.Id));
        if (existentes != idsEjercicios.Count)
            throw new ArgumentException("Alguno de los ejercicios indicados no existe.");

        // Transacción: se desactiva el plan anterior y se crea el nuevo con todos sus ejercicios.
        var planId = await _uow.EjecutarEnTransaccionAsync(async () =>
        {
            var anteriores = await _planes.Consultar()
                .Where(p => p.SocioId == socio.Id && p.Activo)
                .ToListAsync();
            foreach (var anterior in anteriores) anterior.Activo = false;

            var plan = new PlanEntrenamiento
            {
                SocioId = socio.Id,
                EntrenadorId = entrenadorId,
                Nombre = request.Nombre,
                Objetivo = request.Objetivo,
                FechaInicio = inicio,
                FechaFin = request.FechaFin?.Date,
                Activo = true
            };
            await _planes.AgregarAsync(plan);

            foreach (var e in request.Ejercicios)
            {
                await _planEjercicios.AgregarAsync(new PlanEjercicio
                {
                    PlanEntrenamiento = plan,
                    EjercicioId = e.EjercicioId,
                    Series = e.Series,
                    Repeticiones = e.Repeticiones
                });
            }

            await _uow.GuardarAsync();
            return plan.Id;
        });

        var creado = await ConsultarPlanes().Where(p => p.Id == planId).ToListAsync();
        await CargarEjerciciosAsync(creado);
        return creado[0];
    }

    public async Task<PaginadoResponse<PlanResponse>> ListarPorSocioAsync(
        int socioId, ClaimsPrincipal usuario, int pagina, int tamanio)
    {
        await _acceso.ObtenerSocioAutorizadoAsync(socioId, usuario);

        var resultado = await ConsultarPlanes()
            .Where(p => p.SocioId == socioId)
            .OrderByDescending(p => p.FechaInicio).ThenByDescending(p => p.Id)
            .PaginarAsync(pagina, tamanio);

        await CargarEjerciciosAsync(resultado.Datos);
        return resultado;
    }

    public async Task<PlanResponse> ObtenerPlanActivoAsync(int socioId, ClaimsPrincipal usuario)
    {
        await _acceso.ObtenerSocioAutorizadoAsync(socioId, usuario);

        var planes = await ConsultarPlanes()
            .Where(p => p.SocioId == socioId && p.Activo)
            .ToListAsync();

        if (planes.Count == 0)
            throw new KeyNotFoundException("El socio no tiene un plan de entrenamiento activo.");

        await CargarEjerciciosAsync(planes);
        return planes[0];
    }

    private IQueryable<PlanResponse> ConsultarPlanes()
    {
        return _planes.Consultar().AsNoTracking().Select(p => new PlanResponse
        {
            Id = p.Id,
            SocioId = p.SocioId,
            SocioNombre = p.Socio.Nombre,
            EntrenadorId = p.EntrenadorId,
            EntrenadorNombre = p.Entrenador.Nombre,
            Nombre = p.Nombre,
            Objetivo = p.Objetivo,
            FechaInicio = p.FechaInicio,
            FechaFin = p.FechaFin,
            Activo = p.Activo
        });
    }

    // Carga los ejercicios de todos los planes con una sola consulta (evita una consulta por plan).
    private async Task CargarEjerciciosAsync(IEnumerable<PlanResponse> planes)
    {
        var lista = planes.ToList();
        if (lista.Count == 0) return;

        var ids = lista.Select(p => p.Id).ToList();

        var filas = await _planEjercicios.Consultar().AsNoTracking()
            .Where(pe => ids.Contains(pe.PlanEntrenamientoId))
            .OrderBy(pe => pe.EjercicioId)
            .Select(pe => new
            {
                pe.PlanEntrenamientoId,
                Ejercicio = new PlanEjercicioResponse
                {
                    EjercicioId = pe.EjercicioId,
                    Nombre = pe.Ejercicio.Nombre,
                    GrupoMuscular = pe.Ejercicio.GrupoMuscular,
                    Series = pe.Series,
                    Repeticiones = pe.Repeticiones
                }
            })
            .ToListAsync();

        foreach (var plan in lista)
            plan.Ejercicios = filas.Where(f => f.PlanEntrenamientoId == plan.Id).Select(f => f.Ejercicio).ToList();
    }
}
