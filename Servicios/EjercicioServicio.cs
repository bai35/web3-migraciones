using GimnasioApi.Dtos;
using GimnasioApi.Models;
using GimnasioApi.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace GimnasioApi.Servicios;

public interface IEjercicioServicio
{
    Task<EjercicioResponse> CrearAsync(EjercicioRequest request);
    Task<PaginadoResponse<EjercicioResponse>> ListarAsync(int pagina, int tamanio, string? grupoMuscular);
}

public class EjercicioServicio : IEjercicioServicio
{
    private readonly IRepositorio<Ejercicio> _ejercicios;
    private readonly IUnidadDeTrabajo _uow;

    public EjercicioServicio(IRepositorio<Ejercicio> ejercicios, IUnidadDeTrabajo uow)
    {
        _ejercicios = ejercicios;
        _uow = uow;
    }

    public async Task<EjercicioResponse> CrearAsync(EjercicioRequest request)
    {
        if (await _ejercicios.Consultar().AnyAsync(e => e.Nombre == request.Nombre))
            throw new InvalidOperationException($"Ya existe el ejercicio {request.Nombre}.");

        var ejercicio = new Ejercicio { Nombre = request.Nombre, GrupoMuscular = request.GrupoMuscular };
        await _ejercicios.AgregarAsync(ejercicio);
        await _uow.GuardarAsync();

        return new EjercicioResponse { Id = ejercicio.Id, Nombre = ejercicio.Nombre, GrupoMuscular = ejercicio.GrupoMuscular };
    }

    public async Task<PaginadoResponse<EjercicioResponse>> ListarAsync(int pagina, int tamanio, string? grupoMuscular)
    {
        var consulta = _ejercicios.Consultar().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(grupoMuscular))
            consulta = consulta.Where(e => e.GrupoMuscular.Contains(grupoMuscular));

        return await consulta
            .OrderBy(e => e.Id)
            .Select(e => new EjercicioResponse { Id = e.Id, Nombre = e.Nombre, GrupoMuscular = e.GrupoMuscular })
            .PaginarAsync(pagina, tamanio);
    }
}
