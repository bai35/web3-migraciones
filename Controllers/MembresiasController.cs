using GimnasioApi.Dtos;
using GimnasioApi.Seguridad;
using GimnasioApi.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GimnasioApi.Controllers;

/// <summary>Gestión de membresías (tipos de plan de pago). Registrar y modificar: solo ADMIN.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MembresiasController : ControllerBase
{
    private readonly IMembresiaServicio _servicio;

    public MembresiasController(IMembresiaServicio servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Registrar una membresía.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Registrar([FromBody] MembresiaRequest request)
    {
        var membresia = await _servicio.CrearAsync(request);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = membresia.Id }, membresia);
    }

    /// <summary>Listar membresías (paginado). ADMIN y ENTRENADOR.</summary>
    [HttpGet]
    [Authorize(Roles = Roles.Admin + "," + Roles.Entrenador)]
    public async Task<IActionResult> Listar(
        [FromQuery] int pagina = 1, [FromQuery] int tamanio = 5, [FromQuery] bool soloActivas = false)
    {
        return Ok(await _servicio.ListarAsync(pagina, tamanio, soloActivas));
    }

    /// <summary>Consultar una membresía por id. ADMIN y ENTRENADOR.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = Roles.Admin + "," + Roles.Entrenador)]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        return Ok(await _servicio.ObtenerAsync(id));
    }

    /// <summary>Modificar nombre, duración o precio.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] MembresiaRequest request)
    {
        return Ok(await _servicio.ActualizarAsync(id, request));
    }

    /// <summary>Desactivar una membresía (ya no se podrá asignar a nuevos socios).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Desactivar(int id)
    {
        await _servicio.DesactivarAsync(id);
        return NoContent();
    }
}
