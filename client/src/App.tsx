import { BrowserRouter, Routes, Route } from "react-router";
import { ToastContainer } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";
import Home from "./Views/Home";
import Game from "./Views/Game";
import BaseLayout from "./Views/BaseLayout";

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Home />} />

        <Route element= {<BaseLayout/>}>
          <Route path="/game" element={<Game />} />
        </Route>
        
      </Routes>
      <ToastContainer />
    </BrowserRouter>
  );
}

export default App;
