using System.Linq.Expressions;
using GimnasioApi.Dtos;
using GimnasioApi.Models;
using GimnasioApi.Repositorios;
using GimnasioApi.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace GimnasioApi.Servicios;

public interface IEntrenadorServicio
{
    Task<EntrenadorResponse> CrearAsync(EntrenadorCrearRequest request);
    Task<PaginadoResponse<EntrenadorResponse>> ListarAsync(int pagina, int tamanio, string? buscar);
    Task<EntrenadorResponse> ObtenerAsync(int id);
    Task<EntrenadorResponse> ActualizarAsync(int id, EntrenadorActualizarRequest request);
    Task DesactivarAsync(int id);
}

public class EntrenadorServicio : IEntrenadorServicio
{
    private readonly IRepositorio<Entrenador> _entrenadores;
    private readonly IRepositorio<Usuario> _usuarios;
    private readonly IRepositorio<Socio> _socios;
    private readonly IUnidadDeTrabajo _uow;

    public EntrenadorServicio(
        IRepositorio<Entrenador> entrenadores,
        IRepositorio<Usuario> usuarios,
        IRepositorio<Socio> socios,
        IUnidadDeTrabajo uow)
    {
        _entrenadores = entrenadores;
        _usuarios = usuarios;
        _socios = socios;
        _uow = uow;
    }

    private static readonly Expression<Func<Entrenador, EntrenadorResponse>> Proyeccion = e => new EntrenadorResponse
    {
        Id = e.Id,
        Nombre = e.Nombre,
        Documento = e.Documento,
        Telefono = e.Telefono,
        Especialidad = e.Especialidad,
        Email = e.Usuario.Email,
        Activo = e.Usuario.Activo
    };

    public async Task<EntrenadorResponse> CrearAsync(EntrenadorCrearRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _usuarios.Consultar().AnyAsync(u => u.Email == email))
            throw new InvalidOperationException($"Ya existe un usuario con el correo {email}.");
        if (await _entrenadores.Consultar().AnyAsync(e => e.Documento == request.Documento))
            throw new InvalidOperationException($"Ya existe un entrenador con el documento {request.Documento}.");

        // Transacción: se crea el usuario de acceso y el perfil de entrenador, o no se crea nada.
        var id = await _uow.EjecutarEnTransaccionAsync(async () =>
        {
            var usuario = new Usuario
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Rol = Roles.Entrenador,
                Activo = true
            };
            await _usuarios.AgregarAsync(usuario);
            await _uow.GuardarAsync();

            var entrenador = new Entrenador
            {
                UsuarioId = usuario.Id,
                Nombre = request.Nombre,
                Documento = request.Documento,
                Telefono = request.Telefono,
                Especialidad = request.Especialidad
            };
            await _entrenadores.AgregarAsync(entrenador);
            await _uow.GuardarAsync();

            return entrenador.Id;
        });

        return await ObtenerAsync(id);
    }

    public async Task<PaginadoResponse<EntrenadorResponse>> ListarAsync(int pagina, int tamanio, string? buscar)
    {
        var consulta = _entrenadores.Consultar().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(buscar))
            consulta = consulta.Where(e => e.Nombre.Contains(buscar) || e.Documento.Contains(buscar));

        return await consulta
            .OrderBy(e => e.Id)
            .Select(Proyeccion)
            .PaginarAsync(pagina, tamanio);
    }

    public async Task<EntrenadorResponse> ObtenerAsync(int id)
    {
        return await _entrenadores.Consultar().AsNoTracking()
            .Where(e => e.Id == id)
            .Select(Proyeccion)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"No existe un entrenador con id {id}.");
    }

    public async Task<EntrenadorResponse> ActualizarAsync(int id, EntrenadorActualizarRequest request)
    {
        var entrenador = await _entrenadores.ObtenerPorIdAsync(id)
            ?? throw new KeyNotFoundException($"No existe un entrenador con id {id}.");

        entrenador.Nombre = request.Nombre;
        entrenador.Telefono = request.Telefono;
        entrenador.Especialidad = request.Especialidad;
        await _uow.GuardarAsync();

        return await ObtenerAsync(id);
    }

    public async Task DesactivarAsync(int id)
    {
        var entrenador = await _entrenadores.Consultar()
            .Include(e => e.Usuario)
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new KeyNotFoundException($"No existe un entrenador con id {id}.");

        var tieneSocios = await _socios.Consultar()
            .AnyAsync(s => s.EntrenadorId == id && s.Usuario.Activo);
        if (tieneSocios)
            throw new InvalidOperationException("No se puede dar de baja: el entrenador aún tiene socios activos asignados.");

        entrenador.Usuario.Activo = false;
        await _uow.GuardarAsync();
    }
}
