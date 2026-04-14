using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SquadIA.Data;
using SquadIA.Models;
using SquadIA.Services;

namespace SquadIA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SquadController : ControllerBase
{
    private readonly IAService _iaService;
    private readonly ApplicationDbContext _context;

    public SquadController(IAService iaService, ApplicationDbContext context)
    {
        _iaService = iaService;
        _context = context;
    }

    [HttpPost("analisar")]
    public async Task<ActionResult<AnaliseResultado>> Analisar([FromBody] SquadMetrics squad)
    {
        if (string.IsNullOrWhiteSpace(squad.NomeSquad))
            return BadRequest("NomeSquad é obrigatório.");

        var resultado = await _iaService.AnalisarSquadAsync(squad);

        var historico = new HistoricoAnalise
        {
            NomeSquad = squad.NomeSquad,
            LeadTimeMedio = squad.LeadTimeMedio,
            Throughput = squad.Throughput,
            Bugs = squad.Bugs,
            Bloqueios = squad.Bloqueios,
            Diagnostico = resultado.Diagnostico,
            Prioridade = resultado.Prioridade,
            ResumoExecutivo = resultado.ResumoExecutivo,
            ScoreSaude = resultado.ScoreSaude,
            CriadoEm = DateTime.UtcNow
        };

        _context.HistoricosAnalise.Add(historico);
        await _context.SaveChangesAsync();

        return Ok(resultado);
    }

    [HttpGet("historico")]
    public async Task<ActionResult<object>> ListarHistorico(
        [FromQuery] string? nomeSquad,
        [FromQuery] DateTime? dataInicial,
        [FromQuery] DateTime? dataFinal,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 10)
    {
        if (pagina < 1)
            pagina = 1;

        if (tamanhoPagina < 1)
            tamanhoPagina = 10;

        if (tamanhoPagina > 100)
            tamanhoPagina = 100;

        var query = _context.HistoricosAnalise.AsQueryable();

        if (!string.IsNullOrWhiteSpace(nomeSquad))
        {
            var nomeNormalizado = nomeSquad.Trim().ToLower();
            query = query.Where(x => x.NomeSquad.ToLower().Contains(nomeNormalizado));
        }

        if (dataInicial.HasValue)
        {
            var inicio = dataInicial.Value.Date;
            query = query.Where(x => x.CriadoEm >= inicio);
        }

        if (dataFinal.HasValue)
        {
            var fim = dataFinal.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.CriadoEm <= fim);
        }

        var totalItens = await query.CountAsync();

        var itens = await query
            .OrderByDescending(x => x.CriadoEm)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync();

        var totalPaginas = (int)Math.Ceiling(totalItens / (double)tamanhoPagina);

        return Ok(new
        {
            paginaAtual = pagina,
            tamanhoPagina,
            totalItens,
            totalPaginas,
            itens
        });
    }

    [HttpGet("historico/{id:int}")]
    public async Task<ActionResult<HistoricoAnalise>> ObterHistoricoPorId(int id)
    {
        var item = await _context.HistoricosAnalise.FindAsync(id);

        if (item is null)
            return NotFound();

        return Ok(item);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardResumo>> ObterDashboard(
        [FromQuery] string? nomeSquad,
        [FromQuery] DateTime? dataInicial,
        [FromQuery] DateTime? dataFinal)
    {
        var query = _context.HistoricosAnalise.AsQueryable();

        if (!string.IsNullOrWhiteSpace(nomeSquad))
        {
            var nomeNormalizado = nomeSquad.Trim().ToLower();
            query = query.Where(x => x.NomeSquad.ToLower().Contains(nomeNormalizado));
        }

        if (dataInicial.HasValue)
        {
            var inicio = dataInicial.Value.Date;
            query = query.Where(x => x.CriadoEm >= inicio);
        }

        if (dataFinal.HasValue)
        {
            var fim = dataFinal.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.CriadoEm <= fim);
        }

        var historicos = await query
            .OrderByDescending(x => x.CriadoEm)
            .ToListAsync();

        if (historicos.Count == 0)
        {
            return Ok(new DashboardResumo
            {
                TotalAnalises = 0,
                MediaScoreSaude = 0,
                PrioridadeAlta = 0,
                PrioridadeMedia = 0,
                PrioridadeBaixa = 0,
                UltimaSquadAnalisada = null,
                UltimaAnaliseEm = null
            });
        }

        var ultima = historicos.First();

        var dashboard = new DashboardResumo
        {
            TotalAnalises = historicos.Count,
            MediaScoreSaude = Math.Round(historicos.Average(x => x.ScoreSaude), 2),
            PrioridadeAlta = historicos.Count(x => x.Prioridade == "Alta"),
            PrioridadeMedia = historicos.Count(x => x.Prioridade == "Média" || x.Prioridade == "Media"),
            PrioridadeBaixa = historicos.Count(x => x.Prioridade == "Baixa"),
            UltimaSquadAnalisada = ultima.NomeSquad,
            UltimaAnaliseEm = ultima.CriadoEm
        };

        return Ok(dashboard);
    }
}