# RPG API - C# .NET Web API

A RESTful API for managing RPG players and their characters. Built with ASP.NET Core 8.0 and Entity Framework Core.

## Features

- Full CRUD operations for Players
- Full CRUD operations for Characters
- Player-Character relationships with cascade delete
- Unique constraints on username and email
- Comprehensive logging
- Swagger/OpenAPI documentation
- CORS enabled for all origins
- Entity Framework Core with SQL Server support

## Prerequisites

- .NET 8.0 SDK or later
- SQL Server (LocalDB for development)
- Visual Studio Code or Visual Studio

## Project Structure

```
RpgApi/
├── Models/
│   ├── Player.cs          # Player entity model
│   └── Character.cs       # Character entity model
├── Data/
│   └── RpgContext.cs      # Entity Framework DbContext
├── Controllers/
│   ├── PlayersController.cs      # Player CRUD endpoints
│   └── CharactersController.cs   # Character CRUD endpoints
├── Program.cs             # Application startup configuration
├── appsettings.json       # Configuration settings
└── RpgApi.csproj          # Project file
```

## Installation & Setup

### 1. Install Dependencies

```bash
cd /workspaces/Test
dotnet restore
```

### 2. Create Database

The database is automatically created on first run using `EnsureCreated()`. For migrations, use:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 3. Run the Application

```bash
dotnet run
```

The API will be available at:
- API: `http://localhost:5000`
- Swagger UI: `http://localhost:5000/swagger`

## API Endpoints

### Players Endpoints

#### Get All Players
```
GET /api/players
```

Response:
```json
[
  {
    "id": 1,
    "username": "player1",
    "email": "player1@example.com",
    "createdAt": "2026-03-20T10:30:00Z",
    "updatedAt": null,
    "characters": []
  }
]
```

#### Get Player by ID
```
GET /api/players/{id}
```

#### Create Player
```
POST /api/players

Request Body:
{
  "username": "newplayer",
  "email": "newplayer@example.com"
}
```

#### Update Player
```
PUT /api/players/{id}

Request Body:
{
  "username": "updatedplayer",
  "email": "updated@example.com"
}
```

#### Delete Player
```
DELETE /api/players/{id}
```

---

### Characters Endpoints

#### Get All Characters for a Player
```
GET /api/players/{playerId}/characters
```

Response:
```json
[
  {
    "id": 1,
    "name": "Aragorn",
    "class": "Warrior",
    "level": 10,
    "health": 150,
    "mana": 30,
    "experience": 5000,
    "description": "Brave warrior",
    "createdAt": "2026-03-20T10:30:00Z",
    "updatedAt": null,
    "playerId": 1
  }
]
```

#### Get Character by ID
```
GET /api/players/{playerId}/characters/{characterId}
```

#### Create Character
```
POST /api/players/{playerId}/characters

Request Body:
{
  "name": "Legolas",
  "class": "Archer",
  "level": 8,
  "health": 100,
  "mana": 60,
  "experience": 3000,
  "description": "Skilled archer"
}
```

Note: `level`, `health`, `mana`, and `experience` are optional with default values:
- `level`: 1
- `health`: 100
- `mana`: 50
- `experience`: 0

#### Update Character
```
PUT /api/players/{playerId}/characters/{characterId}

Request Body:
{
  "name": "Legolas",
  "class": "Archer",
  "level": 12,
  "health": 120,
  "mana": 70,
  "experience": 8000,
  "description": "More experienced archer"
}
```

#### Delete Character
```
DELETE /api/players/{playerId}/characters/{characterId}
```

## Database Schema

### Players Table
- `Id` (INT, Primary Key)
- `Username` (NVARCHAR(MAX), Unique)
- `Email` (NVARCHAR(MAX), Unique)
- `CreatedAt` (DATETIME2)
- `UpdatedAt` (DATETIME2, Nullable)

### Characters Table
- `Id` (INT, Primary Key)
- `Name` (NVARCHAR(MAX))
- `Class` (NVARCHAR(MAX))
- `Level` (INT)
- `Health` (INT)
- `Mana` (INT)
- `Experience` (INT)
- `Description` (NVARCHAR(MAX), Nullable)
- `CreatedAt` (DATETIME2)
- `UpdatedAt` (DATETIME2, Nullable)
- `PlayerId` (INT, Foreign Key)

## Error Handling

All endpoints return appropriate HTTP status codes:

- `200 OK` - Successful GET request
- `201 Created` - Successful POST request
- `204 No Content` - Successful PUT/DELETE request
- `400 Bad Request` - Invalid input (e.g., duplicate username)
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

Error responses include a message:
```json
{
  "message": "Username already exists"
}
```

## Example Usage

### Create a Player
```bash
curl -X POST http://localhost:5000/api/players \
  -H "Content-Type: application/json" \
  -d '{"username":"hero1","email":"hero1@example.com"}'
```

### Create a Character for a Player
```bash
curl -X POST http://localhost:5000/api/players/1/characters \
  -H "Content-Type: application/json" \
  -d '{
    "name":"Gandalf",
    "class":"Wizard",
    "level":15,
    "health":80,
    "mana":200,
    "experience":10000,
    "description":"A wise wizard"
  }'
```

### Get All Characters for a Player
```bash
curl http://localhost:5000/api/players/1/characters
```

### Update a Character
```bash
curl -X PUT http://localhost:5000/api/players/1/characters/1 \
  -H "Content-Type: application/json" \
  -d '{
    "name":"Gandalf",
    "class":"Wizard",
    "level":20,
    "health":90,
    "mana":250,
    "experience":20000,
    "description":"An ever-wiser wizard"
  }'
```

### Delete a Character
```bash
curl -X DELETE http://localhost:5000/api/players/1/characters/1
```

## Configuration

### Connection Strings

Update `appsettings.json` to use different databases:

**SQL Server (Default):**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=RpgDatabase;Trusted_Connection=true;"
}
```

**SQL Server (Production):**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=your-server;Database=RpgDatabase;User Id=sa;Password=your-password;"
}
```

## Logging

The application includes comprehensive logging. Configure logging in `appsettings.json`:

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning"
  }
}
```

## Future Enhancements

- [ ] Authentication and Authorization (JWT)
- [ ] Character equipment and inventory system
- [ ] Combat/leveling system
- [ ] Data validation with FluentValidation
- [ ] Unit and integration tests
- [ ] API rate limiting
- [ ] Advanced filtering and pagination

## License

MIT License
