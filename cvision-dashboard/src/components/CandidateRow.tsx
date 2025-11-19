import { Link } from "react-router-dom";

type CandidateRowProps = {
  candidate: {
    id: number;
    jobId: number;
    name: string;
    matchScore: number;
    status?: number;
    errorMessage?: string;
  };
};

const CandidateRow = ({ candidate }: CandidateRowProps) => {
  const { id, jobId, name, matchScore, status, errorMessage } = candidate;

  const getStatusBadge = () => {
    switch (status) {
      case 0: // Pending
        return (
          <span className="bg-yellow-100 text-yellow-800 text-xs font-medium px-2 py-1 rounded">
            ⏳ Pending
          </span>
        );
      case 1: // Processing
        return (
          <span className="bg-blue-100 text-blue-800 text-xs font-medium px-2 py-1 rounded animate-pulse">
            ⚙️ Processing...
          </span>
        );
      case 2: // Completed
        return (
          <span className="bg-green-100 text-green-800 text-xs font-medium px-2 py-1 rounded">
            ✓ Completed
          </span>
        );
      case 3: // Failed
        return (
          <span className="bg-red-100 text-red-800 text-xs font-medium px-2 py-1 rounded" title={errorMessage || "Unknown error"}>
            ✗ Failed
          </span>
        );
      case 4: // Cached
        return (
          <span className="bg-purple-100 text-purple-800 text-xs font-medium px-2 py-1 rounded">
            💾 Cached
          </span>
        );
      default:
        return null;
    }
  };

  return (
    <Link
      to={`/jobs/${jobId}/candidates/${id}`}
      className="flex justify-between items-center p-4 border-b bg-white dark:bg-gray-700 hover:bg-gray-100 dark:hover:bg-gray-600 transition"
    >
      <div className="flex flex-col">
        <p className="font-semibold text-gray-900 dark:text-white">{name}</p>
        {status !== undefined && status !== 2 && status !== 4 && (
          <div className="mt-1">{getStatusBadge()}</div>
        )}
      </div>

      <div className="flex items-center gap-4">
        {(status === 2 || status === 4) && getStatusBadge()}
        {matchScore > 0 && (
          <span className="bg-cyan-500 text-white text-xs font-bold px-3 py-1 rounded-full">
            {matchScore}%
          </span>
        )}
        <span className="text-sm text-blue-600 dark:text-blue-400 font-medium">
          Profil →
        </span>
      </div>
    </Link>
  );
};

export default CandidateRow;
