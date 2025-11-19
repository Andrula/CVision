const API_BASE = "http://localhost:5000/api";

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  companyName: string;
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  user: {
    id: string;
    email: string;
    name: string;
  };
}

export async function login(credentials: LoginRequest): Promise<AuthResponse> {
  const res = await fetch(`${API_BASE}/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      Email: credentials.email,
      Password: credentials.password,
    }),
  });

  if (!res.ok) {
    const error = await res.text();
    throw new Error(error || "Login failed");
  }

  return await res.json();
}

export async function register(userData: RegisterRequest): Promise<AuthResponse> {
  const res = await fetch(`${API_BASE}/auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      FullName: userData.fullName,
      CompanyName: userData.companyName,
      Email: userData.email,
      Password: userData.password,
    }),
  });

  if (!res.ok) {
    const error = await res.text();
    throw new Error(error || "Registration failed");
  }

  return await res.json();
}
