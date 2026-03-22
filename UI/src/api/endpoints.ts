import { apiClient } from "./client";

export type EnemySummary = {
  id: number;
  name: string;
  level: number;
  experienceReward: number;
};

export type CharacterSummary = {
  id: number;
  name: string;
  class: string;
  level: number;
};

export type CharacterSkill = {
  id: number;
  name: string;
  element?: string;
  requiredLevel: number;
  manaCost: number;
  staminaCost: number;
};

export type CharacterDetail = {
  id: number;
  level: number;
  skills: CharacterSkill[];
};

export type PlayerSummary = {
  id: number;
  username: string;
  characters: CharacterSummary[];
};

export type StartFightRequest = {
  playerId: number;
  characterId: number;
  enemyId: number;
};

export type FightTurnRequest = {
  playerId: number;
  skillId: number;
};

export type FightMove = {
  turn: number;
  isPlayer: boolean;
  skillId: number;
  damage: number;
  description: string;
  timestamp: string;
};

export type FightSummary = {
  id: string;
  characterName: string;
  enemyName: string;
  currentTurn: number;
  characterCurrentHp: number;
  characterMaxHp: number;
  enemyCurrentHp: number;
  enemyMaxHp: number;
  isVictory: boolean;
  status: string;
  moves: FightMove[];
};

export async function fetchEnemies(): Promise<EnemySummary[]> {
  const response = await apiClient.get<EnemySummary[]>("/api/enemies");
  return response.data;
}

export async function fetchPlayers(): Promise<PlayerSummary[]> {
  const response = await apiClient.get<PlayerSummary[]>("/api/players");
  return response.data;
}

export async function fetchCharacterDetail(playerId: number, characterId: number): Promise<CharacterDetail> {
  const response = await apiClient.get<CharacterDetail>(`/api/players/${playerId}/characters/${characterId}`);
  return response.data;
}

export async function startFight(payload: StartFightRequest): Promise<FightSummary> {
  const response = await apiClient.post<FightSummary>("/api/fights/start", payload);
  return response.data;
}

export async function getFightSession(sessionId: string): Promise<FightSummary> {
  const response = await apiClient.get<FightSummary>(`/api/fights/${sessionId}`);
  return response.data;
}

export async function takeFightTurn(sessionId: string, payload: FightTurnRequest): Promise<FightSummary> {
  const response = await apiClient.post<FightSummary>(`/api/fights/${sessionId}/turn`, payload);
  return response.data;
}
