import { useTheme } from "../context/ThemeContext";
import { useAuth } from "../context/AuthContext";
import { Link, useNavigate } from "react-router-dom";

const Header = () => {
  const { darkMode, toggleDarkMode } = useTheme();
  const { isAuthenticated, user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <header className="w-full px-6 py-4 flex justify-between items-center border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800">
      <Link to={isAuthenticated ? "/dashboard" : "/login"} className="text-2xl font-bold text-blue-700 dark:text-blue-700 hover:opacity-80 transition">
        CVision
      </Link>

      <div className="flex items-center gap-4">
        {/* User Info and Auth Buttons */}
        {isAuthenticated ? (
          <div className="flex items-center gap-4">
            <span className="text-sm text-gray-700 dark:text-gray-300">
              Welcome, <span className="font-semibold">{user?.name}</span>
            </span>
            <button
              onClick={handleLogout}
              className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 transition"
            >
              Logout
            </button>
          </div>
        ) : (
          <div className="flex items-center gap-3">
            <Link
              to="/login"
              className="px-4 py-2 text-sm font-medium text-blue-600 dark:text-blue-400 hover:underline"
            >
              Login
            </Link>
            <Link
              to="/register"
              className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 transition"
            >
              Sign Up
            </Link>
          </div>
        )}

        {/* Toggle Switch */}
        <div
          onClick={toggleDarkMode}
          className="w-12 h-6 flex items-center bg-gray-300 dark:bg-blue-600 rounded-full p-1 cursor-pointer transition-colors duration-300"
        >
          <div
            className={`w-4 h-4 rounded-full shadow-md transform transition-transform duration-300 ${
              darkMode ? "translate-x-6 bg-white" : "translate-x-0 bg-red-400"
            }`}
          />
        </div>
      </div>
    </header>
  );
};

export default Header;
