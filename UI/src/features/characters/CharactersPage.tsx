import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Panel } from "../../components/ui/Panel";
import {
  AVAILABLE_CLASSES,
  type CharacterProgressionDetail,
  type CharacterRow,
  type ClassUpgradeOption,
  type PlayerRow,
  createCharacter,
  deleteCharacter,
  fetchCharacterProgressionDetail,
  fetchClassUpgrades,
  fetchCharacters,
  fetchPlayersBasic,
  renameCharacter,
  upgradeCharacterClass,
} from "./charactersApi";

type EditState = { id: number; name: string; description: string };

export function CharactersPage() {
  const [players, setPlayers] = useState<PlayerRow[]>([]);
  const [selectedPlayerId, setSelectedPlayerId] = useState<number | null>(null);
  const [characters, setCharacters] = useState<CharacterRow[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  // Create form
  const [newName, setNewName] = useState("");
  const [newClass, setNewClass] = useState<string>(AVAILABLE_CLASSES[0]);
  const [newDesc, setNewDesc] = useState("");
  const [isCreating, setIsCreating] = useState(false);

  // Inline rename state
  const [editing, setEditing] = useState<EditState | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  // Confirm delete
  const [confirmDeleteId, setConfirmDeleteId] = useState<number | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  // Class progression
  const [progressionCharacterId, setProgressionCharacterId] = useState<number | null>(null);
  const [progressionOptions, setProgressionOptions] = useState<ClassUpgradeOption[]>([]);
  const [selectedProgressionOptionId, setSelectedProgressionOptionId] = useState<number | null>(null);
  const [progressionCharacterDetail, setProgressionCharacterDetail] = useState<CharacterProgressionDetail | null>(null);
  const [isLoadingProgression, setIsLoadingProgression] = useState(false);
  const [isUpgradingClass, setIsUpgradingClass] = useState<string | null>(null);
  const [progressionError, setProgressionError] = useState<string | null>(null);
  const [progressionSuccess, setProgressionSuccess] = useState<string | null>(null);

  useEffect(() => {
    fetchPlayersBasic()
      .then((rows) => {
        setPlayers(rows);
        if (rows.length > 0) setSelectedPlayerId(rows[0].id);
      })
      .catch(() => setError("Could not load players."));
  }, []);

  useEffect(() => {
    if (!selectedPlayerId) {
      setCharacters([]);
      setProgressionCharacterId(null);
      setProgressionOptions([]);
      setSelectedProgressionOptionId(null);
      setProgressionCharacterDetail(null);
      return;
    }
    setLoading(true);
    setError(null);
    fetchCharacters(selectedPlayerId)
      .then(setCharacters)
      .catch(() => setError("Could not load characters."))
      .finally(() => setLoading(false));
  }, [selectedPlayerId]);

  useEffect(() => {
    if (characters.length === 0) {
      setProgressionCharacterId(null);
      setProgressionOptions([]);
      setSelectedProgressionOptionId(null);
      setProgressionCharacterDetail(null);
      return;
    }

    const stillValid = progressionCharacterId && characters.some((character) => character.id === progressionCharacterId);
    if (!stillValid) {
      setProgressionCharacterId(characters[0].id);
    }
  }, [characters, progressionCharacterId]);

  useEffect(() => {
    if (!selectedPlayerId || !progressionCharacterId) {
      setProgressionOptions([]);
      setSelectedProgressionOptionId(null);
      setProgressionCharacterDetail(null);
      setProgressionError(null);
      return;
    }

    setIsLoadingProgression(true);
    setProgressionError(null);

    Promise.all([
      fetchClassUpgrades(selectedPlayerId, progressionCharacterId),
      fetchCharacterProgressionDetail(selectedPlayerId, progressionCharacterId)
    ])
      .then(([items, detail]) => {
        setProgressionOptions(items);
        setProgressionCharacterDetail(detail);
        setSelectedProgressionOptionId((current) =>
          current && items.some((option) => option.id === current)
            ? current
            : (items[0]?.id ?? null)
        );
      })
      .catch(() => {
        setProgressionOptions([]);
        setSelectedProgressionOptionId(null);
        setProgressionCharacterDetail(null);
        setProgressionError("No class progression options available right now.");
      })
      .finally(() => setIsLoadingProgression(false));
  }, [progressionCharacterId, selectedPlayerId]);

  const flash = (msg: string) => {
    setSuccess(msg);
    window.setTimeout(() => setSuccess(null), 3000);
  };

  const handleCreate = async () => {
    if (!selectedPlayerId || !newName.trim()) return;
    setIsCreating(true);
    setError(null);
    try {
      const created = await createCharacter(selectedPlayerId, {
        name: newName.trim(),
        class: newClass,
        description: newDesc.trim() || undefined,
      });
      setCharacters((prev) => [...prev, created]);
      setNewName("");
      setNewDesc("");
      flash(`${created.name} created!`);
    } catch {
      setError("Failed to create character. Check API or try a different name.");
    } finally {
      setIsCreating(false);
    }
  };

  const handleSaveRename = async () => {
    if (!editing || !selectedPlayerId) return;
    const char = characters.find((c) => c.id === editing.id);
    if (!char) return;
    setIsSaving(true);
    setError(null);
    try {
      await renameCharacter(selectedPlayerId, editing.id, {
        name: editing.name.trim(),
        class: char.class?.name ?? AVAILABLE_CLASSES[0],
        description: editing.description,
      });
      setCharacters((prev) =>
        prev.map((c) =>
          c.id === editing.id ? { ...c, name: editing.name.trim(), description: editing.description } : c
        )
      );
      setEditing(null);
      flash("Character updated.");
    } catch {
      setError("Failed to save changes.");
    } finally {
      setIsSaving(false);
    }
  };

  const handleDelete = async (id: number) => {
    if (!selectedPlayerId) return;
    setIsDeleting(true);
    setError(null);
    try {
      await deleteCharacter(selectedPlayerId, id);
      setCharacters((prev) => prev.filter((c) => c.id !== id));
      setConfirmDeleteId(null);
      flash("Character deleted.");
    } catch {
      setError("Failed to delete character.");
    } finally {
      setIsDeleting(false);
    }
  };

  const handleUpgradeClass = async (nextClassName: string) => {
    if (!selectedPlayerId || !progressionCharacterId) {
      return;
    }

    setIsUpgradingClass(nextClassName);
    setProgressionError(null);
    setProgressionSuccess(null);

    try {
      const upgraded = await upgradeCharacterClass(selectedPlayerId, progressionCharacterId, nextClassName);

      setCharacters((current) =>
        current.map((character) => (character.id === upgraded.id ? { ...character, ...upgraded } : character))
      );
      setProgressionSuccess(`${upgraded.name} advanced to ${upgraded.class?.name ?? nextClassName}.`);

      const [refreshedOptions, refreshedDetail] = await Promise.all([
        fetchClassUpgrades(selectedPlayerId, progressionCharacterId),
        fetchCharacterProgressionDetail(selectedPlayerId, progressionCharacterId)
      ]);
      setProgressionOptions(refreshedOptions);
      setProgressionCharacterDetail(refreshedDetail);
      setSelectedProgressionOptionId(refreshedOptions[0]?.id ?? null);
    } catch {
      setProgressionError("Class upgrade failed. Check level requirements and branch restrictions.");
    } finally {
      setIsUpgradingClass(null);
    }
  };

  const progressionCharacter = characters.find((character) => character.id === progressionCharacterId) ?? null;
  const selectedProgressionOption = progressionOptions.find((option) => option.id === selectedProgressionOptionId) ?? null;
  const unlockedSkills = selectedProgressionOption
    ? selectedProgressionOption.skills.filter(
        (skill) => !progressionCharacterDetail?.skills.some((existing) => existing.id === skill.id)
      )
    : [];

  return (
    <main className="page">
      <header>
        <h1>Character Management</h1>
        <p>Create, rename, or delete characters for any player.</p>
      </header>

      <div className="char-mgmt-layout">
        {/* Left: player + create */}
        <Panel title="Player & New Character">
          <label htmlFor="cm-player">Player</label>
          <select
            id="cm-player"
            value={selectedPlayerId ?? ""}
            onChange={(e) => {
              setSelectedPlayerId(e.target.value ? Number(e.target.value) : null);
              setEditing(null);
              setConfirmDeleteId(null);
            }}
          >
            <option value="">Select player</option>
            {players.map((p) => (
              <option key={p.id} value={p.id}>{p.username}</option>
            ))}
          </select>

          <p className="section-label">Create new character</p>
          <label htmlFor="cm-name">Name</label>
          <input
            id="cm-name"
            className="text-input"
            type="text"
            value={newName}
            placeholder="Character name"
            maxLength={40}
            onChange={(e) => setNewName(e.target.value)}
          />

          <label htmlFor="cm-class">Class</label>
          <select
            id="cm-class"
            value={newClass}
            onChange={(e) => setNewClass(e.target.value)}
          >
            {AVAILABLE_CLASSES.map((cls) => (
              <option key={cls} value={cls}>{cls}</option>
            ))}
          </select>

          <label htmlFor="cm-desc">Description (optional)</label>
          <input
            id="cm-desc"
            className="text-input"
            type="text"
            value={newDesc}
            placeholder="Short description"
            maxLength={120}
            onChange={(e) => setNewDesc(e.target.value)}
          />

          <button
            className="button"
            type="button"
            disabled={!selectedPlayerId || !newName.trim() || isCreating}
            onClick={handleCreate}
          >
            {isCreating ? "Creating…" : "Create Character"}
          </button>

          {success && <p className="success-msg">{success}</p>}
          {error && <p className="error">{error}</p>}

          <Link className="button button-muted" to="/" style={{ marginTop: "1rem" }}>
            Back to Home
          </Link>
        </Panel>

        {/* Right: character list */}
        <Panel title={`Characters${selectedPlayerId ? ` (${characters.length})` : ""}`}>
          {loading && <p style={{ opacity: 0.6 }}>Loading…</p>}
          {!loading && characters.length === 0 && selectedPlayerId && (
            <p style={{ opacity: 0.6 }}>No characters yet. Create one on the left.</p>
          )}
          {!loading && !selectedPlayerId && (
            <p style={{ opacity: 0.6 }}>Select a player to see their characters.</p>
          )}

          <div className="char-list">
            {characters.map((char) => {
              const isEditingThis = editing?.id === char.id;
              const isConfirmingDelete = confirmDeleteId === char.id;
              return (
                <div key={char.id} className="char-card">
                  {isEditingThis ? (
                    <>
                      <input
                        className="text-input char-edit-name"
                        value={editing.name}
                        maxLength={40}
                        onChange={(e) => setEditing({ ...editing, name: e.target.value })}
                        autoFocus
                      />
                      <input
                        className="text-input char-edit-desc"
                        value={editing.description}
                        placeholder="Description"
                        maxLength={120}
                        onChange={(e) => setEditing({ ...editing, description: e.target.value })}
                      />
                      <div className="char-card-actions">
                        <button className="button" type="button" disabled={isSaving} onClick={handleSaveRename}>
                          {isSaving ? "Saving…" : "Save"}
                        </button>
                        <button className="button button-muted" type="button" onClick={() => setEditing(null)}>
                          Cancel
                        </button>
                      </div>
                    </>
                  ) : (
                    <>
                      <div className="char-card-info">
                        <span className="char-card-name">{char.name}</span>
                        <span className="char-card-meta">
                          {char.class?.name ?? "Unknown"} · Lv {char.level} · {char.experience} XP
                        </span>
                        {char.description && (
                          <span className="char-card-desc">{char.description}</span>
                        )}
                      </div>
                      <div className="char-card-actions">
                        {isConfirmingDelete ? (
                          <>
                            <span className="char-delete-confirm">Delete {char.name}?</span>
                            <button
                              className="button button-danger"
                              type="button"
                              disabled={isDeleting}
                              onClick={() => void handleDelete(char.id)}
                            >
                              {isDeleting ? "Deleting…" : "Yes, delete"}
                            </button>
                            <button
                              className="button button-muted"
                              type="button"
                              onClick={() => setConfirmDeleteId(null)}
                            >
                              Cancel
                            </button>
                          </>
                        ) : (
                          <>
                            <button
                              className="button button-accent"
                              type="button"
                              onClick={() => setEditing({ id: char.id, name: char.name, description: char.description ?? "" })}
                            >
                              Rename
                            </button>
                            <button
                              className="button button-danger"
                              type="button"
                              onClick={() => setConfirmDeleteId(char.id)}
                            >
                              Delete
                            </button>
                          </>
                        )}
                      </div>
                    </>
                  )}
                </div>
              );
            })}
          </div>
        </Panel>

        <Panel title="Class Progression">
          <div className="progression-layout">
            <div>
              <label htmlFor="progression-character">Choose character</label>
              <select
                id="progression-character"
                value={progressionCharacterId ?? ""}
                onChange={(event) => setProgressionCharacterId(event.target.value ? Number(event.target.value) : null)}
              >
                <option value="">Select character</option>
                {characters.map((character) => (
                  <option key={character.id} value={character.id}>
                    {character.name} | {character.class?.name ?? "Unknown"} | Lv {character.level}
                  </option>
                ))}
              </select>

              <p className="progression-help">
                Current: {progressionCharacter ? `${progressionCharacter.class?.name ?? "Unknown"} (Tier ${progressionCharacter.class?.tier ?? 0})` : "-"}
              </p>
              <p className="progression-help">
                Level: {progressionCharacter?.level ?? "-"}
              </p>

              {progressionSuccess && <p className="success-msg">{progressionSuccess}</p>}
              {progressionError && <p className="error">{progressionError}</p>}
            </div>

            <div>
              <p className="section-label">Available Upgrades</p>
              {isLoadingProgression && <p className="progression-help">Loading progression options...</p>}
              {!isLoadingProgression && progressionOptions.length === 0 && (
                <p className="progression-help">No upgrade options yet. Level up or unlock branch requirements.</p>
              )}

              <div className="progression-list">
                {progressionOptions.map((option) => (
                  <div
                    key={option.id}
                    className={`progression-item ${selectedProgressionOptionId === option.id ? "progression-item-selected" : ""}`}
                  >
                    <strong>{option.name}</strong>
                    <span>Tier {option.tier} | Required Lv {option.requiredLevel}</span>
                    <span>Archetype: {option.archetype}</span>
                    <span>Skills unlocked: {option.skills.length}</span>
                    <button
                      className="button button-accent"
                      type="button"
                      disabled={isUpgradingClass !== null}
                      onClick={() => setSelectedProgressionOptionId(option.id)}
                    >
                      Preview
                    </button>
                  </div>
                ))}
              </div>

              {selectedProgressionOption && (
                <div className="progression-preview">
                  <p className="section-label">Preview</p>
                  <p className="progression-help">
                    {progressionCharacterDetail?.class?.name ?? "Current"} → {selectedProgressionOption.name}
                  </p>
                  <div className="progression-delta-grid">
                    {buildStatDeltas(progressionCharacterDetail, selectedProgressionOption).map((row) => (
                      <div key={row.label} className="progression-delta-row">
                        <span>{row.label}</span>
                        <span className={row.delta > 0 ? "delta-positive" : row.delta < 0 ? "delta-negative" : "delta-neutral"}>
                          {row.current} → {row.next} ({row.delta >= 0 ? "+" : ""}{row.delta})
                        </span>
                      </div>
                    ))}
                  </div>
                  <div className="progression-unlocks">
                    <p className="progression-help">Newly unlocked skills</p>
                    {unlockedSkills.length === 0 ? (
                      <p className="progression-help">No new skills from this class.</p>
                    ) : (
                      <ul className="progression-unlocks-list">
                        {unlockedSkills.map((skill) => (
                          <li key={skill.id}>{skill.name} (Lv {skill.requiredLevel})</li>
                        ))}
                      </ul>
                    )}
                  </div>
                  <button
                    className="button"
                    type="button"
                    disabled={
                      !progressionCharacter ||
                      progressionCharacter.level < selectedProgressionOption.requiredLevel ||
                      isUpgradingClass !== null
                    }
                    onClick={() => void handleUpgradeClass(selectedProgressionOption.name)}
                  >
                    {isUpgradingClass === selectedProgressionOption.name ? "Upgrading..." : "Confirm Progression"}
                  </button>
                </div>
              )}
            </div>
          </div>
        </Panel>
      </div>
    </main>
  );
}

function buildStatDeltas(detail: CharacterProgressionDetail | null, option: ClassUpgradeOption) {
  const current = detail?.class;
  return [
    { label: "Strength", current: current?.baseStrength ?? 0, next: option.baseStrength },
    { label: "Agility", current: current?.baseAgility ?? 0, next: option.baseAgility },
    { label: "Intelligence", current: current?.baseIntelligence ?? 0, next: option.baseIntelligence },
    { label: "Wisdom", current: current?.baseWisdom ?? 0, next: option.baseWisdom },
    { label: "Charisma", current: current?.baseCharisma ?? 0, next: option.baseCharisma },
    { label: "Endurance", current: current?.baseEndurance ?? 0, next: option.baseEndurance },
    { label: "Luck", current: current?.baseLuck ?? 0, next: option.baseLuck }
  ].map((row) => ({
    ...row,
    delta: row.next - row.current
  }));
}
