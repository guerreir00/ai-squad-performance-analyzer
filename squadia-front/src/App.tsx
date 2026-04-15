import { useEffect, useState } from "react";
import axios from "axios";

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

type DashboardResumo = {
  totalAnalises: number;
  mediaScoreSaude: number;
  prioridadeAlta: number;
  prioridadeMedia: number;
  prioridadeBaixa: number;
  ultimaSquadAnalisada: string | null;
  ultimaAnaliseEm: string | null;
};

type HistoricoResponse = {
  paginaAtual: number;
  tamanhoPagina: number;
  totalItens: number;
  totalPaginas: number;
  itens: HistoricoAnalise[];
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

  const [filtros, setFiltros] = useState({
    nomeSquad: "",
    dataInicial: "",
    dataFinal: "",
    pagina: 1,
    tamanhoPagina: 5,
  });

  async function carregarDashboard() {
    const params: Record<string, string | number> = {};

    if (filtros.nomeSquad) params.nomeSquad = filtros.nomeSquad;
    if (filtros.dataInicial) params.dataInicial = filtros.dataInicial;
    if (filtros.dataFinal) params.dataFinal = filtros.dataFinal;

    const response = await api.get<DashboardResumo>("/Squad/dashboard", { params });
    setDashboard(response.data);
  }

  async function carregarHistorico() {
    const params: Record<string, string | number> = {
      pagina: filtros.pagina,
      tamanhoPagina: filtros.tamanhoPagina,
    };

    if (filtros.nomeSquad) params.nomeSquad = filtros.nomeSquad;
    if (filtros.dataInicial) params.dataInicial = filtros.dataInicial;
    if (filtros.dataFinal) params.dataFinal = filtros.dataFinal;

    const response = await api.get<HistoricoResponse>("/Squad/historico", { params });
    setHistorico(response.data);
  }

  async function carregarDados() {
    await Promise.all([carregarDashboard(), carregarHistorico()]);
  }

  useEffect(() => {
    carregarDados();
  }, [filtros.pagina]);

  async function analisarSquad(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);

    try {
      const response = await api.post<AnaliseResultado>("/Squad/analisar", form);
      setResultado(response.data);
      setFiltros((prev) => ({ ...prev, pagina: 1 }));
      await carregarDados();
    } catch (error) {
      alert("Erro ao analisar squad.");
      console.error(error);
    } finally {
      setLoading(false);
    }
  }

  function atualizarCampo<K extends keyof SquadMetrics>(campo: K, valor: SquadMetrics[K]) {
    setForm((prev) => ({
      ...prev,
      [campo]: valor,
    }));
  }

  async function aplicarFiltros() {
    setFiltros((prev) => ({ ...prev, pagina: 1 }));
    const novosFiltros = { ...filtros, pagina: 1 };

    const paramsDashboard: Record<string, string | number> = {};
    if (novosFiltros.nomeSquad) paramsDashboard.nomeSquad = novosFiltros.nomeSquad;
    if (novosFiltros.dataInicial) paramsDashboard.dataInicial = novosFiltros.dataInicial;
    if (novosFiltros.dataFinal) paramsDashboard.dataFinal = novosFiltros.dataFinal;

    const paramsHistorico: Record<string, string | number> = {
      pagina: 1,
      tamanhoPagina: novosFiltros.tamanhoPagina,
    };
    if (novosFiltros.nomeSquad) paramsHistorico.nomeSquad = novosFiltros.nomeSquad;
    if (novosFiltros.dataInicial) paramsHistorico.dataInicial = novosFiltros.dataInicial;
    if (novosFiltros.dataFinal) paramsHistorico.dataFinal = novosFiltros.dataFinal;

    const [dashboardRes, historicoRes] = await Promise.all([
      api.get<DashboardResumo>("/Squad/dashboard", { params: paramsDashboard }),
      api.get<HistoricoResponse>("/Squad/historico", { params: paramsHistorico }),
    ]);

    setDashboard(dashboardRes.data);
    setHistorico(historicoRes.data);
  }

  function limparFiltros() {
    const filtrosLimpos = {
      nomeSquad: "",
      dataInicial: "",
      dataFinal: "",
      pagina: 1,
      tamanhoPagina: 5,
    };

    setFiltros(filtrosLimpos);

    setTimeout(() => {
      carregarDados();
    }, 0);
  }

  return (
    <div className="page">
      <header className="hero">
        <h1>AI Squad Performance Analyzer</h1>
        <p>Análise de squads com IA, dashboard e histórico</p>
      </header>

      <section className="grid">
        <div className="card">
          <h2>Nova análise</h2>

          <form onSubmit={analisarSquad} className="form">
            <div>
              <label htmlFor="nomeSquad">Nome da squad</label>
              <input
                id="nomeSquad"
                placeholder="Ex.: Payments Squad"
                value={form.nomeSquad}
                onChange={(e) => atualizarCampo("nomeSquad", e.target.value)}
                required
              />
            </div>

            <div>
              <label htmlFor="leadTimeMedio">Lead Time Médio (em dias)</label>
              <input
                id="leadTimeMedio"
                type="number"
                min="0"
                placeholder="Ex.: 70"
                value={form.leadTimeMedio}
                onChange={(e) => atualizarCampo("leadTimeMedio", Number(e.target.value))}
                required
              />
              <small>Tempo médio para concluir uma entrega.</small>
            </div>

            <div>
              <label htmlFor="throughput">Throughput</label>
              <input
                id="throughput"
                type="number"
                min="0"
                placeholder="Ex.: 8"
                value={form.throughput}
                onChange={(e) => atualizarCampo("throughput", Number(e.target.value))}
                required
              />
              <small>Quantidade de entregas concluídas no período.</small>
            </div>

            <div>
              <label htmlFor="bugs">Quantidade de bugs</label>
              <input
                id="bugs"
                type="number"
                min="0"
                placeholder="Ex.: 20"
                value={form.bugs}
                onChange={(e) => atualizarCampo("bugs", Number(e.target.value))}
                required
              />
              <small>Total de bugs identificados no período analisado.</small>
            </div>

            <div>
              <label htmlFor="bloqueios">Quantidade de bloqueios</label>
              <input
                id="bloqueios"
                type="number"
                min="0"
                placeholder="Ex.: 6"
                value={form.bloqueios}
                onChange={(e) => atualizarCampo("bloqueios", Number(e.target.value))}
                required
              />
              <small>Impedimentos que afetaram o fluxo da squad.</small>
            </div>

            <button type="submit" disabled={loading}>
              {loading ? "Analisando..." : "Analisar squad"}
            </button>
          </form>
        </div>

        <div className="card">
          <h2>Dashboard</h2>

          {dashboard ? (
            <div className="stats">
              <div className="stat">
                <span>Total de análises</span>
                <strong>{dashboard.totalAnalises}</strong>
              </div>

              <div className="stat">
                <span>Média score saúde</span>
                <strong>{dashboard.mediaScoreSaude}</strong>
              </div>

              <div className="stat">
                <span>Prioridade alta</span>
                <strong>{dashboard.prioridadeAlta}</strong>
              </div>

              <div className="stat">
                <span>Prioridade média</span>
                <strong>{dashboard.prioridadeMedia}</strong>
              </div>

              <div className="stat">
                <span>Prioridade baixa</span>
                <strong>{dashboard.prioridadeBaixa}</strong>
              </div>

              <div className="stat">
                <span>Última squad</span>
                <strong>{dashboard.ultimaSquadAnalisada ?? "-"}</strong>
              </div>
            </div>
          ) : (
            <p>Carregando dashboard...</p>
          )}
        </div>
      </section>

      {resultado && (
        <section className="card result-card">
          <h2>Resultado da análise</h2>

          <p><strong>Diagnóstico:</strong> {resultado.diagnostico}</p>
          <p><strong>Resumo executivo:</strong> {resultado.resumoExecutivo}</p>
          <p><strong>Prioridade:</strong> {resultado.prioridade}</p>
          <p><strong>Score de saúde:</strong> {resultado.scoreSaude}</p>

          <div className="columns">
            <div>
              <h3>Problemas</h3>
              <ul>
                {resultado.problemas.map((item, index) => (
                  <li key={index}>{item}</li>
                ))}
              </ul>
            </div>

            <div>
              <h3>Ações</h3>
              <ul>
                {resultado.acoes.map((item, index) => (
                  <li key={index}>{item}</li>
                ))}
              </ul>
            </div>
          </div>
        </section>
      )}

      <section className="card">
        <h2>Filtros</h2>

        <div className="filters">
          <input
            placeholder="Filtrar por squad"
            value={filtros.nomeSquad}
            onChange={(e) => setFiltros((prev) => ({ ...prev, nomeSquad: e.target.value }))}
          />

          <input
            type="date"
            value={filtros.dataInicial}
            onChange={(e) => setFiltros((prev) => ({ ...prev, dataInicial: e.target.value }))}
          />

          <input
            type="date"
            value={filtros.dataFinal}
            onChange={(e) => setFiltros((prev) => ({ ...prev, dataFinal: e.target.value }))}
          />

          <button type="button" onClick={aplicarFiltros}>
            Aplicar filtros
          </button>

          <button type="button" className="secondary" onClick={limparFiltros}>
            Limpar
          </button>
        </div>
      </section>

      <section className="card">
        <h2>Histórico</h2>

        {!historico ? (
          <p>Carregando histórico...</p>
        ) : historico.itens.length === 0 ? (
          <p>Nenhum registro encontrado.</p>
        ) : (
          <>
            <div className="history-list">
              {historico.itens.map((item) => (
                <div className="history-item" key={item.id}>
                  <div className="history-top">
                    <strong>{item.nomeSquad}</strong>
                    <span>{new Date(item.criadoEm).toLocaleString("pt-BR")}</span>
                  </div>

                  <p><strong>Diagnóstico:</strong> {item.diagnostico}</p>
                  <p><strong>Resumo:</strong> {item.resumoExecutivo}</p>

                  <div className="badges">
                    <span className="badge">Prioridade: {item.prioridade}</span>
                    <span className="badge">Score: {item.scoreSaude}</span>
                    <span className="badge">Lead Time: {item.leadTimeMedio}</span>
                    <span className="badge">Throughput: {item.throughput}</span>
                  </div>
                </div>
              ))}
            </div>

            <div className="pagination">
              <button
                type="button"
                className="secondary"
                disabled={historico.paginaAtual <= 1}
                onClick={() =>
                  setFiltros((prev) => ({ ...prev, pagina: prev.pagina - 1 }))
                }
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
                onClick={() =>
                  setFiltros((prev) => ({ ...prev, pagina: prev.pagina + 1 }))
                }
              >
                Próxima
              </button>
            </div>
          </>
        )}
      </section>
    </div>
  );
}