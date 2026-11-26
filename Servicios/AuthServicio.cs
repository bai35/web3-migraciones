using GimnasioApi.Dtos;
using GimnasioApi.Models;
using GimnasioApi.Repositorios;
using GimnasioApi.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GimnasioApi.Servicios;

public interface IAuthServicio
{
    Task<TokenResponse> LoginAsync(LoginRequest request);
    Task<TokenResponse> RefrescarAsync(string refreshToken);
    Task LogoutAsync(int usuarioId, string refreshToken);
}

public class AuthServicio : IAuthServicio
{
    private readonly IRepositorio<Usuario> _usuarios;
    private readonly IRepositorio<Socio> _socios;
    private readonly IRepositorio<Entrenador> _entrenadores;
    private readonly IRepositorio<RefreshToken> _tokens;
    private readonly IUnidadDeTrabajo _uow;
    private readonly IGeneradorJwt _jwt;
    private readonly JwtOpciones _opciones;

    public AuthServicio(
        IRepositorio<Usuario> usuarios,
        IRepositorio<Socio> socios,
        IRepositorio<Entrenador> entrenadores,
        IRepositorio<RefreshToken> tokens,
        IUnidadDeTrabajo uow,
        IGeneradorJwt jwt,
        IOptions<JwtOpciones> opciones)
    {
        _usuarios = usuarios;
        _socios = socios;
        _entrenadores = entrenadores;
        _tokens = tokens;
        _uow = uow;
        _jwt = jwt;
        _opciones = opciones.Value;
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await _usuarios.Consultar().FirstOrDefaultAsync(u => u.Email == email);

        // Mismo mensaje para "no existe" y "contraseña incorrecta": no revela si el correo está registrado.
        if (usuario is null || !usuario.Activo || !BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
            throw new UnauthorizedAccessException("Correo o contraseña incorrectos.");

        return await EmitirTokensAsync(usuario);
    }

    public async Task<TokenResponse> RefrescarAsync(string refreshToken)
    {
        var hash = _jwt.Hash(refreshToken);
        var guardado = await _tokens.Consultar().FirstOrDefaultAsync(t => t.TokenHash == hash)
            ?? throw new UnauthorizedAccessException("Refresh token inválido.");

        // Si llega un token que ya fue usado, alguien lo copió: se cierran todas las sesiones del usuario.
        if (guardado.Revocado)
        {
            var activos = await _tokens.Consultar()
                .Where(t => t.UsuarioId == guardado.UsuarioId && !t.Revocado)
                .ToListAsync();
            foreach (var t in activos) t.Revocado = true;
            await _uow.GuardarAsync();

            throw new UnauthorizedAccessException("Refresh token ya utilizado. Se cerraron todas las sesiones por seguridad.");
        }

        if (guardado.Expira <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("El refresh token expiró. Inicia sesión nuevamente.");

        var usuario = await _usuarios.ObtenerPorIdAsync(guardado.UsuarioId);
        if (usuario is null || !usuario.Activo)
            throw new UnauthorizedAccessException("El usuario ya no está activo.");

        // Rotación: el token usado se revoca y se entrega uno nuevo (ambos cambios se guardan juntos).
        guardado.Revocado = true;
        return await EmitirTokensAsync(usuario);
    }

    public async Task LogoutAsync(int usuarioId, string refreshToken)
    {
        var hash = _jwt.Hash(refreshToken);
        var guardado = await _tokens.Consultar()
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.UsuarioId == usuarioId);

        if (guardado is null || guardado.Revocado) return;

        guardado.Revocado = true;
        await _uow.GuardarAsync();
    }

    private async Task<TokenResponse> EmitirTokensAsync(Usuario usuario)
    {
        var perfilId = await ObtenerPerfilIdAsync(usuario);
        var (accessToken, expiraAccess) = _jwt.GenerarAccessToken(usuario, perfilId);

        var refreshPlano = _jwt.GenerarRefreshToken();
        var expiraRefresh = DateTime.UtcNow.AddDays(_opciones.DiasRefresh);

        await _tokens.AgregarAsync(new RefreshToken
        {
            UsuarioId = usuario.Id,
            TokenHash = _jwt.Hash(refreshPlano),
            Creado = DateTime.UtcNow,
            Expira = expiraRefresh,
            Revocado = false
        });
        await _uow.GuardarAsync();

        return new TokenResponse
        {
            AccessToken = accessToken,
            AccessTokenExpiraUtc = expiraAccess,
            RefreshToken = refreshPlano,
            RefreshTokenExpiraUtc = expiraRefresh,
            Rol = usuario.Rol,
            PerfilId = perfilId
        };
    }

    private async Task<int> ObtenerPerfilIdAsync(Usuario usuario)
    {
        if (usuario.Rol == Roles.Socio)
            return await _socios.Consultar().Where(s => s.UsuarioId == usuario.Id)
                .Select(s => s.Id).FirstOrDefaultAsync();

        if (usuario.Rol == Roles.Entrenador)
            return await _entrenadores.Consultar().Where(e => e.UsuarioId == usuario.Id)
                .Select(e => e.Id).FirstOrDefaultAsync();

        return 0; // ADMIN no tiene perfil
    }
}
