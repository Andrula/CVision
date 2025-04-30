import { useRef } from "react";
import html2canvas from "html2canvas";
import {
    ScatterChart,
    Scatter,
    XAxis,
    YAxis,
    Tooltip,
    ResponsiveContainer,
} from "recharts";

type Candidate = {
    matchScore: number;
    experienceYears: number;
};

type Props = {
    candidates: Candidate[];
};

const ExperienceVsMatchChart = ({ candidates }: Props) => {
    const chartRef = useRef<HTMLDivElement>(null);

    const data = candidates
        .filter(c => c.experienceYears != null && c.matchScore != null)
        .map(c => ({
            x: c.experienceYears,
            y: c.matchScore,
        }));

        const downloadImage = async () => {
            if (!chartRef.current) return;
        
            const canvas = await html2canvas(chartRef.current, {
                ignoreElements: (el) => el.hasAttribute("data-html2canvas-ignore")
            });
        
            const link = document.createElement("a");
            link.download = "experience_vs_matchscore.png";
            link.href = canvas.toDataURL();
            link.click();
        };        

    return (
        <div
            className="bg-white dark:bg-gray-800 p-6 rounded-xl shadow mt-10 relative"
            ref={chartRef}
        >
            <button
                onClick={downloadImage}
                data-html2canvas-ignore
                className="absolute top-4 right-4 text-sm px-3 py-1 bg-blue-500 text-white rounded hover:bg-blue-600"
            >
                Download PNG
            </button>


            <h2 className="text-xl font-bold text-gray-800 dark:text-white mb-4">
                Erfaring vs. Matchscore
            </h2>
            <ResponsiveContainer width="100%" height={300}>
                <ScatterChart>
                    <XAxis
                        type="number"
                        dataKey="x"
                        name="Erfaring (år)"
                        tick={{ fontSize: 12, fill: "currentColor" }}
                        label={{ value: "Erfaring (år)", position: "insideBottom", offset: -5 }}
                    />
                    <YAxis
                        type="number"
                        dataKey="y"
                        name="Matchscore"
                        domain={[0, 100]}
                        tick={{ fontSize: 12, fill: "currentColor" }}
                        label={{ value: "Matchscore", angle: -90, position: "insideLeft" }}
                    />
                    <Tooltip cursor={{ strokeDasharray: "3 3" }} />
                    <Scatter name="Kandidater" data={data} fill="#06b6d4" />
                </ScatterChart>
            </ResponsiveContainer>
        </div>
    );
};

export default ExperienceVsMatchChart;
