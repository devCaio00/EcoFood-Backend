using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using MySqlConnector;
using Microsoft.EntityFrameworkCore;
using EcoFood.Api.Auth;
using EcoFood.Api.Data;
using EcoFood.Api.Dtos;
using EcoFood.Api.Models;

namespace EcoFood.Api.Controllers;

[ApiController]
[Route("api/[controller]")] // vira "api/usuarios"
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _context;

    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public UsuariosController(AppDbContext context, IPasswordHasher<Usuario>? passwordHasher = null)
    {
        _context = context;
        _passwordHasher = passwordHasher ?? new PasswordHasher<Usuario>();
    }

    // GET api/usuarios?tipo=Receptor
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> GetAll(
        [FromQuery] TipoUsuario? tipo)
    {
        var query = _context.Usuarios.AsQueryable();

        if (tipo.HasValue)
        {
            query = query.Where(u => u.TipoUsuario == tipo.Value);
        }

        var usuarios = await query
            .OrderBy(u => u.Nome)
            .Select(u => UsuarioResponseDto.FromEntity(u))
            .ToListAsync();

        return Ok(usuarios);
    }

    // GET api/usuarios/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioResponseDto>> GetById(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);

        if (usuario is null)
        {
            return NotFound(new
            {
                mensagem = $"Usuario {id} nao encontrado."
            });
        }

        return Ok(UsuarioResponseDto.FromEntity(usuario));
    }

    // POST api/usuarios
    [HttpPost]
    public async Task<ActionResult<UsuarioResponseDto>> Create(
        [FromBody] UsuarioCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome) ||
            string.IsNullOrWhiteSpace(dto.Email))
        {
            return BadRequest(new
            {
                mensagem = "Nome e e-mail sao obrigatorios."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.Endereco))
        {
            return BadRequest(new
            {
                mensagem = "Endereco e obrigatorio."
            });
        }

        if (dto.TipoUsuario is not (TipoUsuario.Doador or TipoUsuario.Receptor))
        {
            return BadRequest(new { mensagem = "Escolha o tipo Doador ou Receptor." });
        }

        if (dto.Senha is not null && string.IsNullOrWhiteSpace(dto.Senha))
        {
            return BadRequest(new { mensagem = "A senha precisa conter pelo menos um caractere diferente de espaco." });
        }

        var email = dto.Email.Trim().ToLowerInvariant();
        var jaExiste = await _context.Usuarios
            .AnyAsync(u => u.Email.ToLower() == email);

        if (jaExiste)
        {
            return Conflict(new
            {
                mensagem = "Ja existe um usuario com esse e-mail."
            });
        }

        var usuario = new Usuario
        {
            Nome = dto.Nome,
            Email = email,
            Telefone = dto.Telefone,
            TipoUsuario = dto.TipoUsuario,
            Endereco = dto.Endereco
        };

        if (dto.Senha is { } senha)
        {
            usuario.SenhaHash = _passwordHasher.HashPassword(usuario, senha);
        }

        _context.Usuarios.Add(usuario);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry })
        {
            return Conflict(new { mensagem = "Ja existe um usuario com esse e-mail." });
        }

        // 201 Created com o cabecalho Location apontando para o novo recurso
        return CreatedAtAction(
            nameof(GetById),
            new { id = usuario.Id },
            UsuarioResponseDto.FromEntity(usuario)
        );
    }


    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UsuarioResponseDto>> Update(
        int id, [FromBody] UsuarioUpdateDto dto, CancellationToken cancellationToken)
    {
        var usuario = await SessaoUsuario.BuscarAsync(User, _context, cancellationToken);
        if (usuario is null)
            return Unauthorized();

        if (usuario.Id != id)
            return Forbid();

        var email = dto.Email.Trim().ToLowerInvariant();
        var mudouEmail = !string.Equals(usuario.Email, email, StringComparison.OrdinalIgnoreCase);

        if (dto.NovaSenha is not null && string.IsNullOrWhiteSpace(dto.NovaSenha))
        {
            return BadRequest(new { mensagem = "A senha precisa conter pelo menos um caractere diferente de espaco." });
        }

        if ((mudouEmail || dto.NovaSenha is not null) &&
            (dto.SenhaAtual is null || _passwordHasher.VerifyHashedPassword(usuario, usuario.SenhaHash!, dto.SenhaAtual)
                == PasswordVerificationResult.Failed))
        {
            return BadRequest(new { mensagem = "Informe a senha atual valida para alterar o e-mail ou a senha." });
        }

        if (mudouEmail && await _context.Usuarios.AnyAsync(
            u => u.Id != id && u.Email.ToLower() == email, cancellationToken))
        {
            return Conflict(new { mensagem = "Ja existe um usuario com esse e-mail." });
        }

        usuario.Nome = dto.Nome.Trim();
        usuario.Email = email;
        usuario.Telefone = dto.Telefone;
        usuario.Endereco = dto.Endereco.Trim();

        if (dto.NovaSenha is { } novaSenha)
        {
            usuario.SenhaHash = _passwordHasher.HashPassword(usuario, novaSenha);
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { mensagem = "A conta foi alterada. Entre novamente e tente de novo." });
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1062 })
        {
            return Conflict(new { mensagem = "Ja existe um usuario com esse e-mail." });
        }

        return Ok(UsuarioResponseDto.FromEntity(usuario));
    }


    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var usuario = await SessaoUsuario.BuscarAsync(User, _context, cancellationToken);
        if (usuario is null)
            return Unauthorized();

        if (usuario.Id != id)
            return Forbid();

        if (await _context.Doacoes.AnyAsync(d => d.DoadorId == id, cancellationToken) ||
            await _context.Reservas.AnyAsync(r => r.ReceptorId == id, cancellationToken))
        {
            return Conflict(new { mensagem = "A conta tem doacoes ou reservas vinculadas." });
        }

        _context.Usuarios.Remove(usuario);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { mensagem = "A conta foi alterada. Entre novamente e tente de novo." });
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { ErrorCode: MySqlErrorCode.RowIsReferenced2 })
        {
            return Conflict(new { mensagem = "A conta tem doacoes ou reservas vinculadas." });
        }

        return NoContent();
    }
}
