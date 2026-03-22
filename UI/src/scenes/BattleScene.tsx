import { Canvas, useFrame } from "@react-three/fiber";
import { OrbitControls, Stars, Text } from "@react-three/drei";
import { useEffect, useRef, useState } from "react";
import type { MutableRefObject, RefObject } from "react";
import { BackSide } from "three";
import type { Group, Mesh, MeshBasicMaterial, PointLight } from "three";

type BattleSceneProps = {
  enemyLevel: number;
  enemyName: string;
  enemyType: string;
  fightStatus: string;
  characterHpRatio: number;
  enemyHpRatio: number;
  lastMoveActor: "player" | "enemy";
  lastMoveSignature: string;
  lastMoveDescription: string;
  lastMoveElement: string;
  lastMoveDamage: number;
  autoBattleEnabled: boolean;
};

type FloatingImpact = {
  id: string;
  actor: "player" | "enemy";
  damage: number;
  color: string;
};

type EffectPalette = {
  core: string;
  glow: string;
};

function getElementPalette(element: string): EffectPalette {
  switch (element) {
    case "fire":
      return { core: "#ff744f", glow: "#ffb16d" };
    case "ice":
      return { core: "#7de6ff", glow: "#c4f6ff" };
    case "lightning":
      return { core: "#ffe47d", glow: "#fff2ba" };
    case "poison":
      return { core: "#8ce280", glow: "#bff4a1" };
    case "holy":
      return { core: "#ffeaa8", glow: "#fff8dd" };
    case "shadow":
      return { core: "#9e92ff", glow: "#c1bbff" };
    case "arcane":
      return { core: "#b094ff", glow: "#d0bbff" };
    default:
      return { core: "#8fd8ff", glow: "#c4efff" };
  }
}

function inferEnemyVisualType(enemyType: string, enemyName: string): string {
  const combined = `${enemyType} ${enemyName}`.toLowerCase();
  if (combined.includes("phoenix")) return "phoenix";
  if (combined.includes("dragon")) return "dragon";
  if (combined.includes("lich")) return "lich";
  if (combined.includes("dark mage")) return "dark-mage";
  if (combined.includes("shadow assassin")) return "shadow-assassin";
  if (combined.includes("skeleton")) return "skeleton";
  if (combined.includes("troll")) return "troll";
  if (combined.includes("goblin")) return "goblin";
  if (combined.includes("orc")) return "orc";
  if (combined.includes("sea lord")) return "sea-lord";
  if (combined.includes("skyfather")) return "skyfather";
  if (combined.includes("underworld king")) return "underworld-king";
  if (combined.includes("warbringer")) return "warbringer";
  if (combined.includes("radiant oracle")) return "radiant-oracle";
  if (combined.includes("mage") || combined.includes("oracle")) return "caster";
  if (combined.includes("shadow") || combined.includes("assassin") || combined.includes("rogue")) return "assassin";
  if (combined.includes("god") || combined.includes("olympian") || combined.includes("lord") || combined.includes("king")) return "deity";
  if (combined.includes("troll") || combined.includes("goblin") || combined.includes("orc") || combined.includes("skeleton")) return "brute";
  if (combined.includes("beast")) return "beast";
  return combined.includes("boss") ? "deity" : "brute";
}

type EnemyModelProps = {
  visualType: string;
  enemyColor: string;
  fightStatus: string;
};

function getEnemyAuraColor(visualType: string): string {
  switch (visualType) {
    case "dragon":
    case "phoenix":
      return "#ffb26b";
    case "lich":
    case "dark-mage":
    case "caster":
      return "#b699ff";
    case "shadow-assassin":
    case "assassin":
      return "#8ca0ff";
    case "sea-lord":
      return "#7fd9ff";
    case "skyfather":
    case "radiant-oracle":
      return "#ffe38a";
    case "underworld-king":
      return "#ab88ff";
    case "warbringer":
      return "#ff8d72";
    case "troll":
    case "goblin":
    case "orc":
    case "brute":
      return "#b4e17a";
    default:
      return "#ff7a7a";
  }
}

