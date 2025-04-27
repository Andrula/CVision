import { BrowserRouter, Routes, Route } from "react-router-dom";
import JobDashboard from "./pages/JobDashboard";
import JobDetail from "./pages/JobDetail";
import CandidateDetail from "./pages/CandidateProfile";

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<JobDashboard />} />
        <Route path="/jobs/:id" element={<JobDetail />} />
        <Route path="/jobs/:jobId/candidates/:id" element={<CandidateDetail />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
