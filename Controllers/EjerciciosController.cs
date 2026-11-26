using GimnasioApi.Dtos;
using GimnasioApi.Seguridad;
using GimnasioApi.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GimnasioApi.Controllers;

/// <summary>Catálogo de ejercicios que se usan para armar los planes de entrenamiento.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EjerciciosController : ControllerBase
{
    private readonly IEjercicioServicio _servicio;

    public EjerciciosController(IEjercicioServicio servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Listar ejercicios (paginado). Cualquier usuario autenticado.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int pagina = 1, [FromQuery] int tamanio = 10, [FromQuery] string? grupoMuscular = null)
    {
        return Ok(await _servicio.ListarAsync(pagina, tamanio, grupoMuscular));
    }

    /// <summary>Registrar un ejercicio. ADMIN y ENTRENADOR.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin + "," + Roles.Entrenador)]
    public async Task<IActionResult> Registrar([FromBody] EjercicioRequest request)
    {
        var ejercicio = await _servicio.CrearAsync(request);
        return StatusCode(StatusCodes.Status201Created, ejercicio);
    }
}
