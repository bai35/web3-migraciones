using System.Linq.Expressions;
using System.Security.Claims;
using GimnasioApi.Dtos;
using GimnasioApi.Models;
using GimnasioApi.Repositorios;
using GimnasioApi.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace GimnasioApi.Servicios;

public interface ISocioServicio
{
    Task<SocioResponse> CrearAsync(SocioCrearRequest request);
    Task<PaginadoResponse<SocioResponse>> ListarAsync(ClaimsPrincipal usuario, int pagina, int tamanio, string? buscar, bool? soloVigentes);
    Task<SocioResponse> ObtenerAsync(int id, ClaimsPrincipal usuario);
    Task<SocioResponse> ActualizarAsync(int id, SocioActualizarRequest request);
    Task<SocioResponse> AsignarEntrenadorAsync(int id, int entrenadorId);
    Task<SocioResponse> RenovarMembresiaAsync(int id, int membresiaId);
    Task DesactivarAsync(int id);
}

public class SocioServicio : ISocioServicio
{
    private readonly IRepositorio<Socio> _socios;
    private readonly IRepositorio<Usuario> _usuarios;
    private readonly IRepositorio<Entrenador> _entrenadores;
    private readonly IRepositorio<Membresia> _membresias;
    private readonly IAccesoSocioServicio _acceso;
    private readonly IUnidadDeTrabajo _uow;

    public SocioServicio(
        IRepositorio<Socio> socios,
        IRepositorio<Usuario> usuarios,
        IRepositorio<Entrenador> entrenadores,
        IRepositorio<Membresia> membresias,
        IAccesoSocioServicio acceso,
        IUnidadDeTrabajo uow)
    {
        _socios = socios;
        _usuarios = usuarios;
        _entrenadores = entrenadores;
        _membresias = membresias;
        _acceso = acceso;
        _uow = uow;
    }

    private static readonly Expression<Func<Socio, SocioResponse>> Proyeccion = s => new SocioResponse
    {
        Id = s.Id,
        Nombre = s.Nombre,
        Documento = s.Documento,
        Telefono = s.Telefono,
        Email = s.Usuario.Email,
        Activo = s.Usuario.Activo,
        FechaRegistro = s.FechaRegistro,
        EntrenadorId = s.EntrenadorId,
        EntrenadorNombre = s.Entrenador == null ? null : s.Entrenador.Nombre,
        MembresiaId = s.MembresiaId,
        MembresiaNombre = s.Membresia == null ? null : s.Membresia.Nombre,
        MembresiaInicio = s.MembresiaInicio,
        MembresiaFin = s.MembresiaFin
    };

    public async Task<SocioResponse> CrearAsync(SocioCrearRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _usuarios.Consultar().AnyAsync(u => u.Email == email))
            throw new InvalidOperationException($"Ya existe un usuario con el correo {email}.");
        if (await _socios.Consultar().AnyAsync(s => s.Documento == request.Documento))
            throw new InvalidOperationException($"Ya existe un socio con el documento {request.Documento}.");

        Membresia? membresia = null;
        if (request.MembresiaId.HasValue)
        {
            membresia = await _membresias.ObtenerPorIdAsync(request.MembresiaId.Value);
            if (membresia is null || !membresia.Activa)
                throw new ArgumentException("La membresía indicada no existe o está inactiva.");
        }

        if (request.EntrenadorId.HasValue)
            await ValidarEntrenadorActivoAsync(request.EntrenadorId.Value);

        var hoy = DateTime.Today;

