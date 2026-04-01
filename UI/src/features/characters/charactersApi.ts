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

export type ClassUpgradeSkill = {
  id: number;
  name: string;
  type?: number | string;
  requiredLevel: number;
  manaCost: number;
  staminaCost: number;
  cooldown: number;
};

export type ClassUpgradeOption = {
  id: number;
  name: string;
  archetype: string;
  tier: number;
  requiredLevel: number;
  isAdvanced: boolean;
  branch: number;
  baseStrength: number;
  baseAgility: number;
  baseIntelligence: number;
  baseWisdom: number;
  baseCharisma: number;
  baseEndurance: number;
  baseLuck: number;
  skills: ClassUpgradeSkill[];
};

export type CharacterProgressionDetail = {
  id: number;
  name: string;
  level: number;
  experience: number;
  class: {
    id: number;
    name: string;
    archetype: string;
    tier: number;
    baseStrength: number;
    baseAgility: number;
    baseIntelligence: number;
    baseWisdom: number;
    baseCharisma: number;
    baseEndurance: number;
    baseLuck: number;
  } | null;
  skills: Array<{
    id: number;
    name: string;
    requiredLevel: number;
    type?: number | string;
  }>;
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

export async function fetchClassUpgrades(playerId: number, characterId: number): Promise<ClassUpgradeOption[]> {
  const res = await apiClient.get<ClassUpgradeOption[]>(`/api/players/${playerId}/characters/${characterId}/class-upgrades`);
  return res.data;
}

export async function fetchCharacterProgressionDetail(playerId: number, characterId: number): Promise<CharacterProgressionDetail> {
  const res = await apiClient.get<CharacterProgressionDetail>(`/api/players/${playerId}/characters/${characterId}`);
  return res.data;
}

export async function upgradeCharacterClass(playerId: number, characterId: number, className: string): Promise<CharacterRow> {
  const res = await apiClient.post<CharacterRow>(`/api/players/${playerId}/characters/${characterId}/class-upgrade`, {
    className
  });
  return res.data;
}
