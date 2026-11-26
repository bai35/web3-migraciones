using GimnasioApi.Dtos;
using GimnasioApi.Seguridad;
using GimnasioApi.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GimnasioApi.Controllers;

/// <summary>Inicio de sesión con JWT (autenticación sin estado) y renovación de tokens.</summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthServicio _servicio;

    public AuthController(IAuthServicio servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Iniciar sesión.</summary>
    /// <remarks>Devuelve un access token (corta duración) y un refresh token (para renovarlo).</remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        return Ok(await _servicio.LoginAsync(request));
    }

    /// <summary>Renovar el access token con un refresh token válido.</summary>
    /// <remarks>El refresh token usado queda revocado y se entrega uno nuevo (rotación).</remarks>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refrescar([FromBody] RefreshRequest request)
    {
        return Ok(await _servicio.RefrescarAsync(request.RefreshToken));
    }

    /// <summary>Cerrar sesión: revoca el refresh token indicado.</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
    {
        await _servicio.LogoutAsync(User.ObtenerUsuarioId(), request.RefreshToken);
        return NoContent();
    }
}
