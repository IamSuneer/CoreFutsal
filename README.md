# CoreFutsal API

A backend system for managing futsal — teams, players, stadiums, and matches — built with ASP.NET Core 10.

---

## What is this?

CoreFutsal is a server-side application (an API) that powers a futsal management platform. Think of it as the engine behind a futsal app — it handles everything from player registrations and team formation to stadium bookings and live match scoring.

A mobile or web app would connect to this API to display data to users. The API itself has no visual interface; it communicates through structured data (JSON).

---

## Who uses it and what can they do?

The system has four types of users, each with different permissions:

| Role | Who they are | What they can do |
|---|---|---|
| **Player** | A futsal player | Register, build a profile, browse teams, apply to join or accept invites |
| **Staff** | Coach, manager, physio, etc. | Register, get assigned to teams by team owners |
| **Team Owner** | Runs a futsal team | Create and manage a team, recruit players and staff, request matches, book stadiums |
| **Stadium Owner** | Owns a futsal venue | List their stadium, set time slots and prices, approve bookings, run matches |

---

## How does it work? — Key Flows

### Signing up and getting in
1. A user registers with their details and chooses a role.
2. They receive a verification email and confirm their account.
3. On login they get two tokens — a short-lived access token (30 min) and a longer refresh token (7 days). The app uses these to stay logged in securely without re-entering a password.

### Building a team
1. A Team Owner creates a team.
2. They browse the **player marketplace** — a list of all players who are currently available and looking for a team.
3. They send an invite. The player gets notified and can accept or decline.
4. Alternatively, a player can browse teams and apply directly. The team owner then decides.
5. Once a player joins a team, they are no longer listed in the marketplace.

### Booking a stadium
1. A Stadium Owner lists their venue and sets available time slots (e.g. Saturday 10am–12pm at NPR 1,200/hr).
2. A Team Owner browses available slots and books one directly, or the slot gets reserved automatically when a match is accepted.
3. The total cost is calculated from the slot duration and price.

### Scheduling a match
1. A Team Owner sends a **match request** to a rival team, picking a stadium slot.
2. The rival team's owner accepts or declines.
3. On acceptance, the slot is automatically booked and a match is scheduled.

### Playing a match
1. The Stadium Owner starts the match when teams arrive.
2. During the match, goals, yellow cards, red cards, and substitutions are recorded in real time.
3. Own goals are automatically credited to the opposing team.
4. When the Stadium Owner ends the match, the final score is calculated from the recorded events.
5. Player stats (goals, assists, cards, minutes played) are saved and contribute to each player's career record.

---

## Tech Stack

| | |
|---|---|
| Framework | ASP.NET Core 10 |
| Database | SQL Server |
| ORM | Entity Framework Core 10 |
| Authentication | JWT tokens + refresh token rotation |
| CI/CD | GitHub Actions |

---

## Security

- Passwords are hashed — they are never stored in plain text.
- Login attempts are limited to 10 per minute per IP to prevent brute-force attacks.
- Access tokens expire after 30 minutes. Refresh tokens rotate on every use so a stolen token can't be reused.
- Sensitive config (JWT secret, database password) is never stored in code — loaded from environment variables at runtime.

---

## Getting Started (for developers)

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10)
- SQL Server

### Setup

```bash
git clone https://github.com/your-username/CoreFutsal.git
cd CoreFutsal/CoreFutsal

# Set secrets (never put these in appsettings.json)
dotnet user-secrets set "ConnectionStrings:futsalConn" "Server=.;Database=FutsalDatabase;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Key" "your-secret-key-minimum-32-characters"

dotnet run
```

The database is created automatically on first run. API docs (Swagger UI) open at `https://localhost:{port}/swagger`.

---

## API Overview

### Auth — `/api/auth`

| Method | Endpoint | Description |
|---|---|---|
| POST | `/register` | Create an account |
| POST | `/login` | Log in, receive tokens |
| POST | `/refresh` | Get a new access token |
| POST | `/logout` | Sign out |
| POST | `/verify-email` | Confirm email address |

### Players — `/api/players`

| Method | Endpoint | Description |
|---|---|---|
| GET | `/marketplace` | Browse available players |
| GET | `/{id}` | View a player's profile |
| PUT | `/` | Update own profile |
| DELETE | `/` | Delete own account |

### Teams — `/api/teams`

| Method | Endpoint | Description |
|---|---|---|
| GET | `/` | Browse all active teams |
| GET | `/{id}` | View team details and roster |
| POST | `/` | Create a team |
| PUT | `/{id}` | Update team info |
| DELETE | `/{id}` | Disband a team |
| POST | `/{id}/captain` | Assign team captain |
| PUT | `/{id}/jersey` | Set a player's jersey number |

### Marketplace — `/api/marketplace`

| Method | Endpoint | Description |
|---|---|---|
| POST | `/invite` | Team owner invites a player |
| POST | `/apply` | Player applies to a team |
| POST | `/respond/{requestId}` | Accept or decline a request |

### Staff — `/api/staff`

| Method | Endpoint | Description |
|---|---|---|
| POST | `/` | Add staff to a team |
| GET | `/team/{teamId}` | List a team's staff |
| DELETE | `/{staffId}` | Remove a staff member |

### Stadiums — `/api/stadiums`

| Method | Endpoint | Description |
|---|---|---|
| GET | `/` | Browse all stadiums |
| GET | `/{id}` | View stadium details |
| POST | `/` | List a new stadium |
| PUT | `/{id}` | Update stadium info |
| DELETE | `/{id}` | Remove a stadium listing |
| GET | `/{id}/slots` | View available time slots |
| POST | `/{id}/slots` | Add a time slot |
| POST | `/{id}/book` | Book a slot directly |

### Matches — `/api/matches`

| Method | Endpoint | Description |
|---|---|---|
| POST | `/request` | Send a match request to a team |
| POST | `/respond/{requestId}` | Accept or decline a match request |
| POST | `/{id}/start` | Start the match |
| POST | `/{id}/events` | Record an event (goal, card, sub) |
| POST | `/{id}/end` | End the match, compute final score |
| GET | `/{id}` | Get match details and score |

---

## Project Structure

```
CoreFutsal/
├── Controllers/    — receives requests, sends responses
├── Services/       — all business logic lives here
├── Models/         — database entities (Team, Player, Match, etc.)
├── DTOs/           — data shapes sent to/from the API
├── DAL/            — database connection and configuration
├── Middleware/     — global error handling
├── Migrations/     — database version history
└── Enums/          — fixed value sets (roles, statuses, event types)
```

---

## License

MIT
