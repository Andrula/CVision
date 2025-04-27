import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, Cell } from "recharts";

type SkillDistributionChartProps = {
  data: { skill: string; count: number }[];
};

const COLORS = ["#3b82f6", "#6366f1", "#10b981", "#f59e0b", "#ef4444"];

const SkillDistributionChart = ({ data }: SkillDistributionChartProps) => {
  if (data.length === 0) {
    return <p className="text-gray-500 dark:text-gray-400">No skill data to display yet.</p>;
  }

  const sortedData = [...data].sort((a, b) => b.count - a.count);

  return (
    <div className="bg-white dark:bg-gray-800 p-6 rounded shadow">
      <h2 className="text-xl font-semibold text-gray-800 dark:text-white mb-4">Skill fordeling</h2>
      <ResponsiveContainer width="100%" height={sortedData.length * 28}>
        <BarChart layout="vertical" data={sortedData}>
          <XAxis
            type="number"
            domain={[0, 'auto']}
            allowDecimals={false}
            tick={{
              fontSize: 12,
              fill: "currentColor", 
              className: "text-gray-700 dark:text-gray-300",
            }}
          />
          <YAxis
            dataKey="skill"
            type="category"
            width={200}
            interval={0}
            tick={{
              fontSize: 12,
              fill: "currentColor",
              className: "text-gray-700 dark:text-gray-300",
            }}
          />
          <Tooltip />
          <Bar dataKey="count">
            {sortedData.map((_, i) => (
              <Cell key={`cell-${i}`} fill={COLORS[i % COLORS.length]} />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
};

export default SkillDistributionChart;
