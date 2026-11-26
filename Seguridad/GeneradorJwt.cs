using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GimnasioApi.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GimnasioApi.Seguridad;

public interface IGeneradorJwt
{
    (string Token, DateTime Expira) GenerarAccessToken(Usuario usuario, int perfilId);
    string GenerarRefreshToken();
    string Hash(string valor);
}

public class GeneradorJwt : IGeneradorJwt
{
    private readonly JwtOpciones _opciones;

    public GeneradorJwt(IOptions<JwtOpciones> opciones)
    {
        _opciones = opciones.Value;
    }

    public (string Token, DateTime Expira) GenerarAccessToken(Usuario usuario, int perfilId)
    {
        var claims = new List<Claim>
        {
            new("sub", usuario.Id.ToString()),
            new("email", usuario.Email),
            new("role", usuario.Rol),
            new("perfilId", perfilId.ToString()),
            new("jti", Guid.NewGuid().ToString())
        };

        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opciones.Clave));
        var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);
        var expira = DateTime.UtcNow.AddMinutes(_opciones.MinutosAcceso);

        var token = new JwtSecurityToken(
            issuer: _opciones.Emisor,
            audience: _opciones.Audiencia,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expira,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }

    public string GenerarRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    // En la BD solo se guarda el hash del refresh token, nunca el token original.
    public string Hash(string valor) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(valor)));
}
