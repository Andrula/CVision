import { useRef } from "react";
import html2canvas from "html2canvas";
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer } from "recharts";

type MatchScoreChartProps = {
  matchScores: number[];
};

const groupByCoarseRange = (scores: number[]) => ({
  "0-74": scores.filter(s => s < 75).length,
  "75-84": scores.filter(s => s >= 75 && s < 85).length,
  "85-94": scores.filter(s => s >= 85 && s < 95).length,
  "95-100": scores.filter(s => s >= 95).length,
});

const groupByTenPointRange = (scores: number[]) => {
  const result: Record<string, number> = {};
  for (let i = 0; i <= 90; i += 10) {
    const label = i === 90 ? "90-100" : `${i}-${i + 9}`;
    result[label] = 0;
  }
  scores.forEach(score => {
    const key = score >= 90 ? "90-100" : `${Math.floor(score / 10) * 10}-${Math.floor(score / 10) * 10 + 9}`;
    result[key]++;
  });
  return Object.entries(result).map(([range, count]) => ({ range, count }));
};

const MatchScoreChart = ({ matchScores }: MatchScoreChartProps) => {
  const chartRef = useRef<HTMLDivElement>(null);

  const downloadImage = async () => {
    if (!chartRef.current) return;

    const canvas = await html2canvas(chartRef.current, {
      ignoreElements: (el) => el.hasAttribute("data-html2canvas-ignore")
    });

    const link = document.createElement("a");
    link.download = "matchscore_chart.png";
    link.href = canvas.toDataURL();
    link.click();
  };

  const coarseData = Object.entries(groupByCoarseRange(matchScores)).map(([range, count]) => ({ range, count }));
  const fineData = groupByTenPointRange(matchScores);

  const axisStyle = {
    fontSize: 12,
    fill: "currentColor",
    className: "text-gray-700 dark:text-gray-300",
  };

  return (
    <div className="bg-white dark:bg-gray-800 p-6 rounded-xl shadow space-y-10 relative" ref={chartRef}>
      <button
        onClick={downloadImage}
        data-html2canvas-ignore
        className="absolute top-4 right-4 text-sm px-3 py-1 bg-blue-500 text-white rounded hover:bg-blue-600"
      >
        Download PNG
      </button>

      <div>
        <h2 className="text-xl font-bold text-gray-800 dark:text-white mb-4">
          Matchscore – bred fordeling
        </h2>
        <ResponsiveContainer width="100%" height={250}>
          <BarChart data={coarseData} barCategoryGap={30}>
            <XAxis dataKey="range" tick={axisStyle} />
            <YAxis allowDecimals={false} tick={axisStyle} />
            <Tooltip
              contentStyle={{ backgroundColor: "#1e293b", border: "none", color: "#fff" }}
              labelStyle={{ fontWeight: "bold" }}
            />
            <Bar dataKey="count" fill="#06b6d4" radius={[4, 4, 0, 0]} />
          </BarChart>
        </ResponsiveContainer>
      </div>

      <div>
        <h2 className="text-xl font-bold text-gray-800 dark:text-white mb-4">
          Matchscore – histogram
        </h2>
        <ResponsiveContainer width="100%" height={250}>
          <BarChart data={fineData} barCategoryGap={10}>
            <XAxis dataKey="range" tick={axisStyle} />
            <YAxis allowDecimals={false} tick={axisStyle} />
            <Tooltip
              contentStyle={{ backgroundColor: "#1e293b", border: "none", color: "#fff" }}
              labelStyle={{ fontWeight: "bold" }}
            />
            <Bar dataKey="count" fill="#06b6d4" radius={[4, 4, 0, 0]} />
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
};

export default MatchScoreChart;
