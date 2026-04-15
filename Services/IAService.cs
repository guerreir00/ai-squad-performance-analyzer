using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using SquadIA.Models;

namespace SquadIA.Services;

public class IAService
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<IAService> _logger;
    private static readonly TimeSpan TimeoutAnalise = TimeSpan.FromSeconds(30);

    public IAService(ChatClient chatClient, ILogger<IAService> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<AnaliseResultado> AnalisarSquadAsync(
        SquadMetrics squad,
        CancellationToken cancellationToken = default)
    {
        var prompt = @$"
Você é um coordenador de engenharia experiente.

Analise os dados da squad abaixo e responda SOMENTE em JSON válido,
sem markdown, sem texto extra, no formato:

{{
  ""diagnostico"": ""texto"",
  ""problemas"": [""item 1"", ""item 2""],
  ""acoes"": [""item 1"", ""item 2""],
  ""prioridade"": ""Alta"",
  ""resumoExecutivo"": ""texto curto e objetivo"",
  ""scoreSaude"": 75
}}

Regras:
- Seja objetivo
- Foque em gestão e melhoria contínua
- Prioridade deve ser apenas: Alta, Média ou Baixa
- scoreSaude deve ser um número inteiro de 0 a 100
- Quanto pior a situação da squad, menor o score

Dados da squad:
- Nome: {squad.NomeSquad}
- Lead Time Médio: {squad.LeadTimeMedio} dias
- Throughput: {squad.Throughput}
- Bugs: {squad.Bugs}
- Bloqueios: {squad.Bloqueios}
";

        _logger.LogInformation(
            "Iniciando análise da IA para squad {NomeSquad}.",
            squad.NomeSquad);

        var completionTask = _chatClient.CompleteChatAsync(prompt);
        var timeoutTask = Task.Delay(TimeoutAnalise, cancellationToken);

        var completedTask = await Task.WhenAny(completionTask, timeoutTask);

        if (completedTask == timeoutTask)
        {
            _logger.LogWarning(
                "Timeout ao chamar IA para squad {NomeSquad} após {TimeoutSeconds}s.",
                squad.NomeSquad,
                TimeoutAnalise.TotalSeconds);

            throw new TimeoutException("A chamada para a IA excedeu o tempo limite de 30 segundos.");
        }

        var response = await completionTask;

        if (response?.Value is null)
        {
            _logger.LogWarning(
                "A IA retornou resposta nula para squad {NomeSquad}.",
                squad.NomeSquad);

            throw new InvalidOperationException("Resposta vazia da IA.");
        }

        if (response.Value.Content.Count == 0)
        {
            _logger.LogWarning(
                "A IA retornou Content vazio para squad {NomeSquad}.",
                squad.NomeSquad);

            throw new InvalidOperationException("Resposta vazia da IA.");
        }

        var json = response.Value.Content[0].Text;

        if (string.IsNullOrWhiteSpace(json))
        {
            _logger.LogWarning(
                "A IA retornou texto vazio para squad {NomeSquad}.",
                squad.NomeSquad);

            throw new InvalidOperationException("Resposta vazia da IA.");
        }

        try
        {
            var resultado = JsonSerializer.Deserialize<AnaliseResultado>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (resultado is null)
            {
                _logger.LogWarning(
                    "Falha ao desserializar resposta da IA para squad {NomeSquad}. Tamanho do conteúdo: {Length}.",
                    squad.NomeSquad,
                    json.Length);

                throw new InvalidOperationException("Falha ao interpretar resposta da IA.");
            }

            return resultado;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "AI response parsing failed for squad {NomeSquad}. Length: {Length}.",
                squad.NomeSquad,
                json.Length);

            throw new InvalidOperationException("Falha ao interpretar resposta da IA.");
        }
    }
}