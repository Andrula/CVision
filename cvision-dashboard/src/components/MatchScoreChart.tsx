import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer } from 'recharts';

type MatchScoreChartProps = {
  matchScores: number[];
};

const groupByRange = (scores: number[]) => {
  const ranges = {
    '0-74': 0,
    '75-84': 0,
    '85-94': 0,
    '95-100': 0,
  };

  scores.forEach((score) => {
    if (score < 75) ranges['0-74']++;
    else if (score < 85) ranges['75-84']++;
    else if (score < 95) ranges['85-94']++;
    else ranges['95-100']++;
  });

  return Object.entries(ranges).map(([range, count]) => ({ range, count }));
};

const MatchScoreChart = ({ matchScores }: MatchScoreChartProps) => {
  const data = groupByRange(matchScores);

  return (
    <div className="bg-white dark:bg-gray-800 p-4 rounded shadow">
      <h2 className="text-lg font-semibold text-gray-800 dark:text-gray-100 mb-2">Ansøgeres match fordeling</h2>
      <ResponsiveContainer width="100%" height={250}>
        <BarChart data={data}>
          <XAxis 
            dataKey="range" 
            tick={{ 
              fontSize: 12, 
              fill: "currentColor", 
              className: "text-gray-700 dark:text-gray-300" 
            }} 
          />
          <YAxis 
            allowDecimals={false}
            tick={{ 
              fontSize: 12, 
              fill: "currentColor", 
              className: "text-gray-700 dark:text-gray-300" 
            }} 
          />
          <Tooltip />
          <Bar dataKey="count" fill="#06b6d4" />
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
};

export default MatchScoreChart;
