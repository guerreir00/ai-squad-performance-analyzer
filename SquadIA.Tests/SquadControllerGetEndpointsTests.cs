using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using SquadIA.Controllers;
using SquadIA.Models;
using SquadIA.Tests.Helpers;
using Xunit;

namespace SquadIA.Tests;

public class SquadControllerGetEndpointsTests
{
    [Fact]
    public async Task ListarHistorico_Deve_retornar_itens_paginados()
    {
        using var context = TestDbContextFactory.Create();

        context.HistoricosAnalise.AddRange(
            new HistoricoAnalise
            {
                NomeSquad = "Payments Squad",
                LeadTimeMedio = 30,
                Throughput = 12,
                Bugs = 3,
                Bloqueios = 1,
                Diagnostico = "Saudável",
                Prioridade = "Baixa",
                ResumoExecutivo = "Bom fluxo.",
                ScoreSaude = 85,
                CriadoEm = DateTime.UtcNow.AddDays(-1)
            },
            new HistoricoAnalise
            {
                NomeSquad = "Marketplace Squad",
                LeadTimeMedio = 70,
                Throughput = 8,
                Bugs = 20,
                Bloqueios = 5,
                Diagnostico = "Gargalos identificados",
                Prioridade = "Alta",
                ResumoExecutivo = "Necessita atenção.",
                ScoreSaude = 40,
                CriadoEm = DateTime.UtcNow
            }
        );

        await context.SaveChangesAsync();

        var cache = new MemoryCache(new MemoryCacheOptions());

        var controller = new SquadController(
            null!,
            context,
            new NullLogger<SquadController>(),
            cache);

        var actionResult = await controller.ListarHistorico(
            nomeSquad: null,
            dataInicial: null,
            dataFinal: null,
            pagina: 1,
            tamanhoPagina: 1,
            cancellationToken: CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.NotNull(okResult.Value);

        var paginaAtual = (int)GetPropertyValue(okResult.Value!, "paginaAtual")!;
        var tamanhoPagina = (int)GetPropertyValue(okResult.Value!, "tamanhoPagina")!;
        var totalItens = (int)GetPropertyValue(okResult.Value!, "totalItens")!;
        var totalPaginas = (int)GetPropertyValue(okResult.Value!, "totalPaginas")!;
        var itens = Assert.IsAssignableFrom<IEnumerable<HistoricoAnalise>>(GetPropertyValue(okResult.Value!, "itens"));

        Assert.Equal(1, paginaAtual);
        Assert.Equal(1, tamanhoPagina);
        Assert.Equal(2, totalItens);
        Assert.Equal(2, totalPaginas);
        Assert.Single(itens);
    }

    [Fact]
    public async Task ListarHistorico_Deve_filtrar_por_nomeSquad()
    {
        using var context = TestDbContextFactory.Create();

        context.HistoricosAnalise.AddRange(
            new HistoricoAnalise
            {
                NomeSquad = "Payments Squad",
                LeadTimeMedio = 30,
                Throughput = 12,
                Bugs = 3,
                Bloqueios = 1,
                Diagnostico = "Saudável",
                Prioridade = "Baixa",
                ResumoExecutivo = "Bom fluxo.",
                ScoreSaude = 85,
                CriadoEm = DateTime.UtcNow.AddDays(-1)
            },
            new HistoricoAnalise
            {
                NomeSquad = "Marketplace Squad",
                LeadTimeMedio = 70,
                Throughput = 8,
                Bugs = 20,
                Bloqueios = 5,
                Diagnostico = "Gargalos identificados",
                Prioridade = "Alta",
                ResumoExecutivo = "Necessita atenção.",
                ScoreSaude = 40,
                CriadoEm = DateTime.UtcNow
            }
        );

        await context.SaveChangesAsync();

        var cache = new MemoryCache(new MemoryCacheOptions());

        var controller = new SquadController(
            null!,
            context,
            new NullLogger<SquadController>(),
            cache);

        var actionResult = await controller.ListarHistorico(
            nomeSquad: "Payments",
            dataInicial: null,
            dataFinal: null,
            pagina: 1,
            tamanhoPagina: 10,
            cancellationToken: CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.NotNull(okResult.Value);

        var totalItens = (int)GetPropertyValue(okResult.Value!, "totalItens")!;
        var itens = Assert.IsAssignableFrom<IEnumerable<HistoricoAnalise>>(GetPropertyValue(okResult.Value!, "itens"));
        var primeiroItem = Assert.Single(itens);

        Assert.Equal(1, totalItens);
        Assert.Equal("Payments Squad", primeiroItem.NomeSquad);
    }

    [Fact]
    public async Task Dashboard_Deve_retornar_resumo_correto()
    {
        using var context = TestDbContextFactory.Create();

        context.HistoricosAnalise.AddRange(
            new HistoricoAnalise
            {
                NomeSquad = "Payments Squad",
                LeadTimeMedio = 30,
                Throughput = 12,
                Bugs = 3,
                Bloqueios = 1,
                Diagnostico = "Saudável",
                Prioridade = "Baixa",
                ResumoExecutivo = "Bom fluxo.",
                ScoreSaude = 85,
                CriadoEm = DateTime.UtcNow.AddDays(-2)
            },
            new HistoricoAnalise
            {
                NomeSquad = "Payments Squad",
                LeadTimeMedio = 60,
                Throughput = 9,
                Bugs = 10,
                Bloqueios = 3,
                Diagnostico = "Atenção",
                Prioridade = "Media",
                ResumoExecutivo = "Necessita ajustes.",
                ScoreSaude = 60,
                CriadoEm = DateTime.UtcNow.AddDays(-1)
            },
            new HistoricoAnalise
            {
                NomeSquad = "Marketplace Squad",
                LeadTimeMedio = 80,
                Throughput = 7,
                Bugs = 20,
                Bloqueios = 6,
                Diagnostico = "Crítica",
                Prioridade = "Alta",
                ResumoExecutivo = "Necessita atenção imediata.",
                ScoreSaude = 30,
                CriadoEm = DateTime.UtcNow
            }
        );

        await context.SaveChangesAsync();

        var cache = new MemoryCache(new MemoryCacheOptions());

        var controller = new SquadController(
            null!,
            context,
            new NullLogger<SquadController>(),
            cache);

        var actionResult = await controller.ObterDashboard(
            nomeSquad: null,
            dataInicial: null,
            dataFinal: null,
            cancellationToken: CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var dashboard = Assert.IsType<DashboardResumo>(okResult.Value);

        Assert.Equal(3, dashboard.TotalAnalises);
        Assert.Equal(58.33, dashboard.MediaScoreSaude);
        Assert.Equal(1, dashboard.PrioridadeAlta);
        Assert.Equal(1, dashboard.PrioridadeMedia);
        Assert.Equal(1, dashboard.PrioridadeBaixa);
        Assert.Equal("Marketplace Squad", dashboard.UltimaSquadAnalisada);
        Assert.NotNull(dashboard.UltimaAnaliseEm);
    }

    private static object? GetPropertyValue(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return property!.GetValue(source);
    }
}