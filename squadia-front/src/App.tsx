import { useEffect, useMemo, useState } from "react";
import type { FormEvent, ReactNode } from "react";
import axios from "axios";
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  CartesianGrid,
  LineChart,
  Line,
} from "recharts";
import "./App.css";

type SquadMetrics = {
  nomeSquad: string;
  leadTimeMedio: number;
  throughput: number;
  bugs: number;
  bloqueios: number;
};

type AnaliseResultado = {
  diagnostico: string;
  problemas: string[];
  acoes: string[];
  prioridade: string;
  resumoExecutivo: string;
  scoreSaude: number;
};

type HistoricoAnalise = {
  id: number;
  nomeSquad: string;
  leadTimeMedio: number;
  throughput: number;
  bugs: number;
  bloqueios: number;
  diagnostico: string;
  prioridade: string;
  resumoExecutivo: string;
  scoreSaude: number;
  criadoEm: string;
};

type HistoricoResponse = {
  paginaAtual: number;
  tamanhoPagina: number;
  totalItens: number;
  totalPaginas: number;
  itens: HistoricoAnalise[];
};

type DashboardResumo = {
  totalAnalises: number;
  mediaScoreSaude: number;
  prioridadeAlta: number;
  prioridadeMedia: number;
  prioridadeBaixa: number;
  ultimaSquadAnalisada: string | null;
  ultimaAnaliseEm: string | null;
};

const api = axios.create({
  baseURL: "http://localhost:5103/api",
});

