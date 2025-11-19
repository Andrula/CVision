import { useParams, useNavigate } from "react-router-dom";
import { useEffect, useRef, useState, DragEvent } from "react";
import { useTranslation } from "react-i18next";
import Header from "../components/Header";
import { fetchSkillDistribution } from "../services/api";
import CandidateRow from "../components/CandidateRow";
import MatchScoreChart from "../components/MatchScoreChart";
import SkillDistributionChart from "../components/SkillDistributionChart";
import ExperienceMatchScoreCorrelation from "../components/ExperienceMatchScoreCorrelation";

const JobDetail = () => {
  const { t, i18n } = useTranslation();
  const { id } = useParams();
  const [job, setJob] = useState<any>(null);
  const [candidates, setCandidates] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [isDragging, setIsDragging] = useState(false);
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const [uploadingIndex, setUploadingIndex] = useState<number | null>(null);
  const [uploadStarted, setUploadStarted] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [skills, setSkills] = useState<{ skill: string; count: number }[]>([]);
  const navigate = useNavigate();
  const [pollingInterval, setPollingInterval] = useState<NodeJS.Timeout | null>(null);

  const fetchCandidates = async () => {
    try {
      const candRes = await fetch(`http://localhost:5000/api/jobs/${id}/candidates`);
      const candData = await candRes.json();
      setCandidates(candData);

      // Check if there are any candidates still processing
      const hasProcessing = candData.some((c: any) => c.status === 0 || c.status === 1); // Pending or Processing

      if (hasProcessing && !pollingInterval) {
        // Start polling
        const interval = setInterval(async () => {
          const res = await fetch(`http://localhost:5000/api/jobs/${id}/candidates`);
          const data = await res.json();
          setCandidates(data);

          // Stop polling if no more processing candidates
          const stillProcessing = data.some((c: any) => c.status === 0 || c.status === 1);
          if (!stillProcessing) {
            clearInterval(interval);
            setPollingInterval(null);

            // Refresh skills when all done
            fetchSkillDistribution(Number(id))
              .then(setSkills)
              .catch((err) => console.error("Failed to refresh skills", err));
          }
        }, 3000); // Poll every 3 seconds

        setPollingInterval(interval);
      }
    } catch (err) {
      console.error("Failed to fetch candidates", err);
    }
  };

  useEffect(() => {
    const fetchJobAndCandidates = async () => {
      try {
        const jobRes = await fetch(`http://localhost:5000/api/jobs/${id}`);
        const jobData = await jobRes.json();
        setJob(jobData);

        await fetchCandidates();

        fetchSkillDistribution(Number(id))
          .then(setSkills)
          .catch((err) => console.error("Failed to fetch skills", err));
      } catch (err) {
        console.error("Failed to fetch job or candidates", err);
      } finally {
        setLoading(false);
      }
    };

    fetchJobAndCandidates();

    // Cleanup polling on unmount
    return () => {
      if (pollingInterval) {
        clearInterval(pollingInterval);
      }
    };
  }, [id]);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files ?? []);
    setSelectedFiles(files);
  };

  const handleDrop = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragging(false);
    const files = Array.from(e.dataTransfer.files);
    setSelectedFiles(files);
  };

  const handleFileClick = () => {
    fileInputRef.current?.click();
  };

  const handleUpload = async () => {
    console.log("Upload started");
    if (!selectedFiles.length) return;

    setUploadStarted(true);
    setUploadingIndex(0);

    // Upload all files to queue (fast, non-blocking)
    for (let i = 0; i < selectedFiles.length; i++) {
      const file = selectedFiles[i];
      const formData = new FormData();
      formData.append("file", file);
      formData.append("jobId", id!.toString());
      formData.append("language", i18n.language); // Send current language for parsing

      try {
        const res = await fetch("http://localhost:5000/api/candidates/upload", {
          method: "POST",
          body: formData,
        });
        if (!res.ok) throw new Error("Upload failed");

        const candidate = await res.json();
        setCandidates((prev) => [...prev, candidate]);
      } catch (err) {
        console.error("Error uploading file:", err);
      }

      setUploadingIndex(i + 1);
    }

    setUploadingIndex(null);
    setUploadStarted(false);
    setSelectedFiles([]);

    // Refresh candidates and start polling for status updates
    await fetchCandidates();
  };

  if (loading) return <p className="p-6">{t('common.loading')}</p>;
  if (!job) return <p className="p-6">{t('jobs.jobNotFound')}</p>;

  return (
    <>
      <Header />
      <div className="min-h-screen w-screen bg-gray-50 dark:bg-gray-800 text-gray-900 dark:text-gray-100 p-6 flex flex-col">
        <div className="w-full mx-auto">

          {(uploadStarted) ? (
            <>
              <h1 className="text-3xl font-bold text-blue-700 dark:text-blue-400 mb-4">{job.title}</h1>
              <p className="text-gray-600 dark:text-gray-300 whitespace-pre-line mb-6 line-clamp-6">{job.description}</p>

              <div className={`border-2 border-dashed rounded-lg p-10 text-center bg-white dark:bg-gray-700 border-gray-300 dark:border-gray-600`}>
                <div className="flex flex-col items-center justify-center space-y-4">
                  <div className="w-10 h-10 border-4 border-blue-400 border-t-transparent rounded-full animate-spin"></div>
                  <p className="text-blue-600 dark:text-blue-400 font-semibold">
                    {t('upload.processingCVs', { current: uploadingIndex, total: selectedFiles.length })}
                  </p>
                </div>
              </div>
            </>
          ) : candidates.length === 0 ? (
            <>
              <h1 className="text-3xl font-bold text-blue-700 dark:text-blue-400 mb-4">{job.title}</h1>
              <p className="text-gray-600 dark:text-gray-300 whitespace-pre-line mb-6 line-clamp-6">{job.description}</p>

              <div
                className={`border-2 border-dashed rounded-lg p-10 text-center cursor-pointer
                  ${isDragging ? "bg-blue-100 border-blue-400" : "bg-white dark:bg-gray-700 border-gray-300 dark:border-gray-600"}`}
                onClick={handleFileClick}
                onDragOver={(e) => {
                  e.preventDefault();
                  setIsDragging(true);
                }}
                onDragLeave={() => setIsDragging(false)}
                onDrop={handleDrop}
              >
                <p className="text-lg mb-2">{t('upload.dragDrop')}</p>
                <p className="text-sm text-gray-500 dark:text-gray-400">
                  {t('upload.fileTypes')}
                </p>
                <input
                  type="file"
                  multiple
                  ref={fileInputRef}
                  className="hidden"
                  onChange={handleFileChange}
                />
              </div>

              {selectedFiles.length > 0 && (
                <div className="mt-4 text-center">
                  <button
                    onClick={handleUpload}
                    className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700"
                  >
                    {t('jobs.uploadFiles', { count: selectedFiles.length })}
                  </button>
                </div>
              )}
            </>
          ) : (
            <>
              <div className="flex flex-wrap justify-between items-center px-2 py-2 gap-4 border-b border-gray-300 dark:border-gray-700 mb-6">
                {/* LEFT SIDE */}
                <div className="flex flex-wrap items-center gap-2">
                  <button
                    onClick={() => navigate(`/`)}
                    className="text-sm text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 flex items-center gap-1"
                  >
                    ← {t('common.back')}
                  </button>

                  <h1 className="text-xl font-bold text-blue-700 dark:text-blue-400">{job.title}</h1>

                  <span className="text-xs bg-gray-200 dark:bg-gray-700 text-gray-800 dark:text-gray-200 px-3 py-1 rounded-full flex items-center gap-1">
                    👥 {candidates.length} {candidates.length === 1 ? t('jobs.applicant') : t('jobs.applicants')}
                  </span>

                  <span className="text-xs bg-gray-200 dark:bg-gray-700 text-gray-800 dark:text-gray-200 px-3 py-1 rounded-full flex items-center gap-1">
                    📅 {job.createdAt ? new Date(job.createdAt).toLocaleDateString() : t('common.unknown')}
                  </span>
                </div>

                {/* RIGHT SIDE */}
                <div className="flex items-center gap-4 flex-wrap">
                  <button
                    onClick={() => {/* download logic */ }}
                    className="text-sm text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400"
                  >
                    {t('jobs.downloadReport')}
                  </button>

                  <button
                    onClick={() => fileInputRef.current?.click()}
                    className="text-sm text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400"
                  >
                    {t('jobs.addMoreCVs')}
                  </button>

                  <button
                    onClick={() => setCandidates([])}
                    className="text-sm text-red-600 dark:text-red-400 hover:text-red-700 dark:hover:text-red-300"
                  >
                    {t('jobs.deleteAll')}
                  </button>
                </div>

              </div>


              <div className="grid grid-cols-1 lg:grid-cols-4 gap-8 px-6 py-10">
                <div className="lg:col-span-3 space-y-8">
                  <div>
                    <h1 className="text-3xl font-bold text-blue-700 dark:text-blue-400">{job.title}</h1>
                    <p className="text-gray-700 dark:text-gray-300 mt-2 line-clamp-6">{job.description}</p>

                    <hr className="border-gray-300 dark:border-gray-600 mt-4" />
                  </div>

                  <MatchScoreChart matchScores={candidates.map(c => c.matchScore)} />
                  <ExperienceMatchScoreCorrelation candidates={candidates} />
                  <SkillDistributionChart data={skills} />
                </div>


                <div className="bg-white dark:bg-gray-800 rounded shadow p-4 h-fit">
                  <h2 className="text-xl font-semibold mb-4 text-gray-800 dark:text-gray-100">
                    {t('jobs.applicants')} ({candidates.length})
                  </h2>
                  <div className="space-y-4">
                    {candidates
                      .slice()
                      .sort((a, b) => b.matchScore - a.matchScore)
                      .map((candidate) => (
                        <CandidateRow key={candidate.id} candidate={candidate} />
                      ))}
                  </div>
                </div>
              </div>
            </>
          )}
        </div>
      </div>
    </>
  );
};

export default JobDetail;
