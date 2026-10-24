using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoFood.Api.Data;
using EcoFood.Api.Dtos;
using EcoFood.Api.Models;

namespace EcoFood.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoacoesController : ControllerBase
{
    private readonly AppDbContext _context;

    public DoacoesController(AppDbContext context) => _context = context;

    // GET api/doacoes?status=Disponivel
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DoacaoResponseDto>>> GetAll(
        [FromQuery] StatusDoacao? status)
    {
        // Include carrega o Doador junto: sem ele, DoadorNome e
        // DoadorEndereco voltariam vazios no DTO.
        var query = _context.Doacoes
            .Include(d => d.Doador)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(d => d.Status == status.Value);
        }

        var doacoes = await query
            .OrderBy(d => d.ValidadeAlimento ?? DateOnly.MaxValue)
            .ToListAsync();

        return Ok(doacoes.Select(d => DoacaoResponseDto.FromEntity(d)));
    }

    // GET api/doacoes/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<DoacaoResponseDto>> GetById(int id)
    {
        var doacao = await _context.Doacoes
            .Include(d => d.Doador)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doacao is null)
        {
            return NotFound(new
            {
                mensagem = $"Doacao {id} nao encontrada."
            });
        }

        return Ok(DoacaoResponseDto.FromEntity(doacao));
    }

    // POST api/doacoes
    [HttpPost]
    public async Task<ActionResult<DoacaoResponseDto>> Create(
        [FromBody] DoacaoCreateDto dto)
    {
        var doador = await _context.Usuarios.FindAsync(dto.DoadorId);

        if (doador is null)
        {
            return BadRequest(new
            {
                mensagem = "Doador informado nao existe."
            });
        }

        if (doador.TipoUsuario != TipoUsuario.Doador)
        {
            return BadRequest(new
            {
                mensagem = "Apenas usuarios do tipo Doador podem publicar doacoes."
            });
        }

        if (dto.Quantidade <= 0)
        {
            return BadRequest(new
            {
                mensagem = "Quantidade deve ser maior que zero."
            });
        }

        var doacao = new Doacao
        {
            DoadorId = dto.DoadorId,
            Descricao = dto.Descricao,
            Categoria = dto.Categoria,
            Quantidade = dto.Quantidade,
            UnidadeMedida = string.IsNullOrWhiteSpace(dto.UnidadeMedida)
                ? "kg"
                : dto.UnidadeMedida,
            ValidadeAlimento = dto.ValidadeAlimento,
            Observacoes = dto.Observacoes,
            Status = StatusDoacao.Disponivel
        };

        _context.Doacoes.Add(doacao);
        await _context.SaveChangesAsync();

        doacao.Doador = doador; // para o DTO ter nome e endereco do doador

        return CreatedAtAction(
            nameof(GetById),
            new { id = doacao.Id },
            DoacaoResponseDto.FromEntity(doacao)
        );
    }

    // PUT api/doacoes/5/cancelar
    [HttpPut("{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar(int id)
    {
        var doacao = await _context.Doacoes.FindAsync(id);

        if (doacao is null)
        {
            return NotFound();
        }

        // Uma doacao ja reservada nao pode ser cancelada pelo doador:
        // alguem ja se comprometeu a buscar.
        if (doacao.Status != StatusDoacao.Disponivel)
        {
            return BadRequest(new
            {
                mensagem = "Somente doacoes disponiveis podem ser canceladas."
            });
        }

        doacao.Status = StatusDoacao.Cancelado;
        await _context.SaveChangesAsync();

        return NoContent(); // 204: deu certo e nao ha corpo para devolver
    }
}