export default function App() {
  const [form, setForm] = useState<SquadMetrics>({
    nomeSquad: "",
    leadTimeMedio: 0,
    throughput: 0,
    bugs: 0,
    bloqueios: 0,
  });

  const [resultado, setResultado] = useState<AnaliseResultado | null>(null);
  const [dashboard, setDashboard] = useState<DashboardResumo | null>(null);
  const [historico, setHistorico] = useState<HistoricoResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [paginaHistorico, setPaginaHistorico] = useState(1);

  async function carregarDashboard() {
    const res = await api.get<DashboardResumo>("/Squad/dashboard");
    setDashboard(res.data);
  }

  async function carregarHistorico(pagina = paginaHistorico) {
    const res = await api.get<HistoricoResponse>("/Squad/historico", {
      params: {
        pagina,
        tamanhoPagina: 8,
      },
    });

    setHistorico(res.data);
  }

  async function carregarDados(pagina = paginaHistorico) {
    await Promise.all([carregarDashboard(), carregarHistorico(pagina)]);
  }

  useEffect(() => {
    carregarDados(paginaHistorico);
  }, [paginaHistorico]);

  const prioridadeData = useMemo(() => {
    if (!dashboard) return [];

    return [
      {
        name: "Críticas",
        value: dashboard.prioridadeAlta,
        description: "Exigem ação imediata",
      },
      {
        name: "Em atenção",
        value: dashboard.prioridadeMedia,
        description: "Precisam ser monitoradas",
      },
      {
        name: "Saudáveis",
        value: dashboard.prioridadeBaixa,
        description: "Fluxo sob controle",
      },
    ];
  }, [dashboard]);

  const scoreTrendData = useMemo(() => {
    if (!historico?.itens) return [];

    return [...historico.itens]
      .slice()
      .reverse()
      .map((item) => ({
        name: item.nomeSquad.replace(" Squad", ""),
        score: item.scoreSaude,
      }));
  }, [historico]);

  const piorSquad = useMemo(() => {
    if (!historico?.itens?.length) return null;

    return [...historico.itens].sort((a, b) => a.scoreSaude - b.scoreSaude)[0];
  }, [historico]);

  const melhorSquad = useMemo(() => {
    if (!historico?.itens?.length) return null;

    return [...historico.itens].sort((a, b) => b.scoreSaude - a.scoreSaude)[0];
  }, [historico]);

  async function analisarSquad(e: FormEvent) {
    e.preventDefault();
    setLoading(true);

    try {
      const res = await api.post<AnaliseResultado>("/Squad/analisar", form);
      setResultado(res.data);
      setPaginaHistorico(1);
      await carregarDados(1);
    } catch (error) {
      console.error(error);
      alert("Erro ao analisar squad.");
    } finally {
      setLoading(false);
    }
  }

  async function deletarAnalise(id: number) {
    if (!confirm("Tem certeza que deseja excluir essa análise?")) return;

    try {
      await api.delete(`/Squad/${id}`);

      if (historico?.itens.length === 1 && paginaHistorico > 1) {
        setPaginaHistorico((prev) => prev - 1);
      } else {
        await carregarDados(paginaHistorico);
      }

      setResultado(null);
    } catch (err) {
      console.error(err);
      alert("Erro ao deletar análise.");
    }
  }

  function atualizarCampo<K extends keyof SquadMetrics>(
    campo: K,
    valor: SquadMetrics[K]
  ) {
    setForm((prev) => ({
      ...prev,
      [campo]: valor,
    }));
  }

  function preencherExemploRuim() {
    setForm({
      nomeSquad: "Payments Squad",
      leadTimeMedio: 85,
      throughput: 6,
      bugs: 28,
      bloqueios: 10,
    });
  }

  function preencherExemploMedio() {
    setForm({
      nomeSquad: "Marketplace Squad",
      leadTimeMedio: 55,
      throughput: 10,
      bugs: 12,
      bloqueios: 4,
    });
  }

  function preencherExemploBom() {
    setForm({
      nomeSquad: "Growth Squad",
      leadTimeMedio: 25,
      throughput: 18,
      bugs: 3,
      bloqueios: 1,
    });
  }

  function limparFormulario() {
    setForm({
      nomeSquad: "",
      leadTimeMedio: 0,
      throughput: 0,
      bugs: 0,
      bloqueios: 0,
    });

    setResultado(null);
  }

  function getPrioridadeClass(prioridade: string) {
    const valor = prioridade.toLowerCase();

    if (valor.includes("alta")) return "danger";
    if (valor.includes("media") || valor.includes("média")) return "warning";
    if (valor.includes("baixa")) return "success";

    return "neutral";
  }

  function getScoreClass(score: number) {
    if (score < 50) return "danger";
    if (score < 75) return "warning";
    return "success";
  }

  function getScoreLabel(score: number) {
    if (score < 50) return "Crítico";
    if (score < 75) return "Em atenção";
    return "Saudável";
  }

  return (
    <div className="app">
      <div className="background-glow" />

      <main className="page">
        <header className="hero">
          <div>
            <span className="eyebrow">AI para liderança de engenharia</span>
            <h1>AI Squad Performance Analyzer</h1>
            <p>
              Transforme métricas de squads em diagnóstico, score de saúde e
              plano de ação com apoio de Inteligência Artificial.
            </p>
          </div>

          <div className="hero-card">
            <span>Última squad analisada</span>
            <strong>{dashboard?.ultimaSquadAnalisada ?? "Nenhuma ainda"}</strong>
          </div>
        </header>

        <section className="dashboard-grid">
          <MetricCard
            title="Total de análises"
            value={dashboard?.totalAnalises ?? 0}
            description="Histórico salvo no banco"
          />

          <MetricCard
            title="Score médio"
            value={dashboard?.mediaScoreSaude ?? 0}
            description={getScoreLabel(dashboard?.mediaScoreSaude ?? 0)}
            variant={getScoreClass(dashboard?.mediaScoreSaude ?? 0)}
          />

          <MetricCard
            title="Squads críticas"
            value={dashboard?.prioridadeAlta ?? 0}
            description="Exigem ação imediata"
            variant="danger"
          />

          <MetricCard
            title="Squads saudáveis"
            value={dashboard?.prioridadeBaixa ?? 0}
            description="Fluxo sob controle"
            variant="success"
          />
        </section>

        <section className="executive-grid">
          <div className="card insight-card">
            <span className="eyebrow">Visão executiva</span>
            <h2>Resumo da saúde das squads</h2>
            <p>
              Este painel mostra a distribuição de risco das squads analisadas,
              destacando onde há necessidade de ação imediata e onde o fluxo está
              mais saudável.
            </p>

            <div className="executive-list">
              <ExecutiveItem
                label="Squad mais crítica"
                value={piorSquad?.nomeSquad ?? "-"}
                description={
                  piorSquad
                    ? `Score ${piorSquad.scoreSaude} — ${getScoreLabel(
                        piorSquad.scoreSaude
                      )}`
                    : "Sem dados suficientes"
                }
                variant="danger"
              />

              <ExecutiveItem
                label="Melhor squad"
                value={melhorSquad?.nomeSquad ?? "-"}
                description={
                  melhorSquad
                    ? `Score ${melhorSquad.scoreSaude} — ${getScoreLabel(
                        melhorSquad.scoreSaude
                      )}`
                    : "Sem dados suficientes"
                }
                variant="success"
              />
            </div>
          </div>

          <div className="card insight-card">
            <span className="eyebrow">Leitura rápida</span>
            <h2>Como interpretar</h2>

            <div className="interpretation">
              <div>
                <strong>🔴 Críticas</strong>
                <p>Alto risco operacional. Exigem ação rápida.</p>
              </div>

              <div>
                <strong>🟡 Em atenção</strong>
                <p>Performance intermediária. Precisam monitoramento.</p>
              </div>

              <div>
                <strong>🟢 Saudáveis</strong>
                <p>Boa previsibilidade, baixo atrito e fluxo controlado.</p>
              </div>
            </div>
          </div>
        </section>

        <section className="content-grid">
          <div className="card form-card">
            <div className="section-header">
              <div>
                <h2>Nova análise</h2>
                <p>
                  Preencha os dados ou use uma simulação pronta para gravar sua
                  demo.
                </p>
              </div>
            </div>

            <div className="demo-actions">
              <button
                type="button"
                className="ghost danger"
                onClick={preencherExemploRuim}
              >
                Exemplo crítico
              </button>

              <button
                type="button"
                className="ghost warning"
                onClick={preencherExemploMedio}
              >
                Exemplo médio
              </button>

              <button
                type="button"
                className="ghost success"
                onClick={preencherExemploBom}
              >
                Exemplo saudável
              </button>
            </div>

            <form onSubmit={analisarSquad} className="form">
              <Field label="Nome da squad" hint="Ex.: Payments Squad">
                <input
                  placeholder="Payments Squad"
                  value={form.nomeSquad}
                  onChange={(e) => atualizarCampo("nomeSquad", e.target.value)}
                  required
                />
              </Field>

              <Field
                label="Lead Time médio (dias)"
                hint="Tempo médio para uma demanda sair da entrada até a conclusão."
              >
                <input
                  type="number"
                  min="0"
                  placeholder="Ex.: 85"
                  value={form.leadTimeMedio}
                  onChange={(e) =>
                    atualizarCampo("leadTimeMedio", Number(e.target.value))
                  }
                  required
                />
              </Field>

              <Field
                label="Throughput (itens/semana)"
                hint="Quantidade de entregas finalizadas no período analisado."
              >
                <input
                  type="number"
                  min="0"
                  placeholder="Ex.: 6"
                  value={form.throughput}
                  onChange={(e) =>
                    atualizarCampo("throughput", Number(e.target.value))
                  }
                  required
                />
              </Field>

              <Field
                label="Bugs nos últimos 30 dias"
                hint="Total de defeitos reportados ou encontrados no período."
              >
                <input
                  type="number"
                  min="0"
                  placeholder="Ex.: 28"
                  value={form.bugs}
                  onChange={(e) => atualizarCampo("bugs", Number(e.target.value))}
                  required
                />
              </Field>

              <Field
                label="Bloqueios nos últimos 30 dias"
                hint="Impedimentos que atrasaram ou pararam o fluxo da squad."
              >
                <input
                  type="number"
                  min="0"
                  placeholder="Ex.: 10"
                  value={form.bloqueios}
                  onChange={(e) =>
                    atualizarCampo("bloqueios", Number(e.target.value))
                  }
                  required
                />
              </Field>

              <div className="form-actions">
                <button type="submit" disabled={loading}>
                  {loading ? "Analisando com IA..." : "Analisar squad"}
                </button>

                <button
                  type="button"
                  className="secondary"
                  onClick={limparFormulario}
                >
                  Limpar
                </button>
              </div>
            </form>
          </div>

          <div className="charts-stack">
            <div className="card chart-card">
              <div className="section-header">
                <div>
                  <h2>Distribuição de risco</h2>
                  <p>Quantidade de squads por nível de atenção.</p>
                </div>
              </div>

              <div className="chart-box">
                {prioridadeData.length > 0 ? (
                  <ResponsiveContainer width="100%" height={250}>
                    <BarChart data={prioridadeData}>
                      <CartesianGrid strokeDasharray="3 3" stroke="#263449" />
                      <XAxis dataKey="name" stroke="#94a3b8" />
                      <YAxis allowDecimals={false} stroke="#94a3b8" />
                      <Tooltip
                        formatter={(value, _name, item) => [
                          `${value} análise(s)`,
                          item.payload.description,
                        ]}
                        contentStyle={{
                          background: "#111827",
                          border: "1px solid #334155",
                          borderRadius: 12,
                          color: "#e5e7eb",
                        }}
                      />
                      <Bar dataKey="value" fill="#38bdf8" radius={[8, 8, 0, 0]} />
                    </BarChart>
                  </ResponsiveContainer>
                ) : (
                  <div className="empty-chart">
                    Nenhuma análise encontrada ainda.
                  </div>
                )}
              </div>
            </div>

            <div className="card chart-card">
              <div className="section-header">
                <div>
                  <h2>Evolução do score</h2>
                  <p>Últimas análises ordenadas por data.</p>
                </div>
              </div>

              <div className="chart-box small">
                {scoreTrendData.length > 0 ? (
                  <ResponsiveContainer width="100%" height={220}>
                    <LineChart data={scoreTrendData}>
                      <CartesianGrid strokeDasharray="3 3" stroke="#263449" />
                      <XAxis dataKey="name" stroke="#94a3b8" />
                      <YAxis domain={[0, 100]} stroke="#94a3b8" />
                      <Tooltip
                        contentStyle={{
                          background: "#111827",
                          border: "1px solid #334155",
                          borderRadius: 12,
                          color: "#e5e7eb",
                        }}
                      />
                      <Line
                        type="monotone"
                        dataKey="score"
                        stroke="#22c55e"
                        strokeWidth={3}
                        dot={{ r: 4 }}
                      />
                    </LineChart>
                  </ResponsiveContainer>
                ) : (
                  <div className="empty-chart">
                    Nenhuma análise encontrada ainda.
                  </div>
                )}
              </div>
            </div>
          </div>
        </section>

        {resultado && (
          <section className="card result-card">
            <div className="result-header">
              <div>
                <span className="eyebrow">Resultado da IA</span>
                <h2>{form.nomeSquad || "Squad analisada"}</h2>
              </div>

              <div className={`score-badge ${getScoreClass(resultado.scoreSaude)}`}>
                <span>Score de saúde</span>
                <strong>{resultado.scoreSaude}</strong>
              </div>
            </div>

            <div className="result-summary">
              <div>
                <h3>Resumo executivo</h3>
                <p>{resultado.resumoExecutivo}</p>
              </div>

              <div>
                <h3>Prioridade</h3>
                <span
                  className={`priority ${getPrioridadeClass(
                    resultado.prioridade
                  )}`}
                >
                  {resultado.prioridade}
                </span>
              </div>
            </div>

            <div className="diagnostic">
              <h3>Diagnóstico</h3>
              <p>{resultado.diagnostico}</p>
            </div>

            <div className="result-columns">
              <div className="insight-box">
                <h3>Problemas identificados</h3>
                <ul>
                  {resultado.problemas.map((item, index) => (
                    <li key={index}>{item}</li>
                  ))}
                </ul>
              </div>

              <div className="insight-box">
                <h3>Ações recomendadas</h3>
                <ul>
                  {resultado.acoes.map((item, index) => (
                    <li key={index}>{item}</li>
                  ))}
                </ul>
              </div>
            </div>
          </section>
        )}

        <section className="card result-card">
          <div className="section-header">
            <div>
              <span className="eyebrow">Histórico</span>
              <h2>Últimas análises</h2>
              <p>
                Acompanhe as análises salvas no banco e use essa visão na sua
                demo.
              </p>
            </div>

            <button type="button" className="secondary" onClick={() => carregarDados()}>
              Atualizar
            </button>
          </div>

          {!historico ? (
            <div className="empty-chart">Carregando histórico...</div>
          ) : historico.itens.length === 0 ? (
            <div className="empty-chart">Nenhuma análise encontrada ainda.</div>
          ) : (
            <>
              <div className="history-grid">
                {historico.itens.map((item) => (
                  <div className="history-card" key={item.id}>
                    <div className="history-card-actions">
                      <button
                        type="button"
                        className="delete-button"
                        onClick={() => deletarAnalise(item.id)}
                      >
                        Excluir
                      </button>
                    </div>

                    <div className="history-top">
                      <div>
                        <h3>{item.nomeSquad}</h3>
                        <p>{new Date(item.criadoEm).toLocaleString("pt-BR")}</p>
                      </div>

                      <div
                        className={`score-badge compact ${getScoreClass(
                          item.scoreSaude
                        )}`}
                      >
                        <span>Score</span>
                        <strong>{item.scoreSaude}</strong>
                      </div>
                    </div>

                    <p>
                      <strong>Resumo:</strong> {item.resumoExecutivo}
                    </p>

                    <div className="history-metrics">
                      <span
                        className={`priority ${getPrioridadeClass(
                          item.prioridade
                        )}`}
                      >
                        {item.prioridade}
                      </span>

                      <span>Lead Time: {item.leadTimeMedio}d</span>
                      <span>Throughput: {item.throughput}</span>
                      <span>Bugs: {item.bugs}</span>
                      <span>Bloqueios: {item.bloqueios}</span>
                    </div>
                  </div>
                ))}
              </div>

              <div className="form-actions pagination-actions">
                <button
                  type="button"
                  className="secondary"
                  disabled={historico.paginaAtual <= 1}
                  onClick={() => setPaginaHistorico((prev) => Math.max(1, prev - 1))}
                >
                  Anterior
                </button>

                <span>
                  Página {historico.paginaAtual} de {historico.totalPaginas || 1}
                </span>

                <button
                  type="button"
                  className="secondary"
                  disabled={historico.paginaAtual >= historico.totalPaginas}
                  onClick={() => setPaginaHistorico((prev) => prev + 1)}
                >
                  Próxima
                </button>
              </div>
            </>
          )}
        </section>
      </main>
    </div>
  );
}

function Field({
  label,
  hint,
  children,
}: {
  label: string;
  hint: string;
  children: ReactNode;
}) {
  return (
    <label className="field">
      <span>{label}</span>
      {children}
      <small>{hint}</small>
    </label>
  );
}

function MetricCard({
  title,
  value,
  description,
  variant = "default",
}: {
  title: string;
  value: number | string;
  description: string;
  variant?: "default" | "danger" | "warning" | "success";
}) {
  return (
    <div className={`metric-card ${variant}`}>
      <span>{title}</span>
      <strong>{value}</strong>
      <small>{description}</small>
    </div>
  );
}

function ExecutiveItem({
  label,
  value,
  description,
  variant,
}: {
  label: string;
  value: string;
  description: string;
  variant: "danger" | "success";
}) {
  return (
    <div className={`executive-item ${variant}`}>
      <span>{label}</span>
      <strong>{value}</strong>
      <small>{description}</small>
    </div>
  );
}