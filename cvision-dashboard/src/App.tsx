import { BrowserRouter, Routes, Route } from "react-router-dom";
import JobDashboard from "./pages/JobDashboard";
import JobDetail from "./pages/JobDetail";

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<JobDashboard />} />
        <Route path="/jobs/:id" element={<JobDetail />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