function EnemyModel({ visualType, enemyColor, fightStatus }: EnemyModelProps) {
  const emissive = fightStatus.toLowerCase().includes("progress") ? "#3a1111" : "#1b1111";

  if (visualType === "dragon") {
    return (
      <group>
        <mesh position={[0, 0.05, 0]} scale={[1.2, 0.7, 1]}>
          <sphereGeometry args={[0.72, 24, 24]} />
          <meshStandardMaterial color={enemyColor} emissive={emissive} />
        </mesh>
        <mesh position={[0.85, 0.22, 0]} rotation={[0, 0, -0.8]}>
          <coneGeometry args={[0.18, 1.2, 6]} />
          <meshStandardMaterial color="#ffcf99" emissive="#6f3f12" />
        </mesh>
        <mesh position={[-0.65, 0.45, 0]} rotation={[0, 0, 1.2]}>
          <boxGeometry args={[0.18, 1.4, 0.08]} />
          <meshStandardMaterial color="#724247" emissive="#261416" />
        </mesh>
        <mesh position={[0, 0.72, 0]} scale={[1.5, 0.25, 0.8]}>
          <sphereGeometry args={[0.45, 18, 18]} />
          <meshStandardMaterial color="#5f3340" emissive="#281318" />
        </mesh>
      </group>
    );
  }

  if (visualType === "phoenix") {
    return (
      <group>
        <mesh position={[0, 0.2, 0]}>
          <octahedronGeometry args={[0.7, 0]} />
          <meshStandardMaterial color="#ff9d57" emissive="#8f2a1c" />
        </mesh>
        <mesh position={[0.7, 0.2, 0]} rotation={[0, 0, -1.0]}>
          <coneGeometry args={[0.25, 1.25, 5]} />
          <meshStandardMaterial color="#ffd26e" emissive="#8f4e11" />
        </mesh>
        <mesh position={[-0.7, 0.2, 0]} rotation={[0, 0, 1.0]}>
          <coneGeometry args={[0.25, 1.25, 5]} />
          <meshStandardMaterial color="#ffd26e" emissive="#8f4e11" />
        </mesh>
        <mesh position={[0, -0.55, 0]} rotation={[0, 0, Math.PI]}>
          <coneGeometry args={[0.16, 0.9, 6]} />
          <meshStandardMaterial color="#ff784f" emissive="#74261d" />
        </mesh>
      </group>
    );
  }

  if (visualType === "lich") {
    return (
      <group>
        <mesh position={[0, 0.5, 0]}>
          <sphereGeometry args={[0.34, 18, 18]} />
          <meshStandardMaterial color="#d8e0e4" emissive="#38444a" />
        </mesh>
        <mesh position={[0, -0.1, 0]}>
          <coneGeometry args={[0.65, 1.8, 7]} />
          <meshStandardMaterial color="#6a5c95" emissive="#231934" />
        </mesh>
        <mesh position={[0.62, 0.05, 0]} rotation={[0.15, 0, 0]}>
          <boxGeometry args={[0.08, 1.15, 0.08]} />
          <meshStandardMaterial color="#d1c6ff" emissive="#4c3b83" />
        </mesh>
      </group>
    );
  }

  if (visualType === "dark-mage") {
    return (
      <group>
        <mesh position={[0, 0.1, 0]}>
          <capsuleGeometry args={[0.24, 1.15, 8, 14]} />
          <meshStandardMaterial color="#7d66c7" emissive="#281b52" />
        </mesh>
        <mesh position={[0, 0.98, 0]}>
          <coneGeometry args={[0.34, 0.5, 8]} />
          <meshStandardMaterial color="#2e214e" emissive="#10091f" />
        </mesh>
        <mesh position={[0.55, 0.15, 0]} rotation={[0.2, 0, 0]}>
          <boxGeometry args={[0.12, 1.1, 0.12]} />
          <meshStandardMaterial color="#9e92ff" emissive="#41338c" />
        </mesh>
      </group>
    );
  }

  if (visualType === "shadow-assassin") {
    return (
      <group>
        <mesh scale={[0.72, 1.2, 0.72]}>
          <icosahedronGeometry args={[0.72, 0]} />
          <meshStandardMaterial color="#7e749f" emissive="#1d1831" />
        </mesh>
        <mesh position={[0.72, 0.05, 0]} rotation={[0, 0, 0.85]}>
          <boxGeometry args={[0.06, 1.05, 0.06]} />
          <meshStandardMaterial color="#cdd4f6" emissive="#495687" />
        </mesh>
        <mesh position={[-0.72, 0.05, 0]} rotation={[0, 0, -0.85]}>
          <boxGeometry args={[0.06, 1.05, 0.06]} />
          <meshStandardMaterial color="#cdd4f6" emissive="#495687" />
        </mesh>
      </group>
    );
  }

  if (visualType === "skeleton") {
    return (
      <group>
        <mesh position={[0, 0.4, 0]}>
          <sphereGeometry args={[0.28, 16, 16]} />
          <meshStandardMaterial color="#f2f0e8" emissive="#55514d" />
        </mesh>
        <mesh position={[0, -0.15, 0]} scale={[0.55, 1.2, 0.45]}>
          <boxGeometry args={[1, 1, 1]} />
          <meshStandardMaterial color="#ded9cf" emissive="#46423b" />
        </mesh>
        <mesh position={[0.48, -0.15, 0]} rotation={[0, 0, 0.18]}>
          <boxGeometry args={[0.08, 1.15, 0.08]} />
          <meshStandardMaterial color="#ebe4d9" emissive="#524d43" />
        </mesh>
      </group>
    );
  }

  if (visualType === "troll") {
    return (
      <group>
        <mesh scale={[1.15, 1.35, 1.0]}>
          <capsuleGeometry args={[0.38, 1.05, 8, 14]} />
          <meshStandardMaterial color="#8ab06e" emissive="#283920" />
        </mesh>
        <mesh position={[0.62, -0.08, 0]} rotation={[0, 0, 0.48]}>
          <boxGeometry args={[0.15, 1.25, 0.15]} />
          <meshStandardMaterial color="#6d5240" emissive="#2a1d15" />
        </mesh>
      </group>
    );
  }

  if (visualType === "goblin") {
    return (
      <group>
        <mesh scale={[0.78, 0.98, 0.78]}>
          <capsuleGeometry args={[0.26, 0.9, 8, 14]} />
          <meshStandardMaterial color="#7cc26b" emissive="#20361d" />
        </mesh>
        <mesh position={[0, 0.85, 0]} rotation={[0, 0, 0.2]}>
          <coneGeometry args={[0.15, 0.4, 4]} />
          <meshStandardMaterial color="#dfe689" emissive="#575d24" />
        </mesh>
      </group>
    );
  }

  if (visualType === "orc") {
    return (
      <group>
        <mesh scale={[1.0, 1.15, 0.92]}>
          <capsuleGeometry args={[0.34, 1.0, 8, 14]} />
          <meshStandardMaterial color="#85a34e" emissive="#293517" />
        </mesh>
        <mesh position={[0.52, 0.05, 0]} rotation={[0, 0, 0.24]}>
          <boxGeometry args={[0.12, 1.05, 0.12]} />
          <meshStandardMaterial color="#a3886b" emissive="#3b2d20" />
        </mesh>
      </group>
    );
  }

  if (visualType === "sea-lord") {
    return (
      <group>
        <mesh position={[0, 0.05, 0]}>
          <icosahedronGeometry args={[0.82, 0]} />
          <meshStandardMaterial color="#64c7ff" emissive="#133e59" metalness={0.25} roughness={0.25} />
        </mesh>
        <mesh position={[0.74, 0.02, 0]} rotation={[0, 0, 0.2]}>
          <boxGeometry args={[0.08, 1.35, 0.08]} />
          <meshStandardMaterial color="#d7efff" emissive="#4c7ea5" />
        </mesh>
        <mesh position={[0, -0.7, 0]} scale={[1.25, 0.24, 1.25]}>
          <sphereGeometry args={[0.58, 18, 18]} />
          <meshStandardMaterial color="#1c4c67" emissive="#0c2231" />
        </mesh>
      </group>
    );
  }

  if (visualType === "skyfather") {
    return (
      <group>
        <mesh position={[0, 0.08, 0]}>
          <icosahedronGeometry args={[0.84, 0]} />
          <meshStandardMaterial color="#ffe07a" emissive="#6d560d" metalness={0.35} roughness={0.2} />
        </mesh>
        <mesh position={[0, 1.0, 0]}>
          <torusGeometry args={[0.46, 0.07, 14, 36]} />
          <meshStandardMaterial color="#fff4b8" emissive="#8a7a2e" />
        </mesh>
        <mesh position={[0.68, 0.12, 0]} rotation={[0.2, 0, 0.25]}>
          <boxGeometry args={[0.08, 1.25, 0.08]} />
          <meshStandardMaterial color="#d3e7ff" emissive="#56739b" />
        </mesh>
      </group>
    );
  }

  if (visualType === "underworld-king") {
    return (
      <group>
        <mesh position={[0, 0.08, 0]}>
          <dodecahedronGeometry args={[0.82, 0]} />
          <meshStandardMaterial color="#7d6cff" emissive="#23194c" metalness={0.3} roughness={0.25} />
        </mesh>
        <mesh position={[0, 1.02, 0]}>
          <coneGeometry args={[0.28, 0.38, 6]} />
          <meshStandardMaterial color="#c4b6ff" emissive="#5d53a0" />
        </mesh>
        <mesh position={[0, -0.72, 0]} scale={[1.3, 0.24, 1.3]}>
          <sphereGeometry args={[0.58, 18, 18]} />
          <meshStandardMaterial color="#382650" emissive="#140d1f" />
        </mesh>
      </group>
    );
  }

  if (visualType === "warbringer") {
    return (
      <group>
        <mesh scale={[1.1, 1.18, 1.0]}>
          <capsuleGeometry args={[0.36, 1.1, 8, 14]} />
          <meshStandardMaterial color="#d06753" emissive="#4e1f17" metalness={0.2} roughness={0.3} />
        </mesh>
        <mesh position={[0.72, 0.12, 0]} rotation={[0.1, 0, -0.2]}>
          <boxGeometry args={[0.14, 1.28, 0.14]} />
          <meshStandardMaterial color="#d9d4c8" emissive="#4f4940" />
        </mesh>
        <mesh position={[0, 1.0, 0]}>
          <torusGeometry args={[0.4, 0.05, 14, 28]} />
          <meshStandardMaterial color="#ffb95c" emissive="#6e4510" />
        </mesh>
      </group>
    );
  }

  if (visualType === "radiant-oracle") {
    return (
      <group>
        <mesh position={[0, 0.05, 0]}>
          <capsuleGeometry args={[0.26, 1.2, 8, 14]} />
          <meshStandardMaterial color="#ffd983" emissive="#6b5012" metalness={0.15} roughness={0.25} />
        </mesh>
        <mesh position={[0, 1.02, 0]}>
          <coneGeometry args={[0.36, 0.48, 8]} />
          <meshStandardMaterial color="#fff3d2" emissive="#9e8640" />
        </mesh>
        <mesh position={[0.62, 0.12, 0]} rotation={[0.15, 0, 0]}>
          <boxGeometry args={[0.1, 1.1, 0.1]} />
          <meshStandardMaterial color="#dff7ff" emissive="#6a97a9" />
        </mesh>
      </group>
    );
  }

  if (visualType === "beast") {
    return (
      <group>
        <mesh position={[0, 0.05, 0]}>
          <octahedronGeometry args={[0.82, 0]} />
          <meshStandardMaterial color={enemyColor} emissive={emissive} />
        </mesh>
        <mesh position={[0.5, 0.4, 0]} rotation={[0, 0, 0.45]}>
          <coneGeometry args={[0.18, 0.75, 5]} />
          <meshStandardMaterial color="#ffd7a1" emissive="#67411a" />
        </mesh>
        <mesh position={[-0.5, 0.4, 0]} rotation={[0, 0, -0.45]}>
          <coneGeometry args={[0.18, 0.75, 5]} />
          <meshStandardMaterial color="#ffd7a1" emissive="#67411a" />
        </mesh>
        <mesh position={[0, -0.55, 0]} scale={[1.15, 0.35, 1.1]}>
          <sphereGeometry args={[0.6, 18, 18]} />
          <meshStandardMaterial color="#472d2d" emissive="#1f1414" />
        </mesh>
      </group>
    );
  }

  if (visualType === "caster") {
    return (
      <group>
        <mesh position={[0, 0.05, 0]}>
          <capsuleGeometry args={[0.26, 1.25, 8, 14]} />
          <meshStandardMaterial color={enemyColor} emissive={emissive} />
        </mesh>
        <mesh position={[0, 1.05, 0]}>
          <coneGeometry args={[0.38, 0.55, 7]} />
          <meshStandardMaterial color="#cab8ff" emissive="#4a3794" />
        </mesh>
        <mesh position={[0.58, 0.15, 0]} rotation={[0.2, 0, 0]}>
          <boxGeometry args={[0.12, 1.05, 0.12]} />
          <meshStandardMaterial color="#e7d1ff" emissive="#5d4399" />
        </mesh>
      </group>
    );
  }

  if (visualType === "assassin") {
    return (
      <group>
        <mesh position={[0, 0, 0]} scale={[0.85, 1.15, 0.85]}>
          <dodecahedronGeometry args={[0.72, 0]} />
          <meshStandardMaterial color={enemyColor} emissive={emissive} />
        </mesh>
        <mesh position={[0.62, 0.08, 0]} rotation={[0, 0, 0.8]}>
          <boxGeometry args={[0.08, 0.92, 0.08]} />
          <meshStandardMaterial color="#cfd6ff" emissive="#40518a" />
        </mesh>
        <mesh position={[-0.62, 0.08, 0]} rotation={[0, 0, -0.8]}>
          <boxGeometry args={[0.08, 0.92, 0.08]} />
          <meshStandardMaterial color="#cfd6ff" emissive="#40518a" />
        </mesh>
      </group>
    );
  }

  if (visualType === "deity") {
    return (
      <group>
        <mesh position={[0, 0.05, 0]}>
          <icosahedronGeometry args={[0.85, 0]} />
          <meshStandardMaterial color={enemyColor} emissive={emissive} metalness={0.4} roughness={0.2} />
        </mesh>
        <mesh position={[0, 1.02, 0]}>
          <torusGeometry args={[0.44, 0.06, 14, 36]} />
          <meshStandardMaterial color="#ffe37d" emissive="#7a4a00" />
        </mesh>
        <mesh position={[0, -0.75, 0]} scale={[1.4, 0.25, 1.4]}>
          <sphereGeometry args={[0.58, 18, 18]} />
          <meshStandardMaterial color="#3d2749" emissive="#1a1120" />
        </mesh>
      </group>
    );
  }

  return (
    <group>
      <mesh>
        <dodecahedronGeometry args={[0.75, 0]} />
        <meshStandardMaterial color={enemyColor} emissive={emissive} />
      </mesh>
      <mesh position={[0, 1.05, 0]}>
        <coneGeometry args={[0.22, 0.45, 6]} />
        <meshStandardMaterial color="#ffe37d" emissive="#7a4a00" />
      </mesh>
    </group>
  );
}

