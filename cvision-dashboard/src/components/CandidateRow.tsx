import { Link } from "react-router-dom";

type CandidateRowProps = {
  candidate: {
    id: number;
    jobId: number;
    name?: string;
    role?: string;
    matchScore?: number;
  };
};

const CandidateRow = ({ candidate }: CandidateRowProps) => {
  const { id, jobId, name, role, matchScore } = candidate;

  return (
    <Link
      to={`/jobs/${jobId}/candidates/${id}`}
      className="flex justify-between items-center p-4 border-b bg-white dark:bg-gray-700 hover:bg-gray-100 dark:hover:bg-gray-600 transition"
    >
      <div>
        <p className="font-semibold text-gray-900 dark:text-white">
          {name ?? "Unnamed Candidate"}
        </p>
        <p className="text-sm text-gray-500 dark:text-gray-300">
          {role ?? "Unknown role"}
        </p>
      </div>

      <div className="flex items-center gap-3">
        <span className="bg-cyan-500 text-white text-xs font-bold px-2 py-1 rounded-full">
          {matchScore ?? "--"}%
        </span>
        <span className="text-sm text-blue-600 dark:text-blue-400 font-medium">
          Match →
        </span>
      </div>
    </Link>
  );
};

export default CandidateRow;
