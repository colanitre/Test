import { lazy, Suspense, useEffect, useMemo, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { Panel } from "../../components/ui/Panel";

const BattleScene = lazy(() =>
  import("../../scenes/BattleScene").then((m) => ({ default: m.BattleScene }))
);
import {
  activatePassiveSkill,
  deactivatePassiveSkill,
  type FightActionPreview,
  type CharacterDetail,
  fetchCharacterDetail,
  fetchEnemies,
  getFightSession,
  fetchPlayers,
  previewFightAction,
  startFight,
  takeFightTurn,
  type CharacterSkill,
  type EnemySummary,
  type FightSummary,
  type PlayerSummary
} from "./battleApi";
import { useGameStore } from "../../store/gameStore";

export function BattlePage() {
  const [players, setPlayers] = useState<PlayerSummary[]>([]);
  const [enemies, setEnemies] = useState<EnemySummary[]>([]);
  const [selectedCharacterDetail, setSelectedCharacterDetail] = useState<CharacterDetail | null>(null);
  const [animatedExperience, setAnimatedExperience] = useState(0);
  const [characterSkills, setCharacterSkills] = useState<CharacterSkill[]>([]);
  const [selectedSkillId, setSelectedSkillId] = useState<number | null>(null);
  const [fightSummary, setFightSummary] = useState<FightSummary | null>(null);
  const [isStartingFight, setIsStartingFight] = useState(false);
  const [isTakingTurn, setIsTakingTurn] = useState(false);
  const [isTogglingPassive, setIsTogglingPassive] = useState<number | null>(null);
  const [autoBattleEnabled, setAutoBattleEnabled] = useState(false);
  const [autoStartNextFightEnabled, setAutoStartNextFightEnabled] = useState(false);
  const [battleSpeed, setBattleSpeed] = useState(1400);
  const [preferredPassiveSkillIds, setPreferredPassiveSkillIds] = useState<number[]>([]);
  const [selectedSkillPreview, setSelectedSkillPreview] = useState<FightActionPreview | null>(null);
  const [error, setError] = useState<string | null>(null);
  const turnInFlightRef = useRef(false);
  const startInFlightRef = useRef(false);
  const selectedPlayerId = useGameStore((state) => state.selectedPlayerId);
  const selectedCharacterId = useGameStore((state) => state.selectedCharacterId);
  const selectedEnemyId = useGameStore((state) => state.selectedEnemyId);
  const setSelectedPlayerId = useGameStore((state) => state.setSelectedPlayerId);
  const setSelectedCharacterId = useGameStore((state) => state.setSelectedCharacterId);
  const setSelectedEnemyId = useGameStore((state) => state.setSelectedEnemyId);

  useEffect(() => {
    Promise.all([fetchPlayers(), fetchEnemies()])
      .then(([playerItems, enemyItems]) => {
        const sortedPlayers = [...playerItems].sort((a, b) => a.username.localeCompare(b.username));
        const sortedEnemies = [...enemyItems].sort((a, b) => a.level - b.level);

        setPlayers(sortedPlayers);
        setEnemies(sortedEnemies);

        if (!selectedPlayerId && sortedPlayers.length > 0) {
          const firstPlayer = sortedPlayers[0];
          setSelectedPlayerId(firstPlayer.id);
          setSelectedCharacterId(firstPlayer.characters[0]?.id ?? null);
        }

        if (!selectedEnemyId && sortedEnemies.length > 0) {
          setSelectedEnemyId(sortedEnemies[0].id);
        }
      })
      .catch(() => setError("Unable to fetch players or enemies from API."));
  }, [selectedEnemyId, selectedPlayerId, setSelectedCharacterId, setSelectedEnemyId, setSelectedPlayerId]);

  const selectedPlayer = players.find((player) => player.id === selectedPlayerId) ?? null;
  const availableCharacters = selectedPlayer?.characters ?? [];
  const selectedCharacter = availableCharacters.find((character) => character.id === selectedCharacterId) ?? null;
  const selectedEnemy = enemies.find((enemy) => enemy.id === selectedEnemyId) ?? null;

  const latestMove = useMemo(() => {
    if (!fightSummary || fightSummary.moves.length === 0) {
      return null;
    }
    return fightSummary.moves[fightSummary.moves.length - 1];
  }, [fightSummary]);

  const activeSkills = useMemo(
    () => characterSkills.filter((skill) => !isPassiveSkill(skill)),
    [characterSkills]
  );

  const passiveSkills = useMemo(
    () => characterSkills.filter((skill) => isPassiveSkill(skill)),
    [characterSkills]
  );

  const passivePreferenceStorageKey = useMemo(() => {
    if (!selectedPlayerId || !selectedCharacterId) {
      return null;
    }
    return `battle-passive-preferences:${selectedPlayerId}:${selectedCharacterId}`;
  }, [selectedCharacterId, selectedPlayerId]);

  const characterCooldowns = useMemo<Record<string, number>>(() => {
    if (!fightSummary?.characterCooldownJson) {
      return {};
    }

    try {
      return JSON.parse(fightSummary.characterCooldownJson) as Record<string, number>;
    } catch {
      return {};
    }
  }, [fightSummary?.characterCooldownJson]);

  const latestMoveElement = useMemo(() => {
    if (!latestMove) {
      return "physical";
    }

    if (latestMove.isPlayer) {
      const skill = characterSkills.find((item) => item.id === latestMove.skillId);
      if (skill?.element) {
        return toElementName(skill.element);
      }
    }

    const text = latestMove.description.toLowerCase();
    if (text.includes("fire") || text.includes("burn")) return "fire";
    if (text.includes("ice") || text.includes("frost")) return "ice";
    if (text.includes("lightning") || text.includes("shock")) return "lightning";
    if (text.includes("poison") || text.includes("venom")) return "poison";
    if (text.includes("holy") || text.includes("radiant")) return "holy";
    if (text.includes("shadow") || text.includes("dark")) return "shadow";
    if (text.includes("arcane") || text.includes("magic")) return "arcane";
    return "physical";
  }, [characterSkills, latestMove]);

  const characterHpRatio = fightSummary
    ? Math.max(0, Math.min(1, fightSummary.characterCurrentHp / Math.max(1, fightSummary.characterMaxHp)))
    : 1;
  const enemyHpRatio = fightSummary
    ? Math.max(0, Math.min(1, fightSummary.enemyCurrentHp / Math.max(1, fightSummary.enemyMaxHp)))
    : 1;
  const characterManaRatio = fightSummary
    ? Math.max(0, Math.min(1, fightSummary.characterCurrentMana / Math.max(1, fightSummary.characterMaxMana)))
    : 1;
  const enemyManaRatio = fightSummary
    ? Math.max(0, Math.min(1, fightSummary.enemyCurrentMana / Math.max(1, fightSummary.enemyMaxMana)))
    : 1;
  const characterStaminaRatio = fightSummary
    ? Math.max(0, Math.min(1, fightSummary.characterCurrentStamina / Math.max(1, fightSummary.characterMaxStamina)))
    : 1;
  const enemyStaminaRatio = fightSummary
    ? Math.max(0, Math.min(1, fightSummary.enemyCurrentStamina / Math.max(1, fightSummary.enemyMaxStamina)))
    : 1;

  const selectedEnemyType = selectedEnemy?.type ?? selectedEnemy?.enemyClass?.type ?? selectedEnemy?.enemyClass?.name ?? selectedEnemy?.name ?? "unknown";
  const playerResistanceRows = getResistanceRows(selectedCharacterDetail?.class);
  const enemyResistanceRows = getResistanceRows(selectedEnemy?.enemyClass);
  const latestMoveDamage = latestMove?.damage ?? 0;
  const displayedExperience = selectedCharacterDetail?.experience ?? selectedCharacter?.experience ?? 0;
  const displayedLevel = selectedCharacterDetail?.level ?? selectedCharacter?.level ?? 1;
  const experienceThreshold = Math.max(1, Math.round(50 * Math.pow(1.08, displayedLevel)));
  const experienceRatio = Math.max(0, Math.min(1, animatedExperience / experienceThreshold));
  const fightEnded = Boolean(
    fightSummary && (fightSummary.isVictory || fightSummary.characterCurrentHp <= 0 || fightSummary.enemyCurrentHp <= 0)
  );
  const resultTitle = fightEnded
    ? fightSummary?.isVictory
      ? "Victory"
      : "Defeat"
    : null;
  const resultSubtitle = fightEnded
    ? autoStartNextFightEnabled
      ? "Next fight will start automatically."
      : fightSummary?.isVictory
        ? "Your character won the duel."
        : "Your character was defeated."
    : null;

  useEffect(() => {
    if (!selectedPlayer) {
      return;
    }

    const characterIsValid = selectedPlayer.characters.some((character) => character.id === selectedCharacterId);
    if (!characterIsValid) {
      setSelectedCharacterId(selectedPlayer.characters[0]?.id ?? null);
    }
  }, [selectedCharacterId, selectedPlayer, setSelectedCharacterId]);

  useEffect(() => {
    if (!selectedPlayerId || !selectedCharacterId) {
      setSelectedCharacterDetail(null);
      setAnimatedExperience(0);
      setCharacterSkills([]);
      setSelectedSkillId(null);
      return;
    }

    fetchCharacterDetail(selectedPlayerId, selectedCharacterId)
      .then((character) => {
        const usableSkills = character.skills.filter((skill) => skill.requiredLevel <= character.level);
        const availablePassiveIds = usableSkills.filter((skill) => isPassiveSkill(skill)).map((skill) => skill.id);

        setSelectedCharacterDetail(character);
        setAnimatedExperience(character.experience);
        setCharacterSkills(usableSkills);
        setSelectedSkillId(usableSkills.find((skill) => !isPassiveSkill(skill))?.id ?? null);

        const storageKey = `battle-passive-preferences:${selectedPlayerId}:${selectedCharacterId}`;
        const stored = window.localStorage.getItem(storageKey);
        if (stored) {
          try {
            const parsed = JSON.parse(stored) as number[];
            setPreferredPassiveSkillIds(
              parsed.filter((id) => availablePassiveIds.includes(id))
            );
          } catch {
            setPreferredPassiveSkillIds([]);
          }
        } else {
          setPreferredPassiveSkillIds([]);
        }
      })
      .catch(() => {
        setSelectedCharacterDetail(null);
        setCharacterSkills([]);
        setSelectedSkillId(null);
        setPreferredPassiveSkillIds([]);
        setError("Unable to load character skills.");
      });
  }, [selectedCharacterId, selectedPlayerId]);

  useEffect(() => {
    if (!passivePreferenceStorageKey) {
      return;
    }

    window.localStorage.setItem(
      passivePreferenceStorageKey,
      JSON.stringify(preferredPassiveSkillIds)
    );
  }, [passivePreferenceStorageKey, preferredPassiveSkillIds]);

  useEffect(() => {
    if (!fightSummary?.id) {
      setSelectedSkillPreview(null);
      return;
    }

    const intervalId = window.setInterval(() => {
      getFightSession(fightSummary.id)
        .then((updated) => setFightSummary(updated))
        .catch(() => {
          // keep existing state; avoid replacing user-visible errors every 3s
        });
    }, 3000);

    return () => window.clearInterval(intervalId);
  }, [fightSummary?.id]);

  useEffect(() => {
    if (!fightSummary?.id || !selectedPlayerId || !selectedSkillId) {
      setSelectedSkillPreview(null);
      return;
    }

    const skill = characterSkills.find((item) => item.id === selectedSkillId);
    if (!skill || isPassiveSkill(skill)) {
      setSelectedSkillPreview(null);
      return;
    }

    previewFightAction(fightSummary.id, {
      playerId: selectedPlayerId,
      skillId: selectedSkillId
    })
      .then((preview) => {
        setSelectedSkillPreview(preview);
      })
      .catch(() => {
        setSelectedSkillPreview(null);
      });
  }, [characterSkills, fightSummary?.id, selectedPlayerId, selectedSkillId]);

  useEffect(() => {
    if (!selectedCharacterDetail) {
      return;
    }

    if (animatedExperience === selectedCharacterDetail.experience) {
      return;
    }

    const timerId = window.setInterval(() => {
      setAnimatedExperience((current) => {
        if (current === selectedCharacterDetail.experience) {
          window.clearInterval(timerId);
          return current;
        }

        const delta = selectedCharacterDetail.experience - current;
        const step = Math.max(1, Math.ceil(Math.abs(delta) / 12));
        const next = current + Math.sign(delta) * step;

        if ((delta > 0 && next >= selectedCharacterDetail.experience) || (delta < 0 && next <= selectedCharacterDetail.experience)) {
          window.clearInterval(timerId);
          return selectedCharacterDetail.experience;
        }

        return next;
      });
    }, 45);

    return () => window.clearInterval(timerId);
  }, [animatedExperience, selectedCharacterDetail]);

  useEffect(() => {
    if (!fightEnded || !selectedPlayerId || !selectedCharacterId) {
      return;
    }

    fetchCharacterDetail(selectedPlayerId, selectedCharacterId)
      .then((character) => {
        setSelectedCharacterDetail(character);
      })
      .catch(() => {
        // Keep the current detail state if refresh fails after combat.
      });
  }, [fightEnded, selectedCharacterId, selectedPlayerId]);

  const startFightWithCurrentSelection = async () => {
    if (!selectedPlayer || !selectedCharacter || !selectedEnemy) {
      setError("Choose a player, character, and enemy before starting a fight.");
      return false;
    }

    if (startInFlightRef.current) {
      return false;
    }

    startInFlightRef.current = true;

    setIsStartingFight(true);
    setError(null);

    try {
      let summary = await startFight({
        playerId: selectedPlayer.id,
        characterId: selectedCharacter.id,
        enemyId: selectedEnemy.id
      });

      for (const passiveSkillId of preferredPassiveSkillIds) {
        if (!summary.characterActivePassiveSkills.includes(passiveSkillId)) {
          try {
            summary = await activatePassiveSkill(summary.id, { skillId: passiveSkillId });
          } catch {
            // Keep fight startup flowing even if one passive cannot be activated.
          }
        }
      }

      setFightSummary(summary);
      return true;
    } catch {
      setError("Failed to start fight. Make sure API is running.");
      return false;
    } finally {
      startInFlightRef.current = false;
      setIsStartingFight(false);
    }
  };

  const handleStartFight = async () => {
    const started = await startFightWithCurrentSelection();
    if (!started) {
      return;
    }

    setAutoBattleEnabled(false);
  };

  const executeTurn = async (preferSelectedSkill: boolean): Promise<boolean> => {
    if (!fightSummary?.id || !selectedPlayerId || characterSkills.length === 0) {
      return false;
    }

    if (turnInFlightRef.current) {
      return false;
    }

    const orderedSkills = [...characterSkills];
    if (preferSelectedSkill && selectedSkillId) {
      orderedSkills.sort((a, b) => {
        if (a.id === selectedSkillId) {
          return -1;
        }
        if (b.id === selectedSkillId) {
          return 1;
        }
        return 0;
      });
    }

    turnInFlightRef.current = true;
    setIsTakingTurn(true);

    for (const skill of orderedSkills.filter((candidate) => !isPassiveSkill(candidate))) {
      try {
        const preview = await previewFightAction(fightSummary.id, {
          playerId: selectedPlayerId,
          skillId: skill.id
        });

        if (!preview.canUse) {
          if (skill.id === selectedSkillId && preview.reasons.length > 0) {
            setError(preview.reasons.join(". "));
          }
          continue;
        }

        const updated = await takeFightTurn(fightSummary.id, {
          playerId: selectedPlayerId,
          skillId: skill.id
        });
        setSelectedSkillId(skill.id);
        setFightSummary(updated);
        setSelectedSkillPreview(null);
        setError(null);
        turnInFlightRef.current = false;
        setIsTakingTurn(false);
        return true;
      } catch {
        // Try next skill automatically if this one cannot be used (cooldown/resources).
      }
    }

    turnInFlightRef.current = false;
    setIsTakingTurn(false);
    return false;
  };

  const handleTakeTurn = async () => {
    const ok = await executeTurn(true);
    if (!ok) {
      if (selectedSkillPreview && selectedSkillPreview.reasons.length > 0) {
        setError(selectedSkillPreview.reasons.join(". "));
      } else {
        setError("No usable skills right now. Wait a turn or pick different skills.");
      }
    }
  };

  const handleTogglePassive = async (skillId: number, isActive: boolean) => {
    if (!fightSummary?.id) {
      setPreferredPassiveSkillIds((current) =>
        isActive ? current.filter((id) => id !== skillId) : Array.from(new Set([...current, skillId]))
      );
      return;
    }

    setIsTogglingPassive(skillId);
    setError(null);

    try {
      const updated = isActive
        ? await deactivatePassiveSkill(fightSummary.id, { skillId })
        : await activatePassiveSkill(fightSummary.id, { skillId });
      setFightSummary(updated);
      setPreferredPassiveSkillIds((current) =>
        isActive ? current.filter((id) => id !== skillId) : Array.from(new Set([...current, skillId]))
      );
    } catch {
      setError("Unable to update passive skill state.");
    } finally {
      setIsTogglingPassive(null);
    }
  };

  useEffect(() => {
    if (!autoBattleEnabled || !fightSummary || !fightSummary.id || !selectedPlayerId) {
      return;
    }

    const fightIsFinished =
      fightSummary.isVictory || fightSummary.characterCurrentHp <= 0 || fightSummary.enemyCurrentHp <= 0;

    if (fightIsFinished) {
      if (!autoStartNextFightEnabled) {
        setAutoBattleEnabled(false);
      }
      return;
    }

    const timerId = window.setInterval(async () => {
      const success = await executeTurn(true);
      if (!success) {
        setAutoBattleEnabled(false);
        setError("Auto battle paused because no usable skills were available.");
      }
    }, battleSpeed);

    return () => window.clearInterval(timerId);
  }, [autoBattleEnabled, autoStartNextFightEnabled, battleSpeed, fightSummary, selectedPlayerId, characterSkills, selectedSkillId]);

  useEffect(() => {
    if (!autoStartNextFightEnabled || !fightSummary || isStartingFight) {
      return;
    }

    const fightIsFinished =
      fightSummary.isVictory || fightSummary.characterCurrentHp <= 0 || fightSummary.enemyCurrentHp <= 0;

    if (!fightIsFinished) {
      return;
    }

    const timerId = window.setTimeout(async () => {
      const started = await startFightWithCurrentSelection();
      if (!started) {
        setAutoStartNextFightEnabled(false);
      }
    }, 1200);

    return () => window.clearTimeout(timerId);
  }, [autoStartNextFightEnabled, fightSummary, isStartingFight, selectedCharacter, selectedEnemy, selectedPlayer]);

  // Keyboard shortcuts
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      const tag = (event.target as HTMLElement).tagName;
      if (tag === "INPUT" || tag === "SELECT" || tag === "TEXTAREA") return;

      if (event.code === "Space") {
        event.preventDefault();
        if (fightSummary && !isTakingTurn) {
          void handleTakeTurn();
        }
        return;
      }

      if (event.code === "Escape") {
        setAutoBattleEnabled(false);
        return;
      }

      const digit = parseInt(event.key, 10);
      if (!isNaN(digit) && digit >= 1 && digit <= 9) {
        const skill = activeSkills[digit - 1];
        if (skill) setSelectedSkillId(skill.id);
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [fightSummary, isTakingTurn, activeSkills]);

  const getMoveTone = (description: string): "buff" | "miss" | "hit" => {
    const lower = description.toLowerCase();
    if (lower.includes("miss") || lower.includes("dodg")) {
      return "miss";
    }
    if (lower.includes("heal") || lower.includes("shield") || lower.includes("buff")) {
      return "buff";
    }
    return "hit";
  };

  return (
    <main className="page page-battle">
      <header>
        <h1>Battle Sandbox</h1>
        <p>Three.js scene + API-driven enemy selection.</p>
      </header>

      <div className="battle-layout">
        <Panel title="Battle Setup">
          <div className="battle-setup-scroll">
          <label htmlFor="player-select">Choose player</label>
          <select
            id="player-select"
            value={selectedPlayerId ?? ""}
            onChange={(event) => {
              const nextValue = event.target.value;
              const nextPlayerId = nextValue ? Number(nextValue) : null;
              const nextPlayer = players.find((player) => player.id === nextPlayerId) ?? null;

              setSelectedPlayerId(nextPlayerId);
              setSelectedCharacterId(nextPlayer?.characters[0]?.id ?? null);
              setFightSummary(null);
            }}
          >
            <option value="">Select a player</option>
            {players.map((player) => (
              <option key={player.id} value={player.id}>
                {player.username}
              </option>
            ))}
          </select>

          <label htmlFor="character-select">Choose character</label>
          <select
            id="character-select"
            value={selectedCharacterId ?? ""}
            onChange={(event) => {
              const nextValue = event.target.value;
              setSelectedCharacterId(nextValue ? Number(nextValue) : null);
              setFightSummary(null);
            }}
          >
            <option value="">Select a character</option>
            {availableCharacters.map((character) => (
              <option key={character.id} value={character.id}>
                {character.name} | {character.class} | Lv {character.level}
              </option>
            ))}
          </select>

          <label htmlFor="enemy-select">Choose enemy</label>
          <select
            id="enemy-select"
            value={selectedEnemyId ?? ""}
            onChange={(event) => {
              const nextValue = event.target.value;
              setSelectedEnemyId(nextValue ? Number(nextValue) : null);
              setFightSummary(null);
            }}
          >
            <option value="">Select an enemy</option>
            {enemies.map((enemy) => (
              <option key={enemy.id} value={enemy.id}>
                {enemy.name} | Lv {enemy.level} | XP {enemy.experienceReward}
              </option>
            ))}
          </select>
          {selectedEnemy && (() => {
            const diff = selectedEnemy.level - (selectedCharacter?.level ?? 1);
            const { label, cls } = diff <= -5
              ? { label: "Trivial", cls: "diff-trivial" }
              : diff <= -2
              ? { label: "Easy", cls: "diff-easy" }
              : diff <= 2
              ? { label: "Fair", cls: "diff-fair" }
              : diff <= 5
              ? { label: "Hard", cls: "diff-hard" }
              : { label: "Deadly", cls: "diff-deadly" };
            return <p className={`difficulty-badge ${cls}`}>Difficulty: {label} (Lv {selectedEnemy.level} vs Lv {selectedCharacter?.level ?? 1})</p>;
          })()}

          <div className="selection-summary">
            <p>Player: {selectedPlayer?.username ?? "None"}</p>
            <p>Character: {selectedCharacter ? `${selectedCharacter.name} (Lv ${selectedCharacter.level})` : "None"}</p>
            <p>Enemy: {selectedEnemy ? `${selectedEnemy.name} (Lv ${selectedEnemy.level})` : "None"}</p>
            <div className="progress-stack">
              <div>
                <p className="meter-label">Experience {displayedExperience}/{experienceThreshold}</p>
                <div className="meter-track">
                  <div className="meter-fill meter-fill-experience" style={{ width: `${experienceRatio * 100}%` }} />
                </div>
              </div>
            </div>
          </div>

          <button className="button" type="button" onClick={handleStartFight} disabled={isStartingFight}>
            {isStartingFight ? "Starting..." : "Start Fight"}
          </button>

          {activeSkills.length > 0 && (
            <div className="active-skills-panel">
              <p className="section-label">Active Skills</p>
              <div className="active-skill-list">
                {activeSkills.map((skill) => {
                  const cooldown = characterCooldowns[String(skill.id)] ?? 0;
                  const isSelected = selectedSkillId === skill.id;
                  return (
                    <button
                      key={skill.id}
                      className={`skill-card ${isSelected ? "skill-card-selected" : "skill-card-idle"}`}
                      type="button"
                      onClick={() => setSelectedSkillId(skill.id)}
                      disabled={!fightSummary && cooldown > 0}
                      title={skill.description ?? skill.name}
                    >
                      {cooldown > 0 && <span className="skill-card-badge">CD {cooldown}</span>}
                      <span className="skill-card-name">{skill.name}</span>
                      <span className="skill-card-meta">Mana {skill.manaCost} | Stamina {skill.staminaCost}</span>
                      <span className="skill-card-meta">Cooldown {cooldown > 0 ? cooldown : skill.cooldown ?? 0}</span>
                    </button>
                  );
                })}
              </div>
            </div>
          )}

          {passiveSkills.length > 0 && (
            <div className="passive-skills-panel">
              <p className="section-label">Passive Skills</p>
              <div className="passive-skill-list">
                {passiveSkills.map((skill) => {
                  const isActive = fightSummary
                    ? fightSummary.characterActivePassiveSkills?.includes(skill.id)
                    : preferredPassiveSkillIds.includes(skill.id);
                  return (
                    <button
                      key={skill.id}
                      className={`passive-chip ${isActive ? "passive-chip-active" : "passive-chip-idle"}`}
                      type="button"
                      onClick={() => void handleTogglePassive(skill.id, isActive)}
                      disabled={isTogglingPassive === skill.id}
                      title={skill.description ?? skill.name}
                    >
                      {skill.name}
                    </button>
                  );
                })}
              </div>
              <div className="passive-description-list">
                {passiveSkills.map((skill) => (
                  <p key={skill.id} className="passive-description-item">
                    <strong>{skill.name}:</strong> {skill.description ?? "Passive effect"}
                  </p>
                ))}
              </div>
            </div>
          )}

          {fightSummary && (
            <div className="fight-summary">
              <p>Session: {fightSummary.id.slice(0, 8)}...</p>
              <p>
                {fightSummary.characterName} vs {fightSummary.enemyName}
              </p>

              <p>Status: {fightSummary.status} | Turn: {fightSummary.currentTurn} | Victory: {fightSummary.isVictory ? "Yes" : "No"}</p>

              <div className="fight-log">
                {fightSummary.moves.length === 0 ? (
                  <p>No moves yet. Press Take Turn or Start Auto Battle.</p>
                ) : (
                  fightSummary.moves.slice(-6).map((move) => (
                    <div
                      key={`${move.turn}-${move.timestamp}`}
                      className={`fight-log-row tone-${getMoveTone(move.description)} ${move.isPlayer ? "actor-player" : "actor-enemy"}`}
                    >
                      <span className="turn-pill">T{move.turn}</span>
                      <span className="actor-pill">{move.isPlayer ? "Player" : "Enemy"}</span>
                      <span className="move-text">{move.description}</span>
                      <span className="damage-pill">{move.damage}</span>
                    </div>
                  ))
                )}
              </div>
            </div>
          )}

          {error && <p className="error">{error}</p>}
          {fightSummary && selectedSkillPreview && selectedSkillId && (
            <p className={`meter-label ${selectedSkillPreview.canUse ? "skill-preview-usable" : "skill-preview-blocked"}`}>
              Preview: {selectedSkillPreview.canUse
                ? `Ready. Mana after: ${selectedSkillPreview.manaAfterAction}, Stamina after: ${selectedSkillPreview.staminaAfterAction}`
                : selectedSkillPreview.reasons.join(". ")}
            </p>
          )}
          </div>
          <div className="battle-action-bar">
            <button className="button" type="button" onClick={handleTakeTurn} disabled={!fightSummary || isTakingTurn}>
              {isTakingTurn ? "Resolving..." : "Take Turn"}
            </button>
            <button
              className={`button ${autoBattleEnabled ? "button-danger" : "button-accent"}`}
              type="button"
              onClick={() => setAutoBattleEnabled((value) => !value)}
              disabled={!fightSummary}
            >
              {autoBattleEnabled ? "Stop Auto Battle" : "Start Auto Battle"}
            </button>
            <button
              className={`button ${autoStartNextFightEnabled ? "button-danger" : "button-accent"}`}
              type="button"
              onClick={() => setAutoStartNextFightEnabled((value) => !value)}
              disabled={!selectedPlayer || !selectedCharacter || !selectedEnemy}
            >
              {autoStartNextFightEnabled ? "Stop Auto Start" : "Auto Start Next Fight"}
            </button>
            <Link className="button button-muted" to="/">
              Back
            </Link>
          </div>
          <div className="speed-slider-bar">
            <label htmlFor="speed-slider" className="speed-label">
              Speed: {battleSpeed >= 2000 ? "Slow" : battleSpeed >= 1200 ? "Normal" : battleSpeed >= 700 ? "Fast" : "Max"}
            </label>
            <input
              id="speed-slider"
              type="range"
              min="300"
              max="3000"
              step="100"
              value={battleSpeed}
              onChange={(e) => setBattleSpeed(Number(e.target.value))}
              className="speed-range"
            />
          </div>
        </Panel>

        <Panel title="3D Arena Preview">
          {fightEnded && resultTitle && resultSubtitle && (
            <div className={`battle-result-overlay ${fightSummary?.isVictory ? "result-victory" : "result-defeat"}`}>
              <p className="battle-result-title">{resultTitle}</p>
              <p className="battle-result-subtitle">{resultSubtitle}</p>
            </div>
          )}
          <Suspense fallback={<div className="battle-scene" style={{ display: "grid", placeItems: "center", opacity: 0.4 }}>Loading scene…</div>}>
          <BattleScene
            enemyLevel={selectedEnemy?.level ?? 1}
            enemyName={selectedEnemy?.name ?? "Unknown Enemy"}
            enemyType={selectedEnemyType}
            fightStatus={fightSummary?.status ?? "Idle"}
            characterHpRatio={characterHpRatio}
            enemyHpRatio={enemyHpRatio}
            lastMoveActor={latestMove?.isPlayer ? "player" : "enemy"}
            lastMoveSignature={latestMove ? `${latestMove.turn}-${latestMove.timestamp}` : "none"}
            lastMoveDescription={latestMove?.description ?? ""}
            lastMoveElement={latestMoveElement}
            lastMoveDamage={latestMoveDamage}
            autoBattleEnabled={autoBattleEnabled}
          />
          </Suspense>
          {fightSummary && (
            <div className="arena-status-grid">
              <section className="arena-status-card">
                <h3>Player</h3>
                <p className="meter-label">HP {fightSummary.characterCurrentHp}/{fightSummary.characterMaxHp}</p>
                <div className="meter-track">
                  <div className="meter-fill meter-fill-player" style={{ width: `${characterHpRatio * 100}%` }} />
                </div>
                <p className="meter-label">Mana {fightSummary.characterCurrentMana}/{fightSummary.characterMaxMana}</p>
                <div className="meter-track">
                  <div className="meter-fill meter-fill-mana" style={{ width: `${characterManaRatio * 100}%` }} />
                </div>
                <p className="meter-label">Stamina {fightSummary.characterCurrentStamina}/{fightSummary.characterMaxStamina}</p>
                <div className="meter-track">
                  <div className="meter-fill meter-fill-stamina" style={{ width: `${characterStaminaRatio * 100}%` }} />
                </div>
                <div className="resistance-grid">
                  {playerResistanceRows.map((resistance) => (
                    <span key={`player-${resistance.key}`} className="resistance-chip">
                      {resistance.label}: {formatResistanceValue(resistance.value)}
                    </span>
                  ))}
                </div>
              </section>

              <section className="arena-status-card">
                <h3>Enemy</h3>
                <p className="meter-label">HP {fightSummary.enemyCurrentHp}/{fightSummary.enemyMaxHp}</p>
                <div className="meter-track">
                  <div className="meter-fill meter-fill-enemy" style={{ width: `${enemyHpRatio * 100}%` }} />
                </div>
                <p className="meter-label">Mana {fightSummary.enemyCurrentMana}/{fightSummary.enemyMaxMana}</p>
                <div className="meter-track">
                  <div className="meter-fill meter-fill-mana" style={{ width: `${enemyManaRatio * 100}%` }} />
                </div>
                <p className="meter-label">Stamina {fightSummary.enemyCurrentStamina}/{fightSummary.enemyMaxStamina}</p>
                <div className="meter-track">
                  <div className="meter-fill meter-fill-stamina" style={{ width: `${enemyStaminaRatio * 100}%` }} />
                </div>
                <div className="resistance-grid">
                  {enemyResistanceRows.map((resistance) => (
                    <span key={`enemy-${resistance.key}`} className="resistance-chip">
                      {resistance.label}: {formatResistanceValue(resistance.value)}
                    </span>
                  ))}
                </div>
              </section>
            </div>
          )}
        </Panel>
      </div>
    </main>
  );
}

function isPassiveSkill(skill: CharacterSkill): boolean {
  return String(skill.type).toLowerCase() === "passive" || Number(skill.type) === 1;
}

function toElementName(value: CharacterSkill["element"]): string {
  const numeric = Number(value);
  if (!Number.isNaN(numeric)) {
    const map = ["physical", "fire", "ice", "lightning", "poison", "holy", "shadow", "arcane"];
    return map[numeric] ?? "physical";
  }

  return String(value).toLowerCase();
}

function formatResistanceValue(value: number): string {
  const percentage = Math.abs(value) < 10 ? value * 100 : value;
  const rounded = Math.round(percentage * 10) / 10;
  return `${rounded}%`;
}

function getResistanceRows(source?: {
  physicalResistance?: number;
  fireResistance?: number;
  iceResistance?: number;
  lightningResistance?: number;
  poisonResistance?: number;
  holyResistance?: number;
  shadowResistance?: number;
  arcaneResistance?: number;
}) {
  return [
    { key: "physical", label: "Physical", value: source?.physicalResistance ?? 0 },
    { key: "fire", label: "Fire", value: source?.fireResistance ?? 0 },
    { key: "ice", label: "Ice", value: source?.iceResistance ?? 0 },
    { key: "lightning", label: "Lightning", value: source?.lightningResistance ?? 0 },
    { key: "poison", label: "Poison", value: source?.poisonResistance ?? 0 },
    { key: "holy", label: "Holy", value: source?.holyResistance ?? 0 },
    { key: "shadow", label: "Shadow", value: source?.shadowResistance ?? 0 },
    { key: "arcane", label: "Arcane", value: source?.arcaneResistance ?? 0 }
  ];
}
