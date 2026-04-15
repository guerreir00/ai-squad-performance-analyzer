using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<SquadController> _logger;

    public SquadController(
        IAService iaService,
        ApplicationDbContext context,
        ILogger<SquadController> logger)
    {
        _iaService = iaService;
        _context = context;
        _logger = logger;
    }

    [HttpPost("analisar")]
    [EnableRateLimiting("openai")]
    public async Task<ActionResult<AnaliseResultado>> Analisar(
        [FromBody] SquadMetrics squad,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (string.IsNullOrWhiteSpace(squad.NomeSquad))
            return BadRequest("NomeSquad é obrigatório.");

        var nomeSquadNormalizado = squad.NomeSquad.Trim();

        var squadNormalizada = new SquadMetrics
        {
            NomeSquad = nomeSquadNormalizado,
            LeadTimeMedio = squad.LeadTimeMedio,
            Throughput = squad.Throughput,
            Bugs = squad.Bugs,
            Bloqueios = squad.Bloqueios
        };

        var resultado = await _iaService.AnalisarSquadAsync(squadNormalizada, cancellationToken);
        resultado.Prioridade = NormalizarPrioridade(resultado.Prioridade);

        var historico = new HistoricoAnalise
        {
            NomeSquad = squadNormalizada.NomeSquad,
            LeadTimeMedio = squadNormalizada.LeadTimeMedio,
            Throughput = squadNormalizada.Throughput,
            Bugs = squadNormalizada.Bugs,
            Bloqueios = squadNormalizada.Bloqueios,
            Diagnostico = resultado.Diagnostico,
            Prioridade = resultado.Prioridade,
            ResumoExecutivo = resultado.ResumoExecutivo,
            ScoreSaude = resultado.ScoreSaude,
            CriadoEm = DateTime.UtcNow
        };

        _context.HistoricosAnalise.Add(historico);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Análise salva com sucesso para squad {NomeSquad} com prioridade {Prioridade}.",
            historico.NomeSquad,
            historico.Prioridade);

        return Ok(resultado);
    }

    [HttpGet("historico")]
    public async Task<ActionResult<object>> ListarHistorico(
        [FromQuery] string? nomeSquad,
        [FromQuery] DateTime? dataInicial,
        [FromQuery] DateTime? dataFinal,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 10,
        CancellationToken cancellationToken = default)
    {
        if (pagina < 1)
            pagina = 1;

        if (tamanhoPagina < 1)
            tamanhoPagina = 10;

        if (tamanhoPagina > 100)
            tamanhoPagina = 100;

        var query = _context.HistoricosAnalise.AsNoTracking().AsQueryable();

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

        var totalItens = await query.CountAsync(cancellationToken);

        var itens = await query
            .OrderByDescending(x => x.CriadoEm)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

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
    public async Task<ActionResult<HistoricoAnalise>> ObterHistoricoPorId(
        int id,
        CancellationToken cancellationToken)
    {
        var item = await _context.HistoricosAnalise
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (item is null)
            return NotFound();

        return Ok(item);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardResumo>> ObterDashboard(
        [FromQuery] string? nomeSquad,
        [FromQuery] DateTime? dataInicial,
        [FromQuery] DateTime? dataFinal,
        CancellationToken cancellationToken)
    {
        var query = _context.HistoricosAnalise.AsNoTracking().AsQueryable();

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
            .ToListAsync(cancellationToken);

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
            PrioridadeAlta = historicos.Count(x => NormalizarPrioridade(x.Prioridade) == "Alta"),
            PrioridadeMedia = historicos.Count(x => NormalizarPrioridade(x.Prioridade) == "Media"),
            PrioridadeBaixa = historicos.Count(x => NormalizarPrioridade(x.Prioridade) == "Baixa"),
            UltimaSquadAnalisada = ultima.NomeSquad,
            UltimaAnaliseEm = ultima.CriadoEm
        };

        return Ok(dashboard);
    }

    [HttpGet("exportar")]
    public async Task<IActionResult> ExportarHistorico(
        [FromQuery] string? nomeSquad,
        [FromQuery] DateTime? dataInicial,
        [FromQuery] DateTime? dataFinal,
        CancellationToken cancellationToken)
    {
        var query = _context.HistoricosAnalise.AsNoTracking().AsQueryable();

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

        var dados = await query
            .OrderByDescending(x => x.CriadoEm)
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder();

        csv.AppendLine("NomeSquad,LeadTime,Throughput,Bugs,Bloqueios,ScoreSaude,Prioridade,Data");

        foreach (var item in dados)
        {
            csv.AppendLine(
                $"{EscapeCsv(item.NomeSquad)}," +
                $"{item.LeadTimeMedio}," +
                $"{item.Throughput}," +
                $"{item.Bugs}," +
                $"{item.Bloqueios}," +
                $"{item.ScoreSaude}," +
                $"{EscapeCsv(NormalizarPrioridade(item.Prioridade))}," +
                $"{item.CriadoEm:yyyy-MM-dd}");
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());

        return File(bytes, "text/csv", "historico-squads.csv");
    }

    private static string NormalizarPrioridade(string? prioridade)
    {
        if (string.IsNullOrWhiteSpace(prioridade))
            return "Media";

        var valor = prioridade.Trim().ToLowerInvariant();

        return valor switch
        {
            "alta" => "Alta",
            "média" => "Media",
            "media" => "Media",
            "baixa" => "Baixa",
            _ => "Media"
        };
    }

    private static string EscapeCsv(string? valor)
    {
        if (string.IsNullOrEmpty(valor))
            return "";

        if (valor.Contains(',') || valor.Contains('"') || valor.Contains('\n'))
            return $"\"{valor.Replace("\"", "\"\"")}\"";

        return valor;
    }
}