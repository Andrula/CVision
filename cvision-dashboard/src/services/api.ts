const API_BASE = "http://localhost:5000/api";

export type Job = {
  id: number;
  title: string;
  description: string;
  createdAt: string;
  applicantCount: number;
};

export async function fetchJobs(): Promise<Job[]> {
  const res = await fetch(`${API_BASE}/jobs`);
  if (!res.ok) throw new Error("Failed to fetch jobs");
  return await res.json();
}

export async function createJob(job: { title: string; description: string }): Promise<Job> {
  const res = await fetch(`${API_BASE}/jobs`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(job),
  });
  if (!res.ok) throw new Error("Failed to create job");
  return await res.json();
}
