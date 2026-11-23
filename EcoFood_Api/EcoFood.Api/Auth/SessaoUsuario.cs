using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EcoFood.Api.Data;
using EcoFood.Api.Models;

namespace EcoFood.Api.Auth;

internal static class SessaoUsuario
{
    private const string VersaoCredencial = "ecofood:credencial";

    public static ClaimsPrincipal CriarPrincipal(Usuario usuario, string senhaHash)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString(CultureInfo.InvariantCulture)),
            new Claim(VersaoCredencial, CalcularVersao(senhaHash))
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }

    public static async Task<Usuario?> BuscarAsync(
        ClaimsPrincipal principal,
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            return null;

        var usuario = await context.Usuarios.FindAsync([id], cancellationToken);

        return usuario?.SenhaHash is { } senhaHash &&
            principal.HasClaim(VersaoCredencial, CalcularVersao(senhaHash))
                ? usuario
                : null;
    }

    private static string CalcularVersao(string senhaHash) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(senhaHash)));
}