function BattleActors({
  enemyLevel,
  enemyName,
  enemyType,
  fightStatus,
  characterHpRatio,
  enemyHpRatio,
  lastMoveActor,
  lastMoveSignature
}: BattleSceneProps) {
  const enemyRef = useRef<Group>(null);
  const playerGroupRef = useRef<Group>(null);
  const playerRef = useRef<Mesh>(null);
  const playerAuraRef = useRef<Mesh>(null);
  const enemyAuraRef = useRef<Mesh>(null);

  const pulse = fightStatus.toLowerCase().includes("progress") ? 1.15 : 1.0;
  const enemyColor = enemyLevel >= 100 ? "#ff6b6b" : enemyLevel >= 40 ? "#f7b267" : "#d9d9d9";
  const visualType = inferEnemyVisualType(enemyType, enemyName);
  const enemyAuraColor = getEnemyAuraColor(visualType);

  useEffect(() => {
    if (lastMoveActor === "player" && playerAuraRef.current) {
      playerAuraRef.current.scale.setScalar(1.25);
    }
    if (lastMoveActor === "enemy" && enemyAuraRef.current) {
      enemyAuraRef.current.scale.setScalar(1.25);
    }
  }, [lastMoveActor, lastMoveSignature]);

  useFrame((state, delta) => {
    const wobble = Math.sin(state.clock.elapsedTime * 2.2) * 0.08;

    if (enemyRef.current) {
      enemyRef.current.rotation.y += delta * 0.7;
      enemyRef.current.position.y = wobble;
      enemyRef.current.scale.setScalar(pulse);
      if (enemyHpRatio <= 0) {
        enemyRef.current.rotation.z = Math.min(1.25, enemyRef.current.rotation.z + delta * 1.7);
        enemyRef.current.position.y = Math.max(-0.72, enemyRef.current.position.y - delta * 0.95);
      }
    }

    if (playerRef.current) {
      playerRef.current.rotation.y -= delta * 0.5;
      playerRef.current.position.y = -wobble * 0.8;
    }

    if (playerGroupRef.current && characterHpRatio <= 0) {
      playerGroupRef.current.rotation.z = Math.max(-1.05, playerGroupRef.current.rotation.z - delta * 1.7);
      playerGroupRef.current.position.y = Math.max(-0.78, playerGroupRef.current.position.y - delta * 0.9);
    }

    if (playerAuraRef.current) {
      playerAuraRef.current.rotation.z += delta * 0.8;
      const next = Math.max(1.0, playerAuraRef.current.scale.x - delta * 0.45);
      playerAuraRef.current.scale.setScalar(next);
    }

    if (enemyAuraRef.current) {
      enemyAuraRef.current.rotation.z -= delta * 0.8;
      const next = Math.max(1.0, enemyAuraRef.current.scale.x - delta * 0.45);
      enemyAuraRef.current.scale.setScalar(next);
    }
  });

  return (
    <group>
      <group ref={playerGroupRef} position={[-1.25, -0.1, 0]}>
        <mesh ref={playerRef}>
          <capsuleGeometry args={[0.3, 1.15, 8, 18]} />
          <meshStandardMaterial color="#64f4c1" roughness={0.3} metalness={0.2} />
        </mesh>
        <mesh position={[0.58, 0.18, 0.05]} rotation={[0.15, 0.1, 0.1]}>
          <boxGeometry args={[0.16, 0.86, 0.14]} />
          <meshStandardMaterial color="#9bb7ff" emissive="#243a75" />
        </mesh>
        <mesh ref={playerAuraRef} position={[0, -0.25, 0]} rotation={[-Math.PI / 2, 0, 0]}>
          <torusGeometry args={[0.8, 0.04, 14, 44]} />
          <meshStandardMaterial color="#9ef7df" emissive="#1d6459" />
        </mesh>
      </group>

      <group ref={enemyRef} position={[1.25, 0, 0]}>
        <EnemyModel visualType={visualType} enemyColor={enemyColor} fightStatus={fightStatus} />
        <mesh ref={enemyAuraRef} position={[0, -0.35, 0]} rotation={[-Math.PI / 2, 0, 0]}>
          <torusGeometry args={[0.95, 0.055, 14, 44]} />
          <meshStandardMaterial color={enemyAuraColor} emissive={enemyAuraColor} emissiveIntensity={0.35} />
        </mesh>
        <mesh position={[0, -0.35, 0]} rotation={[-Math.PI / 2, 0, 0]}>
          <torusGeometry args={[1.15, 0.02, 14, 44]} />
          <meshStandardMaterial color={enemyAuraColor} emissive={enemyAuraColor} emissiveIntensity={0.22} />
        </mesh>
      </group>

      <mesh position={[0, -0.72, 0]} rotation={[-Math.PI / 2, 0, 0]} receiveShadow>
        <ringGeometry args={[1.8, 3.2, 64]} />
        <meshStandardMaterial color="#1a2b3e" />
      </mesh>

      <mesh position={[0, -0.71, 0]} rotation={[-Math.PI / 2, 0, 0]}>
        <ringGeometry args={[0.95, 1.05, 64]} />
        <meshStandardMaterial color="#6bc4ff" emissive="#244d66" />
      </mesh>
    </group>
  );
}

