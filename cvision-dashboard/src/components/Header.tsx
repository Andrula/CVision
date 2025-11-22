import { useTheme } from "../context/ThemeContext";
import { useAuth } from "../context/AuthContext";
import { Link, useNavigate } from "react-router-dom";

const Header = () => {
  const { darkMode, toggleDarkMode } = useTheme();
  const { user, logout, isAuthenticated } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <header className="w-full px-6 py-4 flex justify-between items-center border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800">
      <Link to="/" className="text-2xl font-bold text-blue-700 dark:text-blue-700 hover:opacity-80 transition">
        CVision
      </Link>

      <div className="flex items-center gap-4">
        {isAuthenticated && user && (
          <div className="flex items-center gap-3">
            <span className="text-sm text-gray-700 dark:text-gray-300">
              {user.fullName}
            </span>
            <button
              onClick={handleLogout}
              className="text-sm text-red-600 dark:text-red-400 hover:text-red-700 dark:hover:text-red-300"
            >
              Logout
            </button>
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
