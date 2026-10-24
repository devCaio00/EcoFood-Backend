using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoFood.Api.Data;
using EcoFood.Api.Dtos;
using EcoFood.Api.Models;

namespace EcoFood.Api.Controllers;

[ApiController]
[Route("api/[controller]")] // vira "api/usuarios"
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _context;

    // Injecao de dependencia: o ASP.NET entrega o AppDbContext pronto.
    public UsuariosController(AppDbContext context) => _context = context;

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

        var jaExiste = await _context.Usuarios
            .AnyAsync(u => u.Email == dto.Email);

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
            Email = dto.Email,
            Telefone = dto.Telefone,
            TipoUsuario = dto.TipoUsuario,
            Endereco = dto.Endereco
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        // 201 Created com o cabecalho Location apontando para o novo recurso
        return CreatedAtAction(
            nameof(GetById),
            new { id = usuario.Id },
            UsuarioResponseDto.FromEntity(usuario)
        );
    }
}