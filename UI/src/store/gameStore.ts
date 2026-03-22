import { create } from "zustand";

type GameState = {
  selectedPlayerId: number | null;
  selectedCharacterId: number | null;
  selectedEnemyId: number | null;
  setSelectedPlayerId: (playerId: number | null) => void;
  setSelectedCharacterId: (characterId: number | null) => void;
  setSelectedEnemyId: (enemyId: number | null) => void;
};

export const useGameStore = create<GameState>((set) => ({
  selectedPlayerId: null,
  selectedCharacterId: null,
  selectedEnemyId: null,
  setSelectedPlayerId: (playerId) => set({ selectedPlayerId: playerId }),
  setSelectedCharacterId: (characterId) => set({ selectedCharacterId: characterId }),
  setSelectedEnemyId: (enemyId) => set({ selectedEnemyId: enemyId })
}));
