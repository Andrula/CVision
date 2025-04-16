import { useParams } from "react-router-dom";
import { useEffect, useRef, useState, DragEvent } from "react";
import Header from "../components/Header";
import CandidateRow from "../components/CandidateRow";

const JobDetail = () => {
  const { id } = useParams();
  const [job, setJob] = useState<any>(null);
  const [candidates, setCandidates] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [isDragging, setIsDragging] = useState(false);
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    const fetchJobAndCandidates = async () => {
      try {
        const jobRes = await fetch(`http://localhost:5000/api/jobs/${id}`);
        const jobData = await jobRes.json();
        setJob(jobData);

        const candRes = await fetch(`http://localhost:5000/api/jobs/${id}/candidates`);
        const candData = await candRes.json();
        setCandidates(candData);
      } catch (err) {
        console.error("Failed to fetch job or candidates", err);
      } finally {
        setLoading(false);
      }
    };

    fetchJobAndCandidates();
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
    if (!selectedFiles.length) return;

    for (const file of selectedFiles) {
      const formData = new FormData();
      formData.append("file", file);
      formData.append("jobId", id!.toString());

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
    }

    setSelectedFiles([]); // Clear after upload
  };


  if (loading) return <p className="p-6">Loading...</p>;
  if (!job) return <p className="p-6">Job not found.</p>;

  return (
    <>
      <Header />
      <div className="min-h-screen w-screen bg-gray-50 text-gray-900 dark:bg-gray-800 dark:text-gray-100 p-6 flex flex-col">
        <div className="max-w-5xl w-full mx-auto">
          <h1 className="text-3xl font-bold text-blue-700 dark:text-blue-400">{job.title}</h1>
          <p className="text-gray-600 dark:text-gray-300 mt-2">{job.description}</p>

          <hr className="my-6 border-gray-300 dark:border-gray-600" />

          {candidates.length === 0 ? (
            <>
              <div
                className={`border-2 border-dashed rounded-lg p-10 text-center transition-colors duration-300 cursor-pointer
                ${isDragging ? "bg-blue-100 border-blue-400" : "bg-white dark:bg-gray-700 border-gray-300 dark:border-gray-600"}`}
                onClick={handleFileClick}
                onDragOver={(e) => {
                  e.preventDefault();
                  setIsDragging(true);
                }}
                onDragLeave={() => setIsDragging(false)}
                onDrop={handleDrop}
              >
                <p className="text-lg mb-2">Drag & drop CVs here or click to upload</p>
                <p className="text-sm text-gray-500 dark:text-gray-400">
                  PDF, DOCX, etc. — multiple files supported
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
                    Upload {selectedFiles.length} file{selectedFiles.length > 1 ? "s" : ""}
                  </button>
                </div>
              )}
            </>
          ) : (
            <ul className="mt-6">
            {candidates
              .sort((a, b) => (b.matchScore ?? 0) - (a.matchScore ?? 0))
              .map((c) => (
                <CandidateRow key={c.id} candidate={{ ...c, jobId: Number(id) }} />
              ))}
          </ul>
          )}
        </div>
      </div>
    </>
  );
};

export default JobDetail;