function FloatingDamageNumbers({ impacts }: { impacts: FloatingImpact[] }) {
  return (
    <group>
      {impacts.map((impact, index) => {
        const direction = impact.actor === "player" ? 1.25 : -1.25;
        return (
          <Text
            key={impact.id}
            position={[direction, 0.9 + index * 0.16, 0.3]}
            fontSize={0.22}
            color={impact.color}
            anchorX="center"
            anchorY="middle"
          >
            {impact.damage > 0 ? `-${impact.damage}` : "MISS"}
          </Text>
        );
      })}
    </group>
  );
}

type HpBeaconProps = {
  side: "left" | "right";
  ratio: number;
  color: string;
};

function HpBeacon({ side, ratio, color }: HpBeaconProps) {
  const x = side === "left" ? -2.4 : 2.4;
  return (
    <group position={[x, -0.4, 0]}>
      <mesh position={[0, 0.55, 0]} scale={[0.22, Math.max(0.08, ratio) * 1.3, 0.22]}>
        <boxGeometry args={[1, 1, 1]} />
        <meshStandardMaterial color={color} emissive={color} emissiveIntensity={0.3} />
      </mesh>
      <mesh position={[0, -0.15, 0]}>
        <cylinderGeometry args={[0.24, 0.24, 0.14, 16]} />
        <meshStandardMaterial color="#3e4f74" />
      </mesh>
    </group>
  );
}

