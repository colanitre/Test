# Quick Start Guide - RPG API

## Option 1: Run Locally (Recommended for Development)

### Prerequisites
- .NET 8.0 SDK installed
- SQL Server LocalDB or SQL Server instance

### Steps

1. **Navigate to the project directory:**
   ```bash
   cd /workspaces/Test
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Run the application:**
   ```bash
   dotnet run
   ```

4. **Open Swagger UI:**
   - Open your browser and navigate to: `http://localhost:5000/swagger`
   - You can test all API endpoints directly from the Swagger UI

5. **Test endpoints (using curl or Postman):**

   **Create a Player:**
   ```bash
   curl -X POST http://localhost:5000/api/players \
     -H "Content-Type: application/json" \
     -d '{"username":"TestPlayer","email":"test@example.com"}'
   ```

   **Get all Players:**
   ```bash
   curl http://localhost:5000/api/players
   ```

   **Create a Character:**
   ```bash
   curl -X POST http://localhost:5000/api/players/1/characters \
     -H "Content-Type: application/json" \
     -d '{"name":"Warrior","class":"Fighter","level":5,"health":150,"mana":20}'
   ```

---

## Option 2: Run with Docker Compose

### Prerequisites
- Docker and Docker Compose installed

### Steps

1. **Navigate to the project directory:**
   ```bash
   cd /workspaces/Test
   ```

2. **Start the services:**
   ```bash
   docker-compose up -d
   ```

3. **Access the API:**
   - Swagger UI: `http://localhost:5000/swagger`
   - API: `http://localhost:5000`

4. **View logs:**
   ```bash
   docker-compose logs -f api
   ```

5. **Stop the services:**
   ```bash
   docker-compose down
   ```

---

## Key Features

✅ **Full CRUD Operations** - Create, Read, Update, Delete for Players and Characters  
✅ **One-to-Many Relationship** - Each player can have multiple characters  
✅ **Cascading Deletes** - Deleting a player automatically deletes their characters  
✅ **Validation** - Prevents duplicate usernames and emails  
✅ **Swagger/OpenAPI** - Interactive API documentation and testing  
✅ **Comprehensive Logging** - Built-in logging for debugging  
✅ **CORS Enabled** - Ready for frontend integration  

---

## Database Schema

### Players
- `Id` (int, PK)
- `Username` (string, unique)
- `Email` (string, unique)
- `CreatedAt` (datetime)
- `UpdatedAt` (datetime, nullable)

### Characters
- `Id` (int, PK)
- `Name` (string)
- `Class` (string)
- `Level` (int)
- `Health` (int)
- `Mana` (int)
- `Experience` (int)
- `Description` (string, nullable)
- `CreatedAt` (datetime)
- `UpdatedAt` (datetime, nullable)
- `PlayerId` (int, FK)

---

## API Endpoints Summary

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/players` | Get all players |
| GET | `/api/players/{id}` | Get player by ID |
| POST | `/api/players` | Create new player |
| PUT | `/api/players/{id}` | Update player |
| DELETE | `/api/players/{id}` | Delete player |
| GET | `/api/players/{playerId}/characters` | Get all characters |
| GET | `/api/players/{playerId}/characters/{id}` | Get character by ID |
| POST | `/api/players/{playerId}/characters` | Create character |
| PUT | `/api/players/{playerId}/characters/{id}` | Update character |
| DELETE | `/api/players/{playerId}/characters/{id}` | Delete character |

---

## Troubleshooting

### Database Connection Issues
- Ensure SQL Server is running (LocalDB or full instance)
- Check connection string in `appsettings.json`
- Database is auto-created on first run

### Port Already in Use
- Change the port in `launchSettings.json` (default: 5000)
- Or use: `dotnet run --urls http://localhost:5001`

### Docker Issues
- Ensure Docker daemon is running
- Check logs: `docker-compose logs -f`
- Rebuild images: `docker-compose up --build`

---

## Next Steps

1. Read the full [API_DOCUMENTATION.md](API_DOCUMENTATION.md) for detailed endpoint information
2. Import the Swagger specs into Postman for testing
3. Add authentication (JWT) and authorization as needed
4. Extend with additional features (inventories, guilds, etc.)
5. Add unit tests using xUnit or NUnit

## Support

For detailed API documentation, see [API_DOCUMENTATION.md](API_DOCUMENTATION.md)
