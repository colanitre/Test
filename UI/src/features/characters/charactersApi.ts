import axios from "axios";

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000",
  timeout: 10000
});

export const AVAILABLE_CLASSES = ["Warrior", "Mage", "Archer", "Rogue"] as const;
export type ClassName = (typeof AVAILABLE_CLASSES)[number];

export type CharacterRow = {
  id: number;
  name: string;
  description?: string;
  level: number;
  experience: number;
  playerId: number;
  class: { name: string; tier: number; archetype: string } | null;
};

export type PlayerRow = {
  id: number;
  username: string;
};

export async function fetchPlayersBasic(): Promise<PlayerRow[]> {
  const res = await apiClient.get<{ id: number; username: string }[]>("/api/players");
  return res.data.map((p) => ({ id: p.id, username: p.username }));
}

export async function fetchCharacters(playerId: number): Promise<CharacterRow[]> {
  const res = await apiClient.get<CharacterRow[]>(`/api/players/${playerId}/characters`);
  return res.data;
}

export async function createCharacter(
  playerId: number,
  payload: { name: string; class: string; description?: string }
): Promise<CharacterRow> {
  const res = await apiClient.post<CharacterRow>(`/api/players/${playerId}/characters`, payload);
  return res.data;
}

export async function renameCharacter(
  playerId: number,
  characterId: number,
  payload: { name: string; class: string; description?: string }
): Promise<void> {
  await apiClient.put(`/api/players/${playerId}/characters/${characterId}`, payload);
}

export async function deleteCharacter(playerId: number, characterId: number): Promise<void> {
  await apiClient.delete(`/api/players/${playerId}/characters/${characterId}`);
}