type BattleSceneFxProps = {
  impactFlashRef: RefObject<Mesh | null>;
  impactRingRef: RefObject<Mesh | null>;
  impactFlashMaterialRef: RefObject<MeshBasicMaterial | null>;
  impactLightRef: RefObject<PointLight | null>;
  shakeStrengthRef: MutableRefObject<number>;
  flashStrengthRef: MutableRefObject<number>;
  slashRef: RefObject<Mesh | null>;
  sparkRef: RefObject<Mesh | null>;
};

function BattleSceneFx({
  impactFlashRef,
  impactRingRef,
  impactFlashMaterialRef,
  impactLightRef,
  shakeStrengthRef,
  flashStrengthRef,
  slashRef,
  sparkRef
}: BattleSceneFxProps) {
  useFrame((state, delta) => {
    const cam = state.camera;
    const baseX = 0;
    const baseY = 1.5;
    const baseZ = 4;

    if (shakeStrengthRef.current > 0.001) {
      const jitterX = (Math.random() - 0.5) * shakeStrengthRef.current * 0.12;
      const jitterY = (Math.random() - 0.5) * shakeStrengthRef.current * 0.08;
      cam.position.set(baseX + jitterX, baseY + jitterY, baseZ);
      cam.lookAt(0, 0, 0);
      shakeStrengthRef.current = Math.max(0, shakeStrengthRef.current - delta * 1.6);
    } else {
      cam.position.set(baseX, baseY, baseZ);
      cam.lookAt(0, 0, 0);
    }

    if (impactRingRef.current) {
      const nextScale = Math.max(0.9, impactRingRef.current.scale.x - delta * 0.9);
      impactRingRef.current.scale.set(nextScale, nextScale, nextScale);
      impactRingRef.current.rotation.z += delta * 0.5;
    }

    if (slashRef.current) {
      slashRef.current.rotation.z += delta * 2.2;
      const nextScale = Math.max(0.5, slashRef.current.scale.x - delta * 1.7);
      slashRef.current.scale.set(nextScale, nextScale, 1);
    }

    if (sparkRef.current) {
      sparkRef.current.rotation.y += delta * 3.1;
      sparkRef.current.rotation.x += delta * 1.5;
      const nextScale = Math.max(0.55, sparkRef.current.scale.x - delta * 1.2);
      sparkRef.current.scale.set(nextScale, nextScale, nextScale);
    }

    if (impactFlashRef.current) {
      const targetScale = 1 + flashStrengthRef.current * 1.8;
      impactFlashRef.current.scale.set(targetScale, targetScale, 1);
    }

    if (impactFlashMaterialRef.current) {
      impactFlashMaterialRef.current.opacity = flashStrengthRef.current * 0.28;
    }

    if (flashStrengthRef.current > 0.001) {
      flashStrengthRef.current = Math.max(0, flashStrengthRef.current - delta * 2.5);
    }

    if (impactLightRef.current) {
      impactLightRef.current.intensity = flashStrengthRef.current * 4.2;
    }
  });

  return null;
}

