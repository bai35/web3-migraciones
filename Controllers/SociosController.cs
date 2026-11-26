using GimnasioApi.Dtos;
using GimnasioApi.Seguridad;
using GimnasioApi.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GimnasioApi.Controllers;

/// <summary>Gestión de socios y consulta de su historial y plan.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SociosController : ControllerBase
{
    private readonly ISocioServicio _socios;
    private readonly IAsistenciaServicio _asistencias;
    private readonly IPlanServicio _planes;

    public SociosController(ISocioServicio socios, IAsistenciaServicio asistencias, IPlanServicio planes)
    {
        _socios = socios;
        _asistencias = asistencias;
        _planes = planes;
    }

    /// <summary>Registrar un socio (crea también su usuario de acceso). Solo ADMIN.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Registrar([FromBody] SocioCrearRequest request)
    {
        var socio = await _socios.CrearAsync(request);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = socio.Id }, socio);
    }

    /// <summary>Listar socios (paginado). ADMIN ve todos; ENTRENADOR solo los suyos.</summary>
    /// <param name="buscar">Búsqueda parcial por nombre o documento.</param>
    /// <param name="soloVigentes">true: solo socios con membresía vigente.</param>
    [HttpGet]
    [Authorize(Roles = Roles.Admin + "," + Roles.Entrenador)]
    public async Task<IActionResult> Listar(
        [FromQuery] int pagina = 1, [FromQuery] int tamanio = 5,
        [FromQuery] string? buscar = null, [FromQuery] bool? soloVigentes = null)
    {
        return Ok(await _socios.ListarAsync(User, pagina, tamanio, buscar, soloVigentes));
    }

    /// <summary>Consultar un socio. ADMIN: cualquiera; ENTRENADOR: sus socios; SOCIO: solo él mismo.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        return Ok(await _socios.ObtenerAsync(id, User));
    }

    /// <summary>Datos del socio que inició sesión.</summary>
    [HttpGet("mi-perfil")]
    [Authorize(Roles = Roles.Socio)]
    public async Task<IActionResult> MiPerfil()
    {
        return Ok(await _socios.ObtenerAsync(User.ObtenerPerfilId(), User));
    }

    /// <summary>Modificar nombre o teléfono. Solo ADMIN.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] SocioActualizarRequest request)
    {
        return Ok(await _socios.ActualizarAsync(id, request));
    }

    /// <summary>Asignar o cambiar el entrenador de un socio. Solo ADMIN.</summary>
    [HttpPut("{id:int}/entrenador")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> AsignarEntrenador(int id, [FromBody] AsignarEntrenadorRequest request)
    {
        return Ok(await _socios.AsignarEntrenadorAsync(id, request.EntrenadorId));
    }

    /// <summary>Renovar o cambiar la membresía de un socio. Solo ADMIN.</summary>
    /// <remarks>Si la membresía actual sigue vigente, la nueva empieza cuando termina la anterior.</remarks>
    [HttpPut("{id:int}/membresia")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> RenovarMembresia(int id, [FromBody] RenovarMembresiaRequest request)
    {
        return Ok(await _socios.RenovarMembresiaAsync(id, request.MembresiaId));
    }

    /// <summary>Dar de baja a un socio (baja lógica). Solo ADMIN.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Desactivar(int id)
    {
        await _socios.DesactivarAsync(id);
        return NoContent();
    }

    /// <summary>Historial de asistencias de un socio (paginado, más recientes primero).</summary>
    /// <param name="desde">Fecha inicial (opcional).</param>
    /// <param name="hasta">Fecha final, inclusive (opcional).</param>
    [HttpGet("{id:int}/asistencias")]
    public async Task<IActionResult> Historial(
        int id, [FromQuery] DateTime? desde = null, [FromQuery] DateTime? hasta = null,
        [FromQuery] int pagina = 1, [FromQuery] int tamanio = 5)
    {
        return Ok(await _asistencias.HistorialAsync(id, User, desde, hasta, pagina, tamanio));
    }

    /// <summary>Resumen de asistencias de un socio (calculado con un procedimiento almacenado).</summary>
    [HttpGet("{id:int}/asistencias/resumen")]
    public async Task<IActionResult> ResumenAsistencias(int id)
    {
        return Ok(await _asistencias.ResumenAsync(id, User));
    }

    /// <summary>Planes de entrenamiento de un socio (paginado, el más reciente primero).</summary>
    [HttpGet("{id:int}/planes")]
    public async Task<IActionResult> Planes(int id, [FromQuery] int pagina = 1, [FromQuery] int tamanio = 5)
    {
        return Ok(await _planes.ListarPorSocioAsync(id, User, pagina, tamanio));
    }

    /// <summary>Plan de entrenamiento activo de un socio.</summary>
    [HttpGet("{id:int}/plan-activo")]
    public async Task<IActionResult> PlanActivo(int id)
    {
        return Ok(await _planes.ObtenerPlanActivoAsync(id, User));
    }
}
