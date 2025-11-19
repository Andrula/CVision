import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { fetchJobs, createJob, Job } from "../services/api";
import JobCard from "../components/JobCard";
import Header from "../components/Header";

const JobDashboard = () => {
  const { t } = useTranslation();
  const [jobs, setJobs] = useState<Job[]>([]);
  const [showForm, setShowForm] = useState(false);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [loading, setLoading] = useState(true);
  const [darkMode, setDarkMode] = useState(false);

  useEffect(() => {
    fetchJobs()
      .then(setJobs)
      .catch((err) => console.error("Error fetching jobs:", err))
      .finally(() => setLoading(false));
  }, []);

  const handleAddJob = async () => {
    const newJob = await createJob({ title, description });
    setJobs([newJob, ...jobs]);
    setTitle("");
    setDescription("");
    setShowForm(false);
  };

  return (
    <div className="min-h-screen w-screen bg-white text-gray-900 dark:bg-gray-800 dark:text-gray-100 flex flex-col">
      <Header />

      <div className="text-center mt-6">
        <h2 className="text-4xl font-extrabold text-blue-700 dark:text-blue-400">CVision</h2>
        <p className="text-xl font-semibold text-gray-800 dark:text-gray-300 mt-1">{t('jobs.title')}</p>
        <button
          onClick={() => setShowForm(!showForm)}
          className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 mt-3"
        >
          {showForm ? t('common.cancel') : t('jobs.newPosition')}
        </button>
      </div>

      <hr className="border-t border-gray-300 dark:border-gray-600 my-6" />

      {showForm && (
        <div className="max-w-3xl mx-auto space-y-4 border p-4 rounded bg-gray-50 dark:bg-gray-800 mt-6">
          <input
            type="text"
            placeholder={t('jobs.jobTitle')}
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            className="w-full p-2 border border-gray-300 rounded bg-white text-gray-900 dark:bg-gray-700 dark:text-white dark:border-gray-600"
          />
          <textarea
            placeholder={t('jobs.jobDescription')}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            rows={6}
            className="w-full p-2 border border-gray-300 rounded bg-white text-gray-900 dark:bg-gray-700 dark:text-white dark:border-gray-600"
          />
          <button
            onClick={handleAddJob}
            className="bg-green-600 text-white px-4 py-2 rounded hover:bg-green-700"
          >
            {t('jobs.savePosition')}
          </button>
        </div>
      )}

      {loading ? (
        <p className="text-center text-gray-500 mt-4">{t('jobs.loadingJobs')}</p>
      ) : (
        !showForm && ( 
          <div className="flex flex-wrap gap-4 justify-center w-full px-6 pb-8">
            {jobs.map((job) => (
              <JobCard
                key={job.id}
                id={job.id}
                title={job.title}
                description={job.description}
                createdAt={job.createdAt}
                applicantCount={job.applicantCount}
              />
            ))}
          </div>
        )
      )}
    </div>
  );
};

export default JobDashboard;
