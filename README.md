# Bowling Game API

A production-grade REST API for managing bowling games, built with .NET 9 and MySQL.

## Overview

This API allows users to create bowling games, record rolls, calculate scores following standard bowling rules, and view a leaderboard of top scorers.

## Features

- **Start a Game** – Create a new game for a player
- **Record Rolls** – Submit pins knocked down per roll with full bowling rule validation
- **Get Score** – Retrieve current/final score with frame-by-frame breakdown
- **Leaderboard** – View top completed games ranked by score

## Tech Stack

- **.NET 10** – ASP.NET Core Web API
- **MySQL** – Data persistence
- **ADO.NET** – Raw data access (no ORM)
- **Swagger/OpenAPI** – API documentation

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/games` | Start a new game |
| POST | `/api/games/{id}/roll` | Record a roll |
| GET | `/api/games/{id}/score` | Get game score |
| GET | `/api/games/leaderboard` | Get top scorers |

## Architecture

- **Controllers** – HTTP request handling
- **Services** – Business logic & scoring calculations
- **Repositories** – Data access layer
- **Middleware** – Global exception handling with custom exception mappers
- **DTOs** – Request/Response models

## Running the Project

1. Set up MySQL and run `Database/CreateSchema.sql`
2. Update connection string in `appsettings.json`
3. Run with `dotnet run`
4. Access Swagger UI at `http://localhost:<port>/`

## Testing

Unit tests are located in the `Tests/` folder covering:
- Score calculation logic
- Game state management
- Input validation

Run tests: `dotnet test`

---

## Out of Scope (Future Considerations)

The following features were considered during development but intentionally left out of scope for this assignment:

### 🔐 Authentication & Authorization
- Currently, any user can create games or post rolls for any player
- **Future:** Add authentication to ensure players can only modify their own games

### 👥 Multi-Player Sessions (Lane/Group)
- Each game is standalone – one player per game
- **Future:** Support group sessions where multiple players play together on the same lane, taking turns

### 📊 Game History & Player Profiles
- No persistent player profiles or game history lookup
- **Future:** Player accounts with full game history and statistics

### 🔄 Real-Time Updates
- API is request/response only
- **Future:** WebSocket support for live score updates during group play

### 🗑️ Game Deletion / Cancellation
- Games cannot be deleted or cancelled once started
- **Future:** Allow players to abandon or delete their games

### 📈 Analytics & Statistics
- No player averages, trends, or performance analytics
- **Future:** Track averages, strike/spare percentages, improvement trends

### 🌐 Rate Limiting
- No rate limiting on API endpoints
- **Future:** Add throttling to prevent abuse

---

*Built as part of an interview assignment.*
