import { BrowserRouter, Routes, Route } from "react-router-dom";
import JobDashboard from "./pages/JobDashboard";
import JobDetail from "./pages/JobDetail";
import CandidateDetail from "./pages/CandidateProfile";
import Login from "./pages/Login";
import Register from "./pages/Register";

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route path="/" element={<JobDashboard />} />
        <Route path="/jobs/:id" element={<JobDetail />} />
        <Route path="/jobs/:jobId/candidates/:id" element={<CandidateDetail />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
