import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { fetchJobs, createJob, Job } from "../services/api";
import JobCard from "../components/JobCard";
import Header from "../components/Header";
import SettingsModal from "../components/SettingsModal";

const JobDashboard = () => {
  const { t } = useTranslation();
  const [jobs, setJobs] = useState<Job[]>([]);
  const [showForm, setShowForm] = useState(false);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [loading, setLoading] = useState(true);
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);

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
            className="w-full p-2 border rounded bg-white text-gray-900 dark:bg-gray-700 dark:text-white"
          />
          <textarea
            placeholder={t('jobs.jobDescription')}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            rows={6}
            className="w-full p-2 border rounded bg-white text-gray-900 dark:bg-gray-700 dark:text-white"
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

      {/* Floating Settings Button */}
      <button
        onClick={() => setIsSettingsOpen(true)}
        className="fixed bottom-6 right-6 bg-blue-600 text-white p-4 rounded-full shadow-lg hover:bg-blue-700 hover:scale-110 transition-all duration-200 z-40"
        aria-label="Settings"
      >
        <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
        </svg>
      </button>

      <SettingsModal isOpen={isSettingsOpen} onClose={() => setIsSettingsOpen(false)} />
    </div>
  );
};

export default JobDashboard;
