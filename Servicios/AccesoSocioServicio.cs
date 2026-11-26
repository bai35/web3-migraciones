using System.Security.Claims;
using GimnasioApi.Excepciones;
using GimnasioApi.Models;
using GimnasioApi.Repositorios;
using GimnasioApi.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GimnasioApi.Servicios;

public interface IAccesoSocioServicio
{
    /// <summary>Devuelve el socio si existe y el usuario tiene permiso sobre él (404 / 403 en caso contrario).</summary>
    Task<Socio> ObtenerSocioAutorizadoAsync(int socioId, ClaimsPrincipal usuario);
}

public class AccesoSocioServicio : IAccesoSocioServicio
{
    private readonly IRepositorio<Socio> _socios;
    private readonly IAuthorizationService _autorizacion;

    public AccesoSocioServicio(IRepositorio<Socio> socios, IAuthorizationService autorizacion)
    {
        _socios = socios;
        _autorizacion = autorizacion;
    }

    public async Task<Socio> ObtenerSocioAutorizadoAsync(int socioId, ClaimsPrincipal usuario)
    {
        var socio = await _socios.Consultar()
            .Include(s => s.Usuario)
            .FirstOrDefaultAsync(s => s.Id == socioId)
            ?? throw new KeyNotFoundException($"No existe un socio con id {socioId}.");

        var resultado = await _autorizacion.AuthorizeAsync(usuario, socio, AccesoSocioRequirement.Politica);
        if (!resultado.Succeeded)
            throw new AccesoProhibidoException("No tienes permiso para acceder a los datos de este socio.");

        return socio;
    }
}
