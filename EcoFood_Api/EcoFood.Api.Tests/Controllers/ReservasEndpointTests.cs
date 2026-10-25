using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Json;
using EcoFood.Api.Dtos;
using Xunit;

namespace EcoFood.Api.Tests.Controllers;

public class ReservasEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public ReservasEndpointTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDoacoes_DeveRetornar200ComEnderecoDoDoador()
    {
        var resposta = await _client.GetAsync("/api/doacoes?status=Disponivel");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var doacoes = await resposta.Content.ReadFromJsonAsync<List<DoacaoResponseDto>>();
        
        Assert.NotNull(doacoes);
        Assert.NotEmpty(doacoes!);
        Assert.False(string.IsNullOrWhiteSpace(doacoes![0].DoadorEndereco));
    }

    [Fact]
    public async Task PostReserva_DeveRetornar400_SemConfirmacaoDeEndereco()
    {
        var payload = new { doacaoId = 1, receptorId = 2, enderecoConfirmado = false };

        var resposta = await _client.PostAsJsonAsync("/api/reservas", payload);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var corpo = await resposta.Content.ReadAsStringAsync();
        Assert.Contains("confirmar o endereco", corpo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostReserva_DeveRetornar201_ComConfirmacaoDeEndereco()
    {
        var payload = new { doacaoId = 1, receptorId = 2, enderecoConfirmado = true };

        var resposta = await _client.PostAsJsonAsync("/api/reservas", payload);

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var criada = await resposta.Content.ReadFromJsonAsync<ReservaResponseDto>();
        
        Assert.NotNull(criada);
        Assert.True(criada!.EnderecoConfirmado);
        Assert.False(string.IsNullOrWhiteSpace(criada.EnderecoRetiradaSnapshot));
    }
}