# RPG Client (React + Three.js)

Frontend workspace for the RPG API using React, TypeScript, Vite, and React Three Fiber.

## Stack

- React + TypeScript + Vite
- Three.js via @react-three/fiber and @react-three/drei
- Zustand for client state
- Axios for API calls
- React Router for routing

## Prerequisites

- Node.js 20+ (npm included)

## Setup

1. Copy environment template:
   cp .env.example .env
2. Install dependencies:
   npm install
3. Start dev server:
   npm run dev

The app defaults to API base URL http://localhost:5000. If your .NET API runs on another port, update .env.

## Scripts

- npm run dev
- npm run build
- npm run preview
- npm run lint

## Initial Routes

- / : Home page
- /battle : 3D battle sandbox and enemy selector

## Next Integration Steps

1. Add typed API models for players, characters, fights, and skills.
2. Replace placeholder 3D dummy with character/enemy meshes or glTF assets.
3. Move combat turn logic to dedicated feature modules.
4. Add websocket support for real-time multiplayer sessions.
