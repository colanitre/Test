import axios from "axios";

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000",
  timeout: 10000
});

export type ApiEnvelope<T> = {
  data: T | null;
  meta: {
    serverTime: string;
    traceId: string;
    pagination?: {
      page: number;
      pageSize: number;
      totalItems: number;
      totalPages: number;
    };
  };
  error?: {
    code: string;
    message: string;
    details?: unknown;
  } | null;
};

export type EnemySummary = {
  id: number;
  name: string;
  level: number;
  experienceReward: number;
  type?: string;
  enemyClass?: {
    id: number;
    name: string;
    type?: string;
    physicalResistance?: number;
    fireResistance?: number;
    iceResistance?: number;
    lightningResistance?: number;
    poisonResistance?: number;
    holyResistance?: number;
    shadowResistance?: number;
    arcaneResistance?: number;
  };
};

export type CharacterSummary = {
  id: number;
  name: string;
  class: string;
  level: number;
  experience: number;
};

export type CharacterSkill = {
  id: number;
  name: string;
  description?: string;
  type?: number | string;
  element?: number | string;
  cooldown?: number;
  requiredLevel: number;
  manaCost: number;
  staminaCost: number;
};

export type CharacterDetail = {
  id: number;
  level: number;
  experience: number;
  class?: {
    id: number;
    name: string;
    physicalResistance?: number;
    fireResistance?: number;
    iceResistance?: number;
    lightningResistance?: number;
    poisonResistance?: number;
    holyResistance?: number;
    shadowResistance?: number;
    arcaneResistance?: number;
  };
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
  characterId: number;
  enemyId: number;
  characterName: string;
  enemyName: string;
  currentTurn: number;
  characterCurrentHp: number;
  characterMaxHp: number;
  characterCurrentMana: number;
  characterMaxMana: number;
  characterCurrentStamina: number;
  characterMaxStamina: number;
  enemyCurrentHp: number;
  enemyMaxHp: number;
  enemyCurrentMana: number;
  enemyMaxMana: number;
  enemyCurrentStamina: number;
  enemyMaxStamina: number;
  isVictory: boolean;
  status: string;
  moves: FightMove[];
  characterCooldownJson: string;
  characterActivePassiveSkills: number[];
};

export type PassiveSkillRequest = {
  skillId: number;
};

export type PassiveInteraction = {
  skillId: number;
  name: string;
  active: boolean;
  willRemainActive: boolean;
  canMaintain: boolean;
  reason?: string;
  manaCost: number;
  staminaCost: number;
};

export type FightActionPreview = {
  canUse: boolean;
  skillId: number;
  skillName: string;
  remainingCooldown: number;
  hasEnoughMana: boolean;
  hasEnoughStamina: boolean;
  manaAfterAction: number;
  staminaAfterAction: number;
  passiveInteractions: PassiveInteraction[];
  reasons: string[];
};

type FightActionPreviewRequest = {
  playerId: number;
  skillId: number;
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
  const response = await apiClient.post<ApiEnvelope<FightSummary>>("/api/v1/fights/start", payload);
  return unwrapEnvelope(response.data);
}

export async function getFightSession(sessionId: string): Promise<FightSummary> {
  const response = await apiClient.get<ApiEnvelope<FightSummary>>(`/api/v1/fights/${sessionId}`);
  return unwrapEnvelope(response.data);
}

export async function takeFightTurn(sessionId: string, payload: FightTurnRequest): Promise<FightSummary> {
  const response = await apiClient.post<ApiEnvelope<FightSummary>>(`/api/v1/fights/${sessionId}/turn`, payload);
  return unwrapEnvelope(response.data);
}

export async function activatePassiveSkill(sessionId: string, payload: PassiveSkillRequest): Promise<FightSummary> {
  const response = await apiClient.post<ApiEnvelope<FightSummary>>(`/api/v1/fights/${sessionId}/passive-skill/activate`, payload);
  return unwrapEnvelope(response.data);
}

export async function deactivatePassiveSkill(sessionId: string, payload: PassiveSkillRequest): Promise<FightSummary> {
  const response = await apiClient.post<ApiEnvelope<FightSummary>>(`/api/v1/fights/${sessionId}/passive-skill/deactivate`, payload);
  return unwrapEnvelope(response.data);
}

export async function previewFightAction(
  sessionId: string,
  payload: FightActionPreviewRequest
): Promise<FightActionPreview> {
  const response = await apiClient.post<ApiEnvelope<FightActionPreview>>(
    `/api/v1/fights/${sessionId}/actions/preview`,
    payload
  );
  return unwrapEnvelope(response.data);
}
