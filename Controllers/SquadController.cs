using System.ComponentModel;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SquadIA.Data;
using SquadIA.Models;
using SquadIA.Services;

namespace SquadIA.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SquadController : ControllerBase
{
    private readonly IAService _iaService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SquadController> _logger;
    private readonly IMemoryCache _cache;

    public SquadController(
        IAService iaService,
        ApplicationDbContext context,
        ILogger<SquadController> logger,
        IMemoryCache cache)
    {
        _iaService = iaService;
        _context = context;
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// Analisa uma squad com apoio de IA com base nas métricas informadas.
    /// </summary>
    [HttpPost("analisar")]
    [EnableRateLimiting("openai")]
    [ProducesResponseType(typeof(AnaliseResultado), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
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

        InvalidarCacheDashboard();

        _logger.LogInformation(
            "Análise salva com sucesso para squad {NomeSquad} com prioridade {Prioridade}.",
            historico.NomeSquad,
            historico.Prioridade);

        return Ok(resultado);
    }

    /// <summary>
    /// Lista o histórico de análises com suporte a filtro e paginação.
    /// </summary>
    [HttpGet("historico")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> ListarHistorico(
        [FromQuery, Description("Filtro opcional por nome da squad. Ex.: Payments")] string? nomeSquad,
        [FromQuery, Description("Data inicial no formato yyyy-MM-dd. Ex.: 2026-04-01")] DateTime? dataInicial,
        [FromQuery, Description("Data final no formato yyyy-MM-dd. Ex.: 2026-04-30")] DateTime? dataFinal,
        [FromQuery, Description("Página atual. Padrão: 1")] int pagina = 1,
        [FromQuery, Description("Tamanho da página. Máximo: 100")] int tamanhoPagina = 10,
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

    /// <summary>
    /// Busca um item específico do histórico pelo identificador.
    /// </summary>
    [HttpGet("historico/{id:int}")]
    [ProducesResponseType(typeof(HistoricoAnalise), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    /// <summary>
    /// Deleta uma análise do histórico pelo identificador.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletarAnalise(
        int id,
        CancellationToken cancellationToken)
    {
        var analise = await _context.HistoricosAnalise
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (analise is null)
            return NotFound("Análise não encontrada.");

        _context.HistoricosAnalise.Remove(analise);
        await _context.SaveChangesAsync(cancellationToken);

        InvalidarCacheDashboard();

        _logger.LogInformation(
            "Análise {Id} removida do histórico. Squad: {NomeSquad}.",
            analise.Id,
            analise.NomeSquad);

        return NoContent();
    }

    /// <summary>
    /// Retorna um resumo agregado das análises para uso em dashboard.
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardResumo), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardResumo>> ObterDashboard(
        [FromQuery, Description("Filtro opcional por nome da squad. Ex.: Payments")] string? nomeSquad,
        [FromQuery, Description("Data inicial no formato yyyy-MM-dd. Ex.: 2026-04-01")] DateTime? dataInicial,
        [FromQuery, Description("Data final no formato yyyy-MM-dd. Ex.: 2026-04-30")] DateTime? dataFinal,
        CancellationToken cancellationToken)
    {
        var cacheKey = GerarChaveCacheDashboard(nomeSquad, dataInicial, dataFinal);

        if (_cache.TryGetValue(cacheKey, out DashboardResumo? dashboardCacheado) && dashboardCacheado is not null)
        {
            _logger.LogInformation("Dashboard retornado do cache. Key: {CacheKey}", cacheKey);
            return Ok(dashboardCacheado);
        }

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

        DashboardResumo dashboard;

        if (historicos.Count == 0)
        {
            dashboard = new DashboardResumo
            {
                TotalAnalises = 0,
                MediaScoreSaude = 0,
                PrioridadeAlta = 0,
                PrioridadeMedia = 0,
                PrioridadeBaixa = 0,
                UltimaSquadAnalisada = null,
                UltimaAnaliseEm = null
            };
        }
        else
        {
            var ultima = historicos.First();

            dashboard = new DashboardResumo
            {
                TotalAnalises = historicos.Count,
                MediaScoreSaude = Math.Round(historicos.Average(x => x.ScoreSaude), 2),
                PrioridadeAlta = historicos.Count(x => NormalizarPrioridade(x.Prioridade) == "Alta"),
                PrioridadeMedia = historicos.Count(x => NormalizarPrioridade(x.Prioridade) == "Media"),
                PrioridadeBaixa = historicos.Count(x => NormalizarPrioridade(x.Prioridade) == "Baixa"),
                UltimaSquadAnalisada = ultima.NomeSquad,
                UltimaAnaliseEm = ultima.CriadoEm
            };
        }

        _cache.Set(
            cacheKey,
            dashboard,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

        _logger.LogInformation("Dashboard calculado e salvo no cache. Key: {CacheKey}", cacheKey);

        return Ok(dashboard);
    }

    /// <summary>
    /// Exporta o histórico de análises em formato CSV.
    /// </summary>
    [HttpGet("exportar")]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportarHistorico(
        [FromQuery, Description("Filtro opcional por nome da squad. Ex.: Payments")] string? nomeSquad,
        [FromQuery, Description("Data inicial no formato yyyy-MM-dd. Ex.: 2026-04-01")] DateTime? dataInicial,
        [FromQuery, Description("Data final no formato yyyy-MM-dd. Ex.: 2026-04-30")] DateTime? dataFinal,
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

    private void InvalidarCacheDashboard()
    {
        _cache.Remove("dashboard:all:null:null");
        _cache.Remove("dashboard:all");
        _logger.LogInformation("Cache padrão do dashboard invalidado.");
    }

    private static string GerarChaveCacheDashboard(string? nomeSquad, DateTime? dataInicial, DateTime? dataFinal)
    {
        var nome = string.IsNullOrWhiteSpace(nomeSquad) ? "all" : nomeSquad.Trim().ToLower();
        var inicio = dataInicial?.ToString("yyyyMMdd") ?? "null";
        var fim = dataFinal?.ToString("yyyyMMdd") ?? "null";

        return $"dashboard:{nome}:{inicio}:{fim}";
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