const API_BASE = "http://localhost:5000/api";

export type Job = {
  id: number;
  title: string;
  description: string;
  createdAt: string;
  applicantCount: number;
};

export type Candidate = {
  id: number;
  jobId: number;
  name: string;
  matchScore: number;
  profileSummary: string;
  experienceYears: number;
};

// Helper function to get auth headers
function getAuthHeaders(): HeadersInit {
  const token = localStorage.getItem("authToken");
  const headers: HeadersInit = {
    "Content-Type": "application/json",
  };

  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  return headers;
}

// Helper function for multipart/form-data requests
function getAuthHeadersMultipart(): HeadersInit {
  const token = localStorage.getItem("authToken");
  const headers: HeadersInit = {};

  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  return headers;
}

export async function fetchJobs(): Promise<Job[]> {
  const res = await fetch(`${API_BASE}/jobs`, {
    headers: getAuthHeaders(),
  });
  if (!res.ok) throw new Error("Failed to fetch jobs");
  return await res.json();
}

export async function createJob(job: { title: string; description: string }): Promise<Job> {
  const res = await fetch(`${API_BASE}/jobs`, {
    method: "POST",
    headers: getAuthHeaders(),
    body: JSON.stringify(job),
  });
  if (!res.ok) throw new Error("Failed to create job");
  return await res.json();
}

export async function fetchCandidateProfiles(jobId: number) {
  const res = await fetch(`${API_BASE}/jobs/${jobId}/profiles`, {
    headers: getAuthHeaders(),
  });
  if (!res.ok) throw new Error("Failed to fetch candidate profiles");
  return await res.json();
}

export async function fetchSkillDistribution(jobId: number): Promise<{ skill: string; count: number }[]> {
  const res = await fetch(`${API_BASE}/jobs/${jobId}/skills`, {
    headers: getAuthHeaders(),
  });
  if (!res.ok) throw new Error("Failed to fetch skill distribution");
  return await res.json();
}

export async function uploadCV(jobId: number, file: File): Promise<any> {
  const formData = new FormData();
  formData.append("jobId", String(jobId));
  formData.append("file", file);

  console.log("Uploading to backend...");
  const res = await fetch(`${API_BASE}/candidates/upload`, {
    method: "POST",
    headers: getAuthHeadersMultipart(),
    body: formData,
  });

  if (!res.ok) throw new Error("Failed to upload CV");
  return await res.json();
}



