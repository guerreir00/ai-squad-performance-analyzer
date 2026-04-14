using System.Text.Json;
using OpenAI.Chat;
using SquadIA.Models;

namespace SquadIA.Services;

public class IAService
{
    private readonly string _apiKey;

    public IAService(IConfiguration configuration)
    {
        _apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey não configurada.");
    }

    public async Task<AnaliseResultado> AnalisarSquadAsync(SquadMetrics squad)
    {
        var client = new ChatClient(model: "gpt-4o-mini", apiKey: _apiKey);

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

        var response = await client.CompleteChatAsync(prompt);
        var json = response.Value.Content[0].Text;

        try
        {
            var resultado = JsonSerializer.Deserialize<AnaliseResultado>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (resultado is null)
                throw new InvalidOperationException("A resposta da IA veio vazia.");

            return resultado;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Não foi possível converter a resposta da IA em JSON válido. Resposta recebida: {json}",
                ex);
        }
    }
}