import { useTheme } from "../context/ThemeContext";
import { Link } from "react-router-dom";

const Header = () => {
  const { darkMode, toggleDarkMode } = useTheme();

  return (
    <header className="w-full px-6 py-4 flex justify-between items-center border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800">
      <Link to="/" className="text-2xl font-bold text-blue-700 dark:text-blue-700 hover:opacity-80 transition">
        CVision
      </Link>

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
    </header>
  );
};

export default Header;
