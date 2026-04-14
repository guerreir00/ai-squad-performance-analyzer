# AI Squad Performance Analyzer

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-Web_API-512BD4?logo=dotnet&logoColor=white)
![Entity Framework Core](https://img.shields.io/badge/EF_Core-8.0-512BD4?logo=dotnet&logoColor=white)
![SQLite](https://img.shields.io/badge/SQLite-003B57?logo=sqlite&logoColor=white)
![OpenAI](https://img.shields.io/badge/OpenAI-GPT--4o--mini-412991?logo=openai&logoColor=white)
![Swagger](https://img.shields.io/badge/Swagger-UI-85EA2D?logo=swagger&logoColor=black)
![License](https://img.shields.io/badge/license-MIT-green)

API desenvolvida em .NET 8 para análise de performance de squads com apoio de Inteligência Artificial.

O projeto recebe métricas operacionais de uma squad, envia essas informações para a IA, gera um diagnóstico estruturado e salva o histórico das análises em banco SQLite. Também disponibiliza endpoints para consulta de histórico, dashboard resumido, filtros e paginação.

---

## Objetivo

Demonstrar, na prática, como IA pode ser aplicada no contexto de liderança de engenharia para:

- identificar gargalos
- apoiar a tomada de decisão
- gerar planos de ação
- acompanhar a saúde operacional de squads

---

## Funcionalidades

- Análise de squad com IA
- Geração de diagnóstico estruturado
- Geração de resumo executivo
- Cálculo de score de saúde
- Persistência do histórico em SQLite
- Consulta de histórico por ID
- Filtro por nome da squad
- Filtro por período
- Paginação do histórico
- Dashboard com resumo geral das análises

---

## Tecnologias utilizadas

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- SQLite
- OpenAI API
- Swagger

---

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Chave de API da [OpenAI](https://platform.openai.com/api-keys)
- Git

---

## Como rodar localmente

### 1. Clone o repositório

```bash
git clone https://github.com/guerreir00/ai-squad-performance-analyzer.git
cd ai-squad-performance-analyzer
```

### 2. Configure a chave da OpenAI

Crie o arquivo `appsettings.Development.json` na raiz do projeto (já está no `.gitignore` — nunca commite a chave real):

```json
{
  "OpenAI": {
    "ApiKey": "sk-..."
  }
}
```

> A chave pode ser obtida em [platform.openai.com/api-keys](https://platform.openai.com/api-keys). O modelo usado é `gpt-4o-mini`.

### 3. Aplique as migrations e rode

```bash
dotnet ef database update
dotnet run
```

A API estará disponível em `http://localhost:5103`.

### 4. Explore via Swagger

Acesse `http://localhost:5103/swagger` para ver e testar todos os endpoints interativamente.

---

## Exemplos de uso

### Analisar uma squad

```bash
curl -X POST http://localhost:5103/api/squad/analisar \
  -H "Content-Type: application/json" \
  -d '{
    "nomeSquad": "Squad Alpha",
    "leadTimeMedio": 5,
    "throughput": 12,
    "bugsCriticos": 2,
    "blockers": 1,
    "observacoes": "Sprint com muitas reuniões e dependências externas não resolvidas"
  }'
```

Resposta esperada:

```json
{
  "diagnostico": "A squad apresenta lead time elevado...",
  "resumoExecutivo": "Performance abaixo do esperado com 2 bugs críticos ativos.",
  "problemas": ["Lead time acima da média", "Blockers não resolvidos"],
  "acoes": ["Mapear e remover dependências externas", "Revisar cerimônias para reduzir overhead"],
  "scoresSaude": 62,
  "prioridade": "Alta"
}
```

### Consultar histórico

```bash
# Todos os registros (paginado)
curl "http://localhost:5103/api/squad/historico?page=1&pageSize=10"

# Filtrar por squad e período
curl "http://localhost:5103/api/squad/historico?nomeSquad=Squad+Alpha&dataInicio=2024-01-01&dataFim=2024-12-31"

# Buscar por ID
curl "http://localhost:5103/api/squad/historico/1"
```

### Ver dashboard geral

```bash
curl http://localhost:5103/api/squad/dashboard
```

---

## Estrutura do projeto

```bash
SquadIA
├── Controllers
│   └── SquadController.cs
├── Data
│   ├── ApplicationDbContext.cs
│   └── ApplicationDbContextFactory.cs
├── Models
│   ├── AnaliseResultado.cs
│   ├── DashboardResumo.cs
│   ├── HistoricoAnalise.cs
│   └── SquadMetrics.cs
├── Services
│   └── IAService.cs
├── Program.cs
├── appsettings.json
└── README.md
