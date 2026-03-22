import { Link } from "react-router-dom";
import { Panel } from "../../components/ui/Panel";

export function HomePage() {
  return (
    <main className="page page-home">
      <header>
        <h1>RPG Control Room</h1>
        <p>React + Three.js client connected to your existing .NET RPG API.</p>
      </header>

      <Panel title="Playtest">
        <p>Open the battle scene to preview 3D rendering and API data wiring.</p>
        <Link className="button" to="/battle">
          Enter Battle Sandbox
        </Link>
      </Panel>

      <Panel title="Characters">
        <p>Create, rename, or delete characters for any player.</p>
        <Link className="button" to="/characters">
          Manage Characters
        </Link>
      </Panel>

      <Panel title="Progression">
        <p>Configure loadouts and equipment, then inspect talents, challenges, and seasonal ladder data.</p>
        <Link className="button" to="/progression">
          Open Progression Console
        </Link>
      </Panel>
    </main>
  );
}
