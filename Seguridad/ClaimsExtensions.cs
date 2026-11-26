using System.Security.Claims;

namespace GimnasioApi.Seguridad;

/// <summary>Atajos para leer los datos del usuario autenticado a partir del JWT.</summary>
public static class ClaimsExtensions
{
    public static int ObtenerUsuarioId(this ClaimsPrincipal usuario) =>
        int.TryParse(usuario.FindFirst("sub")?.Value, out var id) ? id : 0;

    public static string ObtenerRol(this ClaimsPrincipal usuario) =>
        usuario.FindFirst("role")?.Value ?? string.Empty;

    /// <summary>Id del Socio o del Entrenador asociado al usuario (0 para ADMIN).</summary>
    public static int ObtenerPerfilId(this ClaimsPrincipal usuario) =>
        int.TryParse(usuario.FindFirst("perfilId")?.Value, out var id) ? id : 0;
}
