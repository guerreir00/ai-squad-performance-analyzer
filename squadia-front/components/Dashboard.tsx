import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer
} from "recharts";

type Props = {
  data: {
    totalAnalises: number;
    mediaScoreSaude: number;
    prioridadeAlta: number;
    prioridadeMedia: number;
    prioridadeBaixa: number;
  };
};

export default function Dashboard({ data }: Props) {
  const chartData = [
    { name: "Alta", value: data.prioridadeAlta },
    { name: "Média", value: data.prioridadeMedia },
    { name: "Baixa", value: data.prioridadeBaixa }
  ];

  return (
    <div style={{ marginTop: 30 }}>
      <h2>Dashboard</h2>

      {/* Cards */}
      <div style={{ display: "flex", gap: 20, marginBottom: 20 }}>
        <Card title="Total Análises" value={data.totalAnalises} />
        <Card title="Score Médio" value={data.mediaScoreSaude} />
      </div>

      {/* Gráfico */}
      <div style={{ width: "100%", height: 300 }}>
        <ResponsiveContainer>
          <BarChart data={chartData}>
            <XAxis dataKey="name" />
            <YAxis />
            <Tooltip />
            <Bar dataKey="value" />
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}

function Card({ title, value }: { title: string; value: any }) {
  return (
    <div
      style={{
        padding: 20,
        borderRadius: 10,
        background: "#f5f5f5",
        minWidth: 150
      }}
    >
      <div style={{ fontSize: 14, color: "#666" }}>{title}</div>
      <div style={{ fontSize: 24, fontWeight: "bold" }}>{value}</div>
    </div>
  );
}