using GimnasioApi.Dtos;
using GimnasioApi.Seguridad;
using GimnasioApi.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GimnasioApi.Controllers;

/// <summary>Planes de entrenamiento.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlanesController : ControllerBase
{
    private readonly IPlanServicio _servicio;

    public PlanesController(IPlanServicio servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Crear un plan de entrenamiento para un socio. ADMIN y ENTRENADOR (solo con sus socios).</summary>
    /// <remarks>El plan nuevo pasa a ser el activo y el anterior queda inactivo (todo en una transacción).</remarks>
    [HttpPost]
    [Authorize(Roles = Roles.Admin + "," + Roles.Entrenador)]
    public async Task<IActionResult> Crear([FromBody] PlanCrearRequest request)
    {
        var plan = await _servicio.CrearAsync(request, User);
        return StatusCode(StatusCodes.Status201Created, plan);
    }

    /// <summary>Plan de entrenamiento activo del socio que inició sesión.</summary>
    [HttpGet("mi-plan")]
    [Authorize(Roles = Roles.Socio)]
    public async Task<IActionResult> MiPlan()
    {
        return Ok(await _servicio.ObtenerPlanActivoAsync(User.ObtenerPerfilId(), User));
    }
}
