import { useParams } from "react-router-dom";
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
      <Header />
      <div className="min-h-screen bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-gray-100">
        <div className="max-w-5xl mx-auto px-6 py-10 space-y-6">
          <h1 className="text-3xl font-bold text-blue-700 dark:text-blue-400">{candidate.name}</h1>
          <ul>
            <li>Email: {candidate.email}</li>
            <li>Telefonnummer: {candidate.phone}</li>
            <li>Lokation: {candidate.location}</li>
          </ul>

          <div className="bg-white dark:bg-gray-800 rounded shadow p-6 space-y-4">
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
        </div>
      </div>
    </>
  );
};

export default CandidateDetail;
