namespace GimnasioApi.Excepciones;

/// <summary>El usuario está autenticado pero no tiene permiso sobre el recurso (HTTP 403).</summary>
public class AccesoProhibidoException : Exception
{
    public AccesoProhibidoException(string mensaje) : base(mensaje) { }
}
