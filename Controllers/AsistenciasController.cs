using GimnasioApi.Dtos;
using GimnasioApi.Seguridad;
using GimnasioApi.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GimnasioApi.Controllers;

/// <summary>Registro de asistencias al gimnasio.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AsistenciasController : ControllerBase
{
    private readonly IAsistenciaServicio _servicio;

    public AsistenciasController(IAsistenciaServicio servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Registrar la asistencia de un socio. ADMIN y ENTRENADOR (solo con sus socios).</summary>
    /// <remarks>
    /// Reglas: el socio debe estar activo, con membresía vigente, y no puede registrarse dos veces el mismo día (409).
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = Roles.Admin + "," + Roles.Entrenador)]
    public async Task<IActionResult> Registrar([FromBody] AsistenciaCrearRequest request)
    {
        var asistencia = await _servicio.RegistrarAsync(request, User);
        return StatusCode(StatusCodes.Status201Created, asistencia);
    }

    /// <summary>Historial de asistencias del socio que inició sesión.</summary>
    [HttpGet("mi-historial")]
    [Authorize(Roles = Roles.Socio)]
    public async Task<IActionResult> MiHistorial(
        [FromQuery] DateTime? desde = null, [FromQuery] DateTime? hasta = null,
        [FromQuery] int pagina = 1, [FromQuery] int tamanio = 5)
    {
        return Ok(await _servicio.HistorialAsync(User.ObtenerPerfilId(), User, desde, hasta, pagina, tamanio));
    }
}
