using EcoFood.Api.Auth;
using EcoFood.Api.Data;
using EcoFood.Api.Dtos;
using EcoFood.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace EcoFood.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext context, IPasswordHasher<Usuario> passwordHasher) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var usuario = await context.Usuarios
            .SingleOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);

        if (usuario?.SenhaHash is not { } senhaHash ||
            passwordHasher.VerifyHashedPassword(usuario, senhaHash, dto.Senha) == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { mensagem = "E-mail ou senha invalidos." });
        }

        Response.Headers.CacheControl = "no-store";

        return SignIn(SessaoUsuario.CriarPrincipal(usuario, senhaHash), "Bearer");
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UsuarioResponseDto>> Me(CancellationToken cancellationToken)
    {
        var usuario = await SessaoUsuario.BuscarAsync(User, context, cancellationToken);
        if (usuario is null)
            return Unauthorized();

        Response.Headers.CacheControl = "no-store";
        return Ok(UsuarioResponseDto.FromEntity(usuario));
    }
}
