type ParsingOverlayProps = {
    current: number;
    total: number;
  };
  
  const ParsingOverlay = ({ current, total }: ParsingOverlayProps) => {
    return (
      <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
        <div className="bg-white dark:bg-gray-800 p-6 rounded shadow text-center">
          <h2 className="text-xl font-semibold text-blue-700 dark:text-blue-400">
            Behandler {current} ud af {total} ansøgere...
          </h2>
          <p className="text-gray-500 dark:text-gray-300 mt-2">Dette kan tage et øjeblik</p>
        </div>
      </div>
    );
  };
  
  export default ParsingOverlay;
  