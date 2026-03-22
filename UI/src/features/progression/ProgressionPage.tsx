import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Panel } from "../../components/ui/Panel";
import {
  fetchChallenges,
  fetchCharacterDetail,
  fetchEquipment,
  fetchLoadouts,
  fetchProgressionContext,
  fetchSeasonalLadder,
  fetchTalentTree,
  upsertEquipment,
  upsertLoadout,
  type ChallengesPayload,
  type CharacterEquipment,
  type CharacterDetail,
  type LoadoutEntry,
  type SeasonalLadderPayload,
  type TalentNode,
} from "./progressionApi";

function createGuid(): string {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }

  return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (c) => {
    const r = Math.floor(Math.random() * 16);
    const v = c === "x" ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

export function ProgressionPage() {
  const [playerId, setPlayerId] = useState<number | null>(null);
  const [players, setPlayers] = useState<{ id: number; username: string }[]>([]);
  const [characters, setCharacters] = useState<{ id: number; name: string; class: { name: string } | null }[]>([]);
  const [characterId, setCharacterId] = useState<number | null>(null);

  const [characterDetail, setCharacterDetail] = useState<CharacterDetail | null>(null);
  const [loadouts, setLoadouts] = useState<LoadoutEntry[]>([]);
  const [equipment, setEquipment] = useState<CharacterEquipment | null>(null);
  const [talents, setTalents] = useState<TalentNode[]>([]);
  const [challenges, setChallenges] = useState<ChallengesPayload | null>(null);
  const [ladder, setLadder] = useState<SeasonalLadderPayload | null>(null);

  const [loadoutName, setLoadoutName] = useState("Balanced Rotation");
  const [savingLoadout, setSavingLoadout] = useState(false);
  const [savingEquipment, setSavingEquipment] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    setLoading(true);
    setError(null);

    fetchProgressionContext()
      .then(({ players: rows, characters: charRows }) => {
        setPlayers(rows);
        setCharacters(charRows.map((c) => ({ id: c.id, name: c.name, class: c.class ? { name: c.class.name } : null })));
        if (rows.length > 0) {
          setPlayerId(rows[0].id);
        }
        if (charRows.length > 0) {
          setCharacterId(charRows[0].id);
        }
      })
      .catch(() => setError("Could not load progression context."))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (!playerId) {
      setCharacters([]);
      setCharacterId(null);
      return;
    }

    fetchProgressionContext(playerId)
      .then(({ characters: charRows }) => {
        const mapped = charRows.map((c) => ({ id: c.id, name: c.name, class: c.class ? { name: c.class.name } : null }));
        setCharacters(mapped);
        if (mapped.length === 0) {
          setCharacterId(null);
          return;
        }

        if (!mapped.some((c) => c.id === characterId)) {
          setCharacterId(mapped[0].id);
        }
      })
      .catch(() => setError("Could not load player characters."));
  }, [playerId]);

  useEffect(() => {
    if (!playerId || !characterId) {
      setCharacterDetail(null);
      setLoadouts([]);
      setEquipment(null);
      setTalents([]);
      return;
    }

    setError(null);
    Promise.all([
      fetchCharacterDetail(playerId, characterId),
      fetchLoadouts(playerId, characterId),
      fetchEquipment(playerId, characterId),
      fetchChallenges(),
      fetchSeasonalLadder(),
    ])
      .then(([detail, loadoutRows, equipmentRow, challengesRow, ladderRow]) => {
        setCharacterDetail(detail);
        setLoadouts(loadoutRows);
        setEquipment(equipmentRow);
        setChallenges(challengesRow);
        setLadder(ladderRow);
        const className = detail.class?.name ?? "Warrior";
        return fetchTalentTree(className).then(setTalents);
      })
      .catch(() => setError("Could not load progression data for this character."));
  }, [playerId, characterId]);

  const activeSkills = useMemo(
    () => characterDetail?.skills.filter((s) => String(s.type).toLowerCase() !== "passive") ?? [],
    [characterDetail]
  );
  const passiveSkills = useMemo(
    () => characterDetail?.skills.filter((s) => String(s.type).toLowerCase() === "passive") ?? [],
    [characterDetail]
  );

  const [activeCsv, setActiveCsv] = useState("");
  const [passiveCsv, setPassiveCsv] = useState("");

  useEffect(() => {
    if (loadouts.length > 0) {
      setLoadoutName(loadouts[0].name);
      setActiveCsv(loadouts[0].activeSkillOrder.join(","));
      setPassiveCsv(loadouts[0].passiveSkillIds.join(","));
      return;
    }

    setActiveCsv(activeSkills.slice(0, 4).map((s) => s.id).join(","));
    setPassiveCsv(passiveSkills.slice(0, 2).map((s) => s.id).join(","));
  }, [loadouts, activeSkills, passiveSkills]);

  const parseIds = (csv: string): number[] =>
    csv
      .split(",")
      .map((token) => Number(token.trim()))
      .filter((n) => Number.isInteger(n) && n > 0);

  const flash = (message: string) => {
    setSuccess(message);
    window.setTimeout(() => setSuccess(null), 2500);
  };

  const handleSaveLoadout = async () => {
    if (!playerId || !characterId || !characterDetail) return;

    setSavingLoadout(true);
    setError(null);

    try {
      const activeIds = parseIds(activeCsv);
      const passiveIds = parseIds(passiveCsv);
      const targetId = loadouts[0]?.id ?? createGuid();

      const saved = await upsertLoadout(playerId, characterId, targetId, {
        name: loadoutName.trim() || "Custom Loadout",
        activeSkillOrder: activeIds,
        passiveSkillIds: passiveIds,
      });

      setLoadouts([saved, ...loadouts.filter((l) => l.id !== saved.id)]);
      flash("Loadout saved.");
    } catch {
      setError("Failed to save loadout. Verify skill IDs belong to this character.");
    } finally {
      setSavingLoadout(false);
    }
  };

  const handleSaveEquipment = async () => {
    if (!playerId || !characterId || !equipment) return;

    setSavingEquipment(true);
    setError(null);

    try {
      const saved = await upsertEquipment(playerId, characterId, {
        weaponAffix: equipment.weaponAffix,
        armorAffix: equipment.armorAffix,
        trinketAffix: equipment.trinketAffix,
        elementalResistMultiplier: equipment.elementalResistMultiplier,
        cooldownModifier: equipment.cooldownModifier,
      });
      setEquipment(saved);
      flash("Equipment profile saved.");
    } catch {
      setError("Failed to save equipment modifiers.");
    } finally {
      setSavingEquipment(false);
    }
  };

  return (
    <main className="page">
      <header>
        <h1>Progression Console</h1>
        <p>Manage loadouts and equipment while tracking talents, challenges, and ladder standings.</p>
      </header>

      <div className="progression-layout">
        <Panel title="Context">
          {loading && <p style={{ opacity: 0.7 }}>Loading progression context...</p>}

          <label htmlFor="prog-player">Player</label>
          <select
            id="prog-player"
            value={playerId ?? ""}
            onChange={(e) => setPlayerId(e.target.value ? Number(e.target.value) : null)}
          >
            <option value="">Select player</option>
            {players.map((player) => (
              <option key={player.id} value={player.id}>
                {player.username}
              </option>
            ))}
          </select>

          <label htmlFor="prog-character">Character</label>
          <select
            id="prog-character"
            value={characterId ?? ""}
            onChange={(e) => setCharacterId(e.target.value ? Number(e.target.value) : null)}
          >
            <option value="">Select character</option>
            {characters.map((character) => (
              <option key={character.id} value={character.id}>
                {character.name}
              </option>
            ))}
          </select>

          {characterDetail?.class && (
            <p className="selection-summary">
              Class: <strong>{characterDetail.class.name}</strong> | Skills: {characterDetail.skills.length}
            </p>
          )}

          {success && <p className="success-msg">{success}</p>}
          {error && <p className="error">{error}</p>}

          <Link className="button button-muted" to="/" style={{ marginTop: "1rem" }}>
            Back to Home
          </Link>
        </Panel>

        <Panel title="Loadouts">
          <label htmlFor="loadout-name">Name</label>
          <input
            id="loadout-name"
            className="text-input"
            value={loadoutName}
            onChange={(e) => setLoadoutName(e.target.value)}
          />

          <label htmlFor="loadout-active">Active Skill IDs (comma-separated)</label>
          <input
            id="loadout-active"
            className="text-input"
            value={activeCsv}
            onChange={(e) => setActiveCsv(e.target.value)}
            placeholder="1,2,3"
          />

          <label htmlFor="loadout-passive">Passive Skill IDs (comma-separated)</label>
          <input
            id="loadout-passive"
            className="text-input"
            value={passiveCsv}
            onChange={(e) => setPassiveCsv(e.target.value)}
            placeholder="10,11"
          />

          <p className="progression-help">Available active: {activeSkills.map((s) => `${s.id}:${s.name}`).join(" | ") || "none"}</p>
          <p className="progression-help">Available passive: {passiveSkills.map((s) => `${s.id}:${s.name}`).join(" | ") || "none"}</p>

          <button className="button" type="button" disabled={!characterId || savingLoadout} onClick={() => void handleSaveLoadout()}>
            {savingLoadout ? "Saving..." : "Save Loadout"}
          </button>

          <div className="progression-list">
            {loadouts.map((loadout) => (
              <article key={loadout.id} className="progression-item">
                <strong>{loadout.name}</strong>
                <span>Active: {loadout.activeSkillOrder.join(", ") || "none"}</span>
                <span>Passive: {loadout.passiveSkillIds.join(", ") || "none"}</span>
              </article>
            ))}
          </div>
        </Panel>

        <Panel title="Equipment">
          <label htmlFor="equip-weapon">Weapon Affix</label>
          <input
            id="equip-weapon"
            className="text-input"
            value={equipment?.weaponAffix ?? ""}
            onChange={(e) => setEquipment((prev) => prev ? { ...prev, weaponAffix: e.target.value || null } : prev)}
          />

          <label htmlFor="equip-armor">Armor Affix</label>
          <input
            id="equip-armor"
            className="text-input"
            value={equipment?.armorAffix ?? ""}
            onChange={(e) => setEquipment((prev) => prev ? { ...prev, armorAffix: e.target.value || null } : prev)}
          />

          <label htmlFor="equip-trinket">Trinket Affix</label>
          <input
            id="equip-trinket"
            className="text-input"
            value={equipment?.trinketAffix ?? ""}
            onChange={(e) => setEquipment((prev) => prev ? { ...prev, trinketAffix: e.target.value || null } : prev)}
          />

          <label htmlFor="equip-resist">Elemental Resist Multiplier</label>
          <input
            id="equip-resist"
            className="text-input"
            type="number"
            min={0.5}
            max={2.0}
            step={0.01}
            value={equipment?.elementalResistMultiplier ?? 1}
            onChange={(e) => setEquipment((prev) => prev ? { ...prev, elementalResistMultiplier: Number(e.target.value) } : prev)}
          />

          <label htmlFor="equip-cooldown">Cooldown Modifier</label>
          <input
            id="equip-cooldown"
            className="text-input"
            type="number"
            min={-5}
            max={5}
            value={equipment?.cooldownModifier ?? 0}
            onChange={(e) => setEquipment((prev) => prev ? { ...prev, cooldownModifier: Number(e.target.value) } : prev)}
          />

          <button className="button" type="button" disabled={!characterId || !equipment || savingEquipment} onClick={() => void handleSaveEquipment()}>
            {savingEquipment ? "Saving..." : "Save Equipment"}
          </button>
        </Panel>

        <Panel title="Talents">
          <div className="progression-list">
            {talents.map((talent) => (
              <article key={talent.id} className="progression-item">
                <strong>{talent.name}</strong>
                <span>{talent.effect}</span>
                <span>
                  {talent.category} | Cost {talent.cost} | {talent.unlocked ? "Unlocked" : "Locked"}
                </span>
              </article>
            ))}
          </div>
        </Panel>

        <Panel title="Challenges & Ladder">
          <h3 className="section-label">Daily</h3>
          <div className="progression-list">
            {challenges?.daily.map((challenge) => (
              <article key={challenge.id} className="progression-item">
                <strong>{challenge.title}</strong>
                <span>{challenge.constraint}</span>
                <span>{challenge.reward} XP</span>
              </article>
            ))}
          </div>

          <h3 className="section-label">Weekly</h3>
          <div className="progression-list">
            {challenges?.weekly.map((challenge) => (
              <article key={challenge.id} className="progression-item">
                <strong>{challenge.title}</strong>
                <span>{challenge.constraint}</span>
                <span>{challenge.reward} XP</span>
              </article>
            ))}
          </div>

          <h3 className="section-label">{ladder?.season ?? "Season"} Leaderboard</h3>
          <div className="progression-list">
            {ladder?.leaderboard.map((entry) => (
              <article key={`${entry.rank}-${entry.player}`} className="progression-item">
                <strong>
                  #{entry.rank} {entry.player}
                </strong>
                <span>Fastest clear: {entry.fastest_clear}</span>
                <span>Highest wave: {entry.highest_wave} | Win streak: {entry.best_win_streak}</span>
              </article>
            ))}
          </div>
        </Panel>
      </div>
    </main>
  );
}
