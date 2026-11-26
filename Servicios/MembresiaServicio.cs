using System.Linq.Expressions;
using GimnasioApi.Dtos;
using GimnasioApi.Models;
using GimnasioApi.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace GimnasioApi.Servicios;

public interface IMembresiaServicio
{
    Task<MembresiaResponse> CrearAsync(MembresiaRequest request);
    Task<PaginadoResponse<MembresiaResponse>> ListarAsync(int pagina, int tamanio, bool soloActivas);
    Task<MembresiaResponse> ObtenerAsync(int id);
    Task<MembresiaResponse> ActualizarAsync(int id, MembresiaRequest request);
    Task DesactivarAsync(int id);
}

public class MembresiaServicio : IMembresiaServicio
{
    private readonly IRepositorio<Membresia> _membresias;
    private readonly IUnidadDeTrabajo _uow;

    public MembresiaServicio(IRepositorio<Membresia> membresias, IUnidadDeTrabajo uow)
    {
        _membresias = membresias;
        _uow = uow;
    }

    private static readonly Expression<Func<Membresia, MembresiaResponse>> Proyeccion = m => new MembresiaResponse
    {
        Id = m.Id,
        Nombre = m.Nombre,
        DuracionDias = m.DuracionDias,
        Precio = m.Precio,
        Activa = m.Activa
    };

    public async Task<MembresiaResponse> CrearAsync(MembresiaRequest request)
    {
        if (await _membresias.Consultar().AnyAsync(m => m.Nombre == request.Nombre))
            throw new InvalidOperationException($"Ya existe una membresía llamada {request.Nombre}.");

        var membresia = new Membresia
        {
            Nombre = request.Nombre,
            DuracionDias = request.DuracionDias,
            Precio = request.Precio,
            Activa = true
        };
        await _membresias.AgregarAsync(membresia);
        await _uow.GuardarAsync();

        return await ObtenerAsync(membresia.Id);
    }

    public async Task<PaginadoResponse<MembresiaResponse>> ListarAsync(int pagina, int tamanio, bool soloActivas)
    {
        var consulta = _membresias.Consultar().AsNoTracking();
        if (soloActivas) consulta = consulta.Where(m => m.Activa);

        return await consulta.OrderBy(m => m.Id).Select(Proyeccion).PaginarAsync(pagina, tamanio);
    }

    public async Task<MembresiaResponse> ObtenerAsync(int id)
    {
        return await _membresias.Consultar().AsNoTracking()
            .Where(m => m.Id == id)
            .Select(Proyeccion)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"No existe una membresía con id {id}.");
    }

    public async Task<MembresiaResponse> ActualizarAsync(int id, MembresiaRequest request)
    {
        var membresia = await _membresias.ObtenerPorIdAsync(id)
            ?? throw new KeyNotFoundException($"No existe una membresía con id {id}.");

        if (await _membresias.Consultar().AnyAsync(m => m.Nombre == request.Nombre && m.Id != id))
            throw new InvalidOperationException($"Ya existe otra membresía llamada {request.Nombre}.");

        membresia.Nombre = request.Nombre;
        membresia.DuracionDias = request.DuracionDias;
        membresia.Precio = request.Precio;
        await _uow.GuardarAsync();

        return await ObtenerAsync(id);
    }

    public async Task DesactivarAsync(int id)
    {
        var membresia = await _membresias.ObtenerPorIdAsync(id)
            ?? throw new KeyNotFoundException($"No existe una membresía con id {id}.");

        membresia.Activa = false;
        await _uow.GuardarAsync();
    }
}
