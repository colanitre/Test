import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Panel } from "../../components/ui/Panel";
import {
  AVAILABLE_CLASSES,
  type CharacterRow,
  type PlayerRow,
  createCharacter,
  deleteCharacter,
  fetchCharacters,
  fetchPlayersBasic,
  renameCharacter,
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
      return;
    }
    setLoading(true);
    setError(null);
    fetchCharacters(selectedPlayerId)
      .then(setCharacters)
      .catch(() => setError("Could not load characters."))
      .finally(() => setLoading(false));
  }, [selectedPlayerId]);

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
      </div>
    </main>
  );
}
