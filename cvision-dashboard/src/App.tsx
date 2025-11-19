import { BrowserRouter, Routes, Route } from "react-router-dom";
import { AuthProvider } from "./context/AuthContext";
import JobDashboard from "./pages/JobDashboard";
import JobDetail from "./pages/JobDetail";
import CandidateDetail from "./pages/CandidateProfile";
import LoginPage from "./pages/LoginPage";
import RegisterPage from "./pages/RegisterPage";

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/" element={<JobDashboard />} />
          <Route path="/jobs/:id" element={<JobDetail />} />
          <Route path="/jobs/:jobId/candidates/:id" element={<CandidateDetail />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