function ArenaBackdrop({ active }: { active: boolean }) {
  const outerRingRef = useRef<Mesh>(null);
  const innerRingRef = useRef<Mesh>(null);
  const pulseRef = useRef<Mesh>(null);

  useFrame((state, delta) => {
    const t = state.clock.elapsedTime;
    if (outerRingRef.current) {
      outerRingRef.current.rotation.z += delta * 0.08;
      outerRingRef.current.rotation.x = Math.sin(t * 0.22) * 0.05;
    }
    if (innerRingRef.current) {
      innerRingRef.current.rotation.z -= delta * 0.12;
      innerRingRef.current.rotation.y = Math.cos(t * 0.19) * 0.06;
    }
    if (pulseRef.current) {
      const pulse = 1 + Math.sin(t * (active ? 2.4 : 1.4)) * (active ? 0.05 : 0.03);
      pulseRef.current.scale.set(pulse, pulse, pulse);
    }
  });

  return (
    <group>
      <Stars radius={70} depth={35} count={2400} factor={2.4} saturation={0} fade speed={active ? 0.8 : 0.35} />

      <mesh position={[0, 0.8, 0]}>
        <sphereGeometry args={[12, 48, 48]} />
        <meshStandardMaterial
          color={active ? "#142145" : "#0f1b36"}
          emissive={active ? "#1d315e" : "#152847"}
          emissiveIntensity={0.36}
          side={BackSide}
          roughness={1}
          metalness={0}
        />
      </mesh>

      <mesh ref={outerRingRef} position={[0, -0.95, -0.65]} rotation={[-Math.PI / 2, 0, 0]}>
        <ringGeometry args={[2.8, 4.2, 96]} />
        <meshStandardMaterial
          color={active ? "#5f8dff" : "#4b6ebd"}
          emissive={active ? "#8cb2ff" : "#6487cf"}
          emissiveIntensity={active ? 0.5 : 0.28}
          transparent
          opacity={0.4}
        />
      </mesh>

      <mesh ref={innerRingRef} position={[0, -0.82, -0.45]} rotation={[-Math.PI / 2, 0, 0]}>
        <ringGeometry args={[1.15, 1.85, 64]} />
        <meshStandardMaterial
          color={active ? "#56d6ff" : "#4aa0c9"}
          emissive={active ? "#7ee3ff" : "#5bb8dd"}
          emissiveIntensity={active ? 0.55 : 0.3}
          transparent
          opacity={0.45}
        />
      </mesh>

      <mesh ref={pulseRef} position={[0, 0.7, -2.6]}>
        <planeGeometry args={[6.2, 2.8]} />
        <meshBasicMaterial color={active ? "#2f417d" : "#20335d"} transparent opacity={0.25} />
      </mesh>
    </group>
  );
}

