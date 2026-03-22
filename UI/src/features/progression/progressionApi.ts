import axios from "axios";
import { fetchCharacters, fetchPlayersBasic, type CharacterRow, type PlayerRow } from "../characters/charactersApi";

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000",
  timeout: 10000,
});

type ApiEnvelope<T> = {
  data: T | null;
  meta: {
    serverTime: string;
    traceId: string;
  };
  error?: {
    code: string;
    message: string;
    details?: unknown;
  } | null;
};

function unwrapEnvelope<T>(envelope: ApiEnvelope<T>): T {
  if (envelope.error) {
    throw new Error(envelope.error.message);
  }

  if (envelope.data == null) {
    throw new Error("API returned no data.");
  }

  return envelope.data;
}

export type ProgressionContext = {
  players: PlayerRow[];
  characters: CharacterRow[];
};

export type LoadoutEntry = {
  id: string;
  name: string;
  activeSkillOrder: number[];
  passiveSkillIds: number[];
  updatedAt: string;
};

export type CharacterDetail = {
  id: number;
  class: { name: string } | null;
  skills: {
    id: number;
    name: string;
    type: string | number;
  }[];
};

export type CharacterEquipment = {
  characterId: number;
  weaponAffix: string | null;
  armorAffix: string | null;
  trinketAffix: string | null;
  elementalResistMultiplier: number;
  cooldownModifier: number;
  updatedAt: string;
};

export type TalentNode = {
  id: string;
  name: string;
  effect: string;
  category: string;
  cost: number;
  unlocked: boolean;
};

export type ChallengesPayload = {
  daily: { id: string; title: string; constraint: string; reward: number }[];
  weekly: { id: string; title: string; constraint: string; reward: number }[];
  generatedAt: string;
};

export type SeasonalLadderPayload = {
  season: string;
  metrics: string[];
  leaderboard: {
    rank: number;
    player: string;
    fastest_clear: number;
    highest_wave: number;
    best_win_streak: number;
  }[];
};

export async function fetchProgressionContext(playerId?: number): Promise<ProgressionContext> {
  const players = await fetchPlayersBasic();
  const activePlayerId = playerId ?? players[0]?.id;

  if (!activePlayerId) {
    return { players, characters: [] };
  }

  const characters = await fetchCharacters(activePlayerId);
  return { players, characters };
}

export async function fetchCharacterDetail(playerId: number, characterId: number): Promise<CharacterDetail> {
  const response = await apiClient.get<CharacterDetail>(`/api/players/${playerId}/characters/${characterId}`);
  return response.data;
}

export async function fetchLoadouts(playerId: number, characterId: number): Promise<LoadoutEntry[]> {
  const response = await apiClient.get<ApiEnvelope<LoadoutEntry[]>>(
    `/api/v1/players/${playerId}/characters/${characterId}/loadouts`
  );
  return unwrapEnvelope(response.data);
}

export async function upsertLoadout(
  playerId: number,
  characterId: number,
  loadoutId: string,
  payload: { name: string; activeSkillOrder: number[]; passiveSkillIds: number[] }
): Promise<LoadoutEntry> {
  const response = await apiClient.put<ApiEnvelope<LoadoutEntry>>(
    `/api/v1/players/${playerId}/characters/${characterId}/loadouts/${loadoutId}`,
    payload
  );
  return unwrapEnvelope(response.data);
}

export async function fetchEquipment(playerId: number, characterId: number): Promise<CharacterEquipment> {
  const response = await apiClient.get<ApiEnvelope<CharacterEquipment>>(
    `/api/v1/progression/players/${playerId}/characters/${characterId}/equipment`
  );
  return unwrapEnvelope(response.data);
}

export async function upsertEquipment(
  playerId: number,
  characterId: number,
  payload: Omit<CharacterEquipment, "characterId" | "updatedAt">
): Promise<CharacterEquipment> {
  const response = await apiClient.put<ApiEnvelope<CharacterEquipment>>(
    `/api/v1/progression/players/${playerId}/characters/${characterId}/equipment`,
    payload
  );
  return unwrapEnvelope(response.data);
}

export async function fetchTalentTree(className: string): Promise<TalentNode[]> {
  const response = await apiClient.get<ApiEnvelope<TalentNode[]>>(`/api/v1/progression/talents/${encodeURIComponent(className)}`);
  return unwrapEnvelope(response.data);
}

export async function fetchChallenges(): Promise<ChallengesPayload> {
  const response = await apiClient.get<ApiEnvelope<ChallengesPayload>>("/api/v1/progression/challenges");
  return unwrapEnvelope(response.data);
}

export async function fetchSeasonalLadder(): Promise<SeasonalLadderPayload> {
  const response = await apiClient.get<ApiEnvelope<SeasonalLadderPayload>>("/api/v1/progression/ladders/seasonal");
  return unwrapEnvelope(response.data);
}
