using GimnasioApi.Dtos;
using GimnasioApi.Seguridad;
using GimnasioApi.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GimnasioApi.Controllers;

/// <summary>Gestión de entrenadores. Solo ADMIN.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Admin)]
public class EntrenadoresController : ControllerBase
{
    private readonly IEntrenadorServicio _servicio;

    public EntrenadoresController(IEntrenadorServicio servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Registrar un entrenador (crea también su usuario de acceso).</summary>
    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] EntrenadorCrearRequest request)
    {
        var entrenador = await _servicio.CrearAsync(request);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = entrenador.Id }, entrenador);
    }

    /// <summary>Listar entrenadores (paginado).</summary>
    /// <param name="pagina">Número de página (empieza en 1).</param>
    /// <param name="tamanio">Registros por página (1 a 50).</param>
    /// <param name="buscar">Búsqueda parcial por nombre o documento.</param>
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int pagina = 1, [FromQuery] int tamanio = 5, [FromQuery] string? buscar = null)
    {
        return Ok(await _servicio.ListarAsync(pagina, tamanio, buscar));
    }

    /// <summary>Consultar un entrenador por id.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        return Ok(await _servicio.ObtenerAsync(id));
    }

    /// <summary>Modificar nombre, teléfono o especialidad.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] EntrenadorActualizarRequest request)
    {
        return Ok(await _servicio.ActualizarAsync(id, request));
    }

    /// <summary>Dar de baja a un entrenador (baja lógica).</summary>
    /// <remarks>No se permite si todavía tiene socios activos asignados (409).</remarks>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Desactivar(int id)
    {
        await _servicio.DesactivarAsync(id);
        return NoContent();
    }
}
