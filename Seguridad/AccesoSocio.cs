using GimnasioApi.Models;
using Microsoft.AspNetCore.Authorization;

namespace GimnasioApi.Seguridad;

/// <summary>
/// Autorización por recurso: decide si el usuario puede ver/modificar los datos de UN socio concreto.
///  - ADMIN: cualquier socio.
///  - ENTRENADOR: solo los socios que tiene asignados.
///  - SOCIO: únicamente sus propios datos.
/// </summary>
public class AccesoSocioRequirement : IAuthorizationRequirement
{
    public const string Politica = "AccesoSocio";
}

public class AccesoSocioHandler : AuthorizationHandler<AccesoSocioRequirement, Socio>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, AccesoSocioRequirement requirement, Socio socio)
    {
        var rol = context.User.ObtenerRol();
        var perfilId = context.User.ObtenerPerfilId();

        var permitido = rol switch
        {
            Roles.Admin => true,
            Roles.Entrenador => socio.EntrenadorId == perfilId,
            Roles.Socio => socio.Id == perfilId,
            _ => false
        };

        if (permitido) context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
