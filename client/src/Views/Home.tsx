import { useState } from "react";
import { useNavigate } from "react-router";
import { ShowSuccess } from "../Constant/ToastUI";

export default function Home() {
  const [playerNames, setPlayerNames] = useState<string[]>([]);
  const [playerInput, setPlayerInput] = useState("");
  const navigate = useNavigate();

  const handleAddPlayer = () => {
    if (playerInput.trim() && playerNames.length < 4) {
      setPlayerNames([...playerNames, playerInput.trim()]);
      setPlayerInput("");
    }
  };

  const handleStartGame = () => {
    if (playerNames.length >= 2) {
      localStorage.setItem("playerNames", JSON.stringify(playerNames));
      ShowSuccess("Game started successfully");
      navigate("/game");
    }
  };

  return (
    <div className="flex flex-col items-center justify-center min-h-screen bg-gray-100 p-5">
      <div className="bg-white p-10 rounded-xl shadow-2xl w-full max-w-md text-center">
        <h1 className="text-6xl font-bold text-white bg-red-600 border-2 border-black py-1 px-5 mb-8 inline-block">
          Monopoly
        </h1>

        <div className="mb-6 text-left">
          <h3 className="text-xl font-bold text-gray-800">
            Players ({playerNames.length}/4)
          </h3>
          <ul className="mt-3 list-none p-0">
            {playerNames.map((name, idx) => (
              <li
                key={idx}
                className="bg-gray-200 text-gray-800 p-3 my-2 rounded-lg text-base"
              >
                {name}
              </li>
            ))}
          </ul>
        </div>

        {playerNames.length < 4 && (
          <div className="flex gap-3 mb-6">
            <input
              type="text"
              value={playerInput}
              onChange={(e) => setPlayerInput(e.target.value)}
              placeholder="Enter player name"
              className="flex-grow p-3 border border-gray-300 rounded-lg text-base focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            <button
              onClick={handleAddPlayer}
              className="px-6 py-3 bg-green-500 text-white rounded-lg text-base font-semibold hover:bg-green-600 transition-colors"
            >
              Add
            </button>
          </div>
        )}

        {playerNames.length >= 2 && (
          <button
            onClick={handleStartGame}
            className="w-full py-4 text-xl bg-blue-600 text-white rounded-lg font-bold hover:bg-blue-700 transition-colors disabled:bg-gray-400 disabled:cursor-not-allowed"
            disabled={playerNames.length < 2}
          >
            Start Game
          </button>
        )}
      </div>
    </div>
  );
}
