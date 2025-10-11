import { useParams, useNavigate } from "react-router-dom";
import { useEffect, useState } from "react";
import Header from "../components/Header";

type CandidateProfile = {
  id: number;
  jobId: number;
  name: string;
  email: string;
  phone: string;
  location: string;
  profileSummary: string;
  matchScore: number;
  skills: string[];
  strengths: string[];
  weaknesses: string[];
  analysisSummary: string;
  createdAt: string;
};

const CandidateDetail = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const [candidate, setCandidate] = useState<CandidateProfile | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchCandidate = async () => {
      try {
        const res = await fetch(`http://localhost:5000/api/candidates/profile/${id}`);
        if (!res.ok) throw new Error("Failed to load candidate profile");
        const data = await res.json();

        setCandidate({
          ...data,
          skills: JSON.parse(data.skills),
          strengths: JSON.parse(data.strengths),
          weaknesses: JSON.parse(data.weaknesses),
        });
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    };

    fetchCandidate();
  }, [id]);

  if (loading) return <p className="p-6">Henter data...</p>;
  if (!candidate) return <p className="p-6">Kandidat ikke fundet.</p>;

  return (
    <>
      <div className="min-h-screen flex flex-col">
        <Header />

        <div className="flex-1 flex flex-col">
          <div className="grid grid-cols-1 lg:grid-cols-2 items-stretch flex-1">
            {/* LEFT SIDE */}
            <div className="bg-white dark:bg-gray-800 rounded shadow p-6 space-y-4 h-full flex flex-col">
              
              {/* 🟢 TOOLBAR inside left box */}
              <div className="flex justify-between items-center mb-4 border-b border-gray-300 dark:border-gray-700 pb-2">
                <div className="flex items-center gap-2">
                  <button
                    onClick={() => navigate(`/jobs/${candidate.jobId}`)}
                    className="text-sm text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 flex items-center gap-1"
                  >
                    ← Tilbage
                  </button>
                  <h1 className="text-lg font-bold text-blue-700 dark:text-blue-400">{candidate.name}</h1>
                </div>
                <div className="flex items-center gap-2 flex-wrap">
                  <button className="text-xs bg-gray-200 dark:bg-gray-700 text-gray-800 dark:text-gray-200 px-3 py-1 rounded-full hover:bg-gray-300 dark:hover:bg-gray-600 transition">
                    Inviter til samtale
                  </button>
                  <button className="text-xs bg-gray-200 dark:bg-gray-700 text-gray-800 dark:text-gray-200 px-3 py-1 rounded-full hover:bg-gray-300 dark:hover:bg-gray-600 transition">
                    Afvis kandidat
                  </button>
                  <button className="text-xs bg-red-100 dark:bg-red-700 text-red-800 dark:text-red-200 px-3 py-1 rounded-full hover:bg-red-200 dark:hover:bg-red-600 transition">
                    Slet
                  </button>
                </div>
              </div>

              <h2 className="text-xl font-semibold text-gray-800 dark:text-white">Beskrivelse</h2>
              <p className="text-gray-700 dark:text-gray-300">{candidate.profileSummary}</p>

              <h2 className="text-xl font-semibold text-gray-800 dark:text-white">Færdigheder</h2>
              <ul className="flex flex-wrap gap-2 text-gray-800 dark:text-gray-200">
                {candidate.skills.map((skill, i) => (
                  <li key={i} className="bg-gray-200 dark:bg-gray-700 px-3 py-1 rounded-full text-sm">
                    {skill}
                  </li>
                ))}
              </ul>

              <h2 className="text-xl font-semibold text-gray-800 dark:text-white">Styrker</h2>
              <ul className="list-disc list-inside text-gray-700 dark:text-gray-300">
                {candidate.strengths.map((s, i) => <li key={i}>{s}</li>)}
              </ul>

              <h2 className="text-xl font-semibold text-gray-800 dark:text-white">Svagheder</h2>
              <ul className="list-disc list-inside text-gray-700 dark:text-gray-300">
                {candidate.weaknesses.map((w, i) => <li key={i}>{w}</li>)}
              </ul>

              <h2 className="text-xl font-semibold text-gray-800 dark:text-white">Analyse</h2>
              <p className="text-gray-700 dark:text-gray-300">{candidate.analysisSummary}</p>

              <div className="mt-6">
                <span className="inline-block bg-blue-600 text-white px-3 py-1 rounded-full font-semibold text-sm">
                  Match Score: {candidate.matchScore}%
                </span>
              </div>
            </div>

            {/* RIGHT SIDE */}
            <div className="bg-white dark:bg-gray-800 rounded shadow overflow-hidden h-full flex flex-col">
              <div className="border-b px-4 py-2 font-semibold text-gray-800 dark:text-white">
                CV
              </div>
              <iframe
                src={`http://localhost:5000/api/candidates/profile/${candidate.id}/cv`}
                className="flex-1 w-full rounded-b"
              />
            </div>
          </div>
        </div>
      </div>
    </>
  );
};

export default CandidateDetail;
