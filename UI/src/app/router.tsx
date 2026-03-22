import { createBrowserRouter } from "react-router-dom";
import { HomePage } from "../features/home/HomePage";
import { BattlePage } from "../features/battle/BattlePage";
import { CharactersPage } from "../features/characters/CharactersPage";
import { ProgressionPage } from "../features/progression/ProgressionPage";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <HomePage />
  },
  {
    path: "/battle",
    element: <BattlePage />
  },
  {
    path: "/characters",
    element: <CharactersPage />
  },
  {
    path: "/progression",
    element: <ProgressionPage />
  }
]);