export function BattleScene({
  enemyLevel,
  enemyName,
  enemyType,
  fightStatus,
  characterHpRatio,
  enemyHpRatio,
  lastMoveActor,
  lastMoveSignature,
  lastMoveDescription,
  lastMoveElement,
  lastMoveDamage,
  autoBattleEnabled
}: BattleSceneProps) {
  const statusIsActive = autoBattleEnabled || fightStatus.toLowerCase().includes("progress");
  const palette = getElementPalette(lastMoveElement);
  const [impacts, setImpacts] = useState<FloatingImpact[]>([]);

  const impactFlashRef = useRef<Mesh>(null);
  const impactRingRef = useRef<Mesh>(null);
  const impactFlashMaterialRef = useRef<MeshBasicMaterial>(null);
  const impactLightRef = useRef<PointLight>(null);
  const slashRef = useRef<Mesh>(null);
  const sparkRef = useRef<Mesh>(null);
  const shakeStrengthRef = useRef(0);
  const flashStrengthRef = useRef(0);

  useEffect(() => {
    if (!lastMoveSignature || lastMoveSignature === "none") {
      return;
    }

    const text = lastMoveDescription.toLowerCase();
    const baseImpact = text.includes("critical") ? 0.55 : text.includes("miss") ? 0.22 : 0.38;
    shakeStrengthRef.current = baseImpact;
    flashStrengthRef.current = baseImpact;

    if (impactRingRef.current) {
      impactRingRef.current.scale.set(1.45, 1.45, 1.45);
    }
    if (slashRef.current) {
      slashRef.current.scale.set(1.2, 1.2, 1);
    }
    if (sparkRef.current) {
      sparkRef.current.scale.set(1.25, 1.25, 1.25);
    }

    setImpacts((current) => {
      const next = [
        ...current.slice(-3),
        {
          id: lastMoveSignature,
          actor: lastMoveActor,
          damage: lastMoveDamage,
          color: palette.glow
        }
      ];
      return next;
    });
  }, [lastMoveActor, lastMoveDamage, lastMoveDescription, lastMoveSignature, palette.glow]);

  useEffect(() => {
    if (impacts.length === 0) {
      return;
    }

    const timerId = window.setTimeout(() => {
      setImpacts((current) => current.slice(1));
    }, 850);

    return () => window.clearTimeout(timerId);
  }, [impacts]);

  return (
    <div className="battle-scene">
      <Canvas camera={{ position: [0, 1.5, 4], fov: 60 }}>
        <color attach="background" args={[statusIsActive ? "#101327" : "#0f192a"]} />
        <fog attach="fog" args={["#0f192a", 4, 11]} />
        <ArenaBackdrop active={statusIsActive} />
        <ambientLight intensity={0.8} />
        <directionalLight position={[2, 3, 2]} intensity={1.4} />
        <pointLight position={[0, 1, 0]} intensity={1.1} color={statusIsActive ? "#f79d65" : "#9bb7ff"} />
        <pointLight position={[-2.8, 2, 1]} intensity={0.45} color="#9ef7df" />
        <pointLight position={[2.8, 2, 1]} intensity={0.45} color="#ff9898" />
        <pointLight
          ref={impactLightRef}
          position={lastMoveActor === "player" ? [1.2, 0.6, 0.7] : [-1.2, 0.6, 0.7]}
          intensity={0}
          color={palette.glow}
        />
        <BattleSceneFx
          impactFlashRef={impactFlashRef}
          impactRingRef={impactRingRef}
          impactFlashMaterialRef={impactFlashMaterialRef}
          impactLightRef={impactLightRef}
          shakeStrengthRef={shakeStrengthRef}
          flashStrengthRef={flashStrengthRef}
          slashRef={slashRef}
          sparkRef={sparkRef}
        />
        <BattleActors
          enemyLevel={enemyLevel}
          enemyName={enemyName}
          enemyType={enemyType}
          fightStatus={fightStatus}
          characterHpRatio={characterHpRatio}
          enemyHpRatio={enemyHpRatio}
          lastMoveActor={lastMoveActor}
          lastMoveSignature={lastMoveSignature}
          lastMoveDescription={lastMoveDescription}
          lastMoveElement={lastMoveElement}
          lastMoveDamage={lastMoveDamage}
          autoBattleEnabled={autoBattleEnabled}
        />
        <FloatingDamageNumbers impacts={impacts} />
        <mesh
          ref={impactRingRef}
          position={lastMoveActor === "player" ? [1.25, 0.1, 0.02] : [-1.25, 0.1, 0.02]}
          rotation={[-Math.PI / 2, 0, 0]}
        >
          <ringGeometry args={[0.28, 0.52, 48]} />
          <meshStandardMaterial color={palette.core} emissive={palette.core} emissiveIntensity={0.85} />
        </mesh>
        <mesh ref={slashRef} position={lastMoveActor === "player" ? [1.28, 0.18, 0.12] : [-1.28, 0.18, 0.12]}>
          <planeGeometry args={[0.95, 0.18]} />
          <meshBasicMaterial color={palette.core} transparent opacity={0.42} />
        </mesh>
        <mesh ref={sparkRef} position={lastMoveActor === "player" ? [1.28, 0.22, 0.18] : [-1.28, 0.22, 0.18]}>
          <octahedronGeometry args={[0.22, 0]} />
          <meshStandardMaterial color={palette.glow} emissive={palette.core} emissiveIntensity={1.2} />
        </mesh>
        <mesh ref={impactFlashRef} position={[0, 0.25, -0.3]}>
          <planeGeometry args={[5.6, 3.4]} />
          <meshBasicMaterial ref={impactFlashMaterialRef} color={palette.glow} transparent opacity={0} />
        </mesh>
        <HpBeacon side="left" ratio={characterHpRatio} color="#72f2c8" />
        <HpBeacon side="right" ratio={enemyHpRatio} color="#ff8b8b" />
        <gridHelper args={[10, 20, "#779", "#334"]} />
        <OrbitControls enablePan={false} minDistance={3.2} maxDistance={6.5} />
      </Canvas>
    </div>
  );
}