        // Transacción: usuario de acceso + socio (con su membresía) se crean juntos o no se crea nada.
        var id = await _uow.EjecutarEnTransaccionAsync(async () =>
        {
            var usuario = new Usuario
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Rol = Roles.Socio,
                Activo = true
            };
            await _usuarios.AgregarAsync(usuario);
            await _uow.GuardarAsync();

            var socio = new Socio
            {
                UsuarioId = usuario.Id,
                EntrenadorId = request.EntrenadorId,
                MembresiaId = membresia?.Id,
                Nombre = request.Nombre,
                Documento = request.Documento,
                Telefono = request.Telefono,
                FechaRegistro = DateTime.Now,
                MembresiaInicio = membresia is null ? null : hoy,
                MembresiaFin = membresia is null ? null : hoy.AddDays(membresia.DuracionDias)
            };
            await _socios.AgregarAsync(socio);
            await _uow.GuardarAsync();

            return socio.Id;
        });

        return await ObtenerSinAutorizarAsync(id);
    }

    public async Task<PaginadoResponse<SocioResponse>> ListarAsync(
        ClaimsPrincipal usuario, int pagina, int tamanio, string? buscar, bool? soloVigentes)
    {
        var consulta = _socios.Consultar().AsNoTracking();

        // Un ENTRENADOR solo ve a los socios que tiene asignados.
        if (usuario.ObtenerRol() == Roles.Entrenador)
        {
            var entrenadorId = usuario.ObtenerPerfilId();
            consulta = consulta.Where(s => s.EntrenadorId == entrenadorId);
        }

        if (!string.IsNullOrWhiteSpace(buscar))
            consulta = consulta.Where(s => s.Nombre.Contains(buscar) || s.Documento.Contains(buscar));

        if (soloVigentes == true)
        {
            var hoy = DateTime.Today;
            consulta = consulta.Where(s => s.MembresiaFin != null && s.MembresiaFin >= hoy);
        }

        return await consulta.OrderBy(s => s.Id).Select(Proyeccion).PaginarAsync(pagina, tamanio);
    }

    public async Task<SocioResponse> ObtenerAsync(int id, ClaimsPrincipal usuario)
    {
        await _acceso.ObtenerSocioAutorizadoAsync(id, usuario);
        return await ObtenerSinAutorizarAsync(id);
    }

    public async Task<SocioResponse> ActualizarAsync(int id, SocioActualizarRequest request)
    {
        var socio = await ObtenerEntidadAsync(id);
        socio.Nombre = request.Nombre;
        socio.Telefono = request.Telefono;
        await _uow.GuardarAsync();

        return await ObtenerSinAutorizarAsync(id);
    }

    public async Task<SocioResponse> AsignarEntrenadorAsync(int id, int entrenadorId)
    {
        var socio = await ObtenerEntidadAsync(id);
        await ValidarEntrenadorActivoAsync(entrenadorId);

        socio.EntrenadorId = entrenadorId;
        await _uow.GuardarAsync();

        return await ObtenerSinAutorizarAsync(id);
    }

    public async Task<SocioResponse> RenovarMembresiaAsync(int id, int membresiaId)
    {
        var socio = await ObtenerEntidadAsync(id);

        var membresia = await _membresias.ObtenerPorIdAsync(membresiaId);
        if (membresia is null || !membresia.Activa)
            throw new ArgumentException("La membresía indicada no existe o está inactiva.");

        // Si todavía está vigente, la nueva membresía se suma a continuación de la actual.
        var hoy = DateTime.Today;
        var inicio = socio.MembresiaFin.HasValue && socio.MembresiaFin.Value > hoy
            ? socio.MembresiaFin.Value
            : hoy;

        socio.MembresiaId = membresia.Id;
        socio.MembresiaInicio = inicio;
        socio.MembresiaFin = inicio.AddDays(membresia.DuracionDias);
        await _uow.GuardarAsync();

        return await ObtenerSinAutorizarAsync(id);
    }

    public async Task DesactivarAsync(int id)
    {
        var socio = await _socios.Consultar()
            .Include(s => s.Usuario)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException($"No existe un socio con id {id}.");

        socio.Usuario.Activo = false;
        await _uow.GuardarAsync();
    }

    private async Task<Socio> ObtenerEntidadAsync(int id)
    {
        return await _socios.ObtenerPorIdAsync(id)
            ?? throw new KeyNotFoundException($"No existe un socio con id {id}.");
    }

    private async Task ValidarEntrenadorActivoAsync(int entrenadorId)
    {
        var existe = await _entrenadores.Consultar()
            .AnyAsync(e => e.Id == entrenadorId && e.Usuario.Activo);
        if (!existe)
            throw new ArgumentException("El entrenador indicado no existe o está dado de baja.");
    }

    private async Task<SocioResponse> ObtenerSinAutorizarAsync(int id)
    {
        return await _socios.Consultar().AsNoTracking()
            .Where(s => s.Id == id)
            .Select(Proyeccion)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"No existe un socio con id {id}.");
    }
}
