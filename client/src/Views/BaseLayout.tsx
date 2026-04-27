import { useEffect } from "react";
import { Outlet, useNavigate } from "react-router";
import { ShowError } from "../Constant/ToastUI";

export default function BaseLayout() {
  const navigate = useNavigate();
    const hasPlayerNames = localStorage.getItem("playerNames") ? true : false;

  useEffect(() => {
    if (!hasPlayerNames) {
      ShowError("Please enter player names first.");
      navigate("/");
    }
  }, [hasPlayerNames, navigate]);

  if (!hasPlayerNames) return null;

  return (
    <Outlet />
  );
}