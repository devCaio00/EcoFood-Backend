using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoFood.Api.Data;
using EcoFood.Api.Dtos;
using EcoFood.Api.Models;

namespace EcoFood.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservasController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReservasController(AppDbContext context) => _context = context;

    // GET api/reservas?receptorId=4
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReservaResponseDto>>> GetAll(
        [FromQuery] int? receptorId)
    {
        var query = _context.Reservas
            .Include(r => r.Doacao)
            .Include(r => r.Receptor)
            .AsQueryable();

        if (receptorId.HasValue)
        {
            query = query.Where(r => r.ReceptorId == receptorId.Value);
        }

        var reservas = await query
            .OrderByDescending(r => r.DataReserva)
            .ToListAsync();

        return Ok(reservas.Select(ReservaResponseDto.FromEntity));
    }

    // POST api/reservas
    [HttpPost]
    public async Task<ActionResult<ReservaResponseDto>> Create(
        [FromBody] ReservaCreateDto dto)
    {
        // ---- REGRA CENTRAL DO ECOFOOD ----
        // O receptor precisa ter confirmado o endereco de retirada.
        // Validamos aqui mesmo que a tela ja valide: a tela e
        // conveniencia, a API e a garantia.
        if (!dto.EnderecoConfirmado)
        {
            return BadRequest(new
            {
                mensagem = "E necessario confirmar o endereco de retirada antes de reservar."
            });
        }

        // Transacao: garante que marcar a doacao como Reservado e criar a
        // reserva acontecam juntos. Se o segundo passo falhar, o primeiro
        // e desfeito e a doacao nao fica presa em um estado invalido.
        await using var transacao = await _context.Database.BeginTransactionAsync();

        var doacao = await _context.Doacoes
            .Include(d => d.Doador)
            .FirstOrDefaultAsync(d => d.Id == dto.DoacaoId);

        if (doacao is null)
        {
            return NotFound(new
            {
                mensagem = "Doacao nao encontrada."
            });
        }

        if (doacao.Status != StatusDoacao.Disponivel)
        {
            return Conflict(new
            {
                mensagem = "Esta doacao ja nao esta mais disponivel."
            });
        }

        var receptor = await _context.Usuarios.FindAsync(dto.ReceptorId);

        if (receptor is null)
        {
            return BadRequest(new
            {
                mensagem = "Receptor informado nao existe."
            });
        }

        if (receptor.TipoUsuario != TipoUsuario.Receptor)
        {
            return BadRequest(new
            {
                mensagem = "Apenas usuarios do tipo Receptor podem reservar."
            });
        }

        var reserva = new Reserva
        {
            DoacaoId = dto.DoacaoId,
            ReceptorId = dto.ReceptorId,
            EnderecoConfirmado = true,

            // O endereco gravado vem do cadastro do doador, NAO do cliente:
            // assim o cliente nao pode gravar um endereco diferente do real.
            EnderecoRetiradaSnapshot = doacao.Doador?.Endereco ?? string.Empty,

            Status = StatusReserva.Pendente
        };

        doacao.Status = StatusDoacao.Reservado;

        _context.Reservas.Add(reserva);
        await _context.SaveChangesAsync();
        await transacao.CommitAsync();

        reserva.Doacao = doacao;
        reserva.Receptor = receptor;

        return CreatedAtAction(
            nameof(GetAll),
            ReservaResponseDto.FromEntity(reserva)
        );
    }

    // PUT api/reservas/5/concluir
    [HttpPut("{id:int}/concluir")]
    public async Task<IActionResult> Concluir(int id)
    {
        var reserva = await _context.Reservas
            .Include(r => r.Doacao)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reserva is null)
        {
            return NotFound();
        }

        if (reserva.Status != StatusReserva.Pendente)
        {
            return BadRequest(new
            {
                mensagem = "Apenas reservas pendentes podem ser concluidas."
            });
        }

        reserva.Status = StatusReserva.Concluida;
        reserva.DataRetirada = DateTime.UtcNow;

        if (reserva.Doacao is not null)
        {
            reserva.Doacao.Status = StatusDoacao.Retirado;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PUT api/reservas/5/cancelar
    [HttpPut("{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar(int id)
    {
        var reserva = await _context.Reservas
            .Include(r => r.Doacao)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reserva is null)
        {
            return NotFound();
        }

        reserva.Status = StatusReserva.Cancelada;

        // A comida volta a circular: outra instituicao pode reservar.
        if (reserva.Doacao is not null)
        {
            reserva.Doacao.Status = StatusDoacao.Disponivel;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }
}