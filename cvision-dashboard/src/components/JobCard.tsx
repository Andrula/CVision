import { Link } from "react-router-dom";

type JobCardProps = {
  id: number;
  title: string;
  description: string;
  createdAt?: string;
  applicantCount?: number;
};

const JobCard = ({ id, title, description, createdAt, applicantCount = 0 }: JobCardProps) => {
  return (
    <Link to={`/jobs/${id}`} className="w-full sm:w-[300px]">
      <div className="h-full flex flex-col justify-between p-4 border rounded shadow hover:bg-gray-50 dark:hover:bg-gray-700 bg-white dark:bg-gray-800 cursor-pointer transition min-h-[220px]">
        {/* Title */}
        <h2 className="text-lg font-semibold text-blue-700 dark:text-blue-400 mb-1">
          {title}
        </h2>

        <p className="text-sm text-gray-600 dark:text-gray-300 overflow-hidden text-ellipsis line-clamp-3">
          {description}
        </p>

        <div className="flex justify-between items-center mt-4">
          <span className="text-xs bg-gray-200 dark:bg-gray-700 text-gray-800 dark:text-gray-200 px-3 py-1 rounded-full flex items-center gap-1">
            👥 {applicantCount} {applicantCount === 1 ? "Applicant" : "Applicants"}
          </span>
          <span className="text-xs bg-gray-200 dark:bg-gray-700 text-gray-800 dark:text-gray-200 px-3 py-1 rounded-full flex items-center gap-1">
            📅 {createdAt ? new Date(createdAt).toLocaleDateString() : "Unknown"}
          </span>
        </div>
      </div>
    </Link>
  );
};

export default JobCard;
