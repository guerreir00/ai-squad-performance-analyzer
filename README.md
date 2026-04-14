# AI Squad Performance Analyzer

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
