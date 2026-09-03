# Techbodia Notes

A full-stack notes application: Vue 3 + TypeScript + Tailwind CSS frontend,
ASP.NET Core 8 Web API backend (Dapper + SQL Server), with JWT-based
authentication and per-user data isolation.

## Features

- Register / sign in with email + password (BCrypt-hashed, JWT sessions)
- Create, view, edit, and delete notes — click anywhere on a note card to
  open it for editing
- Pin notes to keep them at the top of the list
- Live search across title and content
- Sort by newest, oldest, or alphabetical
- Each note shows its created date and last-edited date
- Users can only ever see and modify their own notes — enforced at the
  database query level, not just in the UI
- Responsive, mobile-first layout

## Tech stack

| Layer | Technology |
|---|---|
| Frontend | Vue 3 (`<script setup>`), TypeScript, Tailwind CSS, Axios |
| Backend | C# / .NET 8, ASP.NET Core Web API |
| Data access | Dapper (raw parameterized SQL, no ORM) |
| Database | SQL Server |
| Auth | JWT Bearer tokens, BCrypt password hashing |

## Structure

```
├── backend/
│   ├── init.sql                       # SQL Server (T-SQL) schema
│   └── NotesApp.Api/
│       ├── Controllers/               # AuthController, NotesController
│       ├── Data/                      # DbConnectionFactory (SqlClient)
│       ├── Middleware/                # Global exception handling
│       ├── Models/                    # User, Note, DTOs/requests
│       ├── Repositories/              # Dapper raw-SQL data access
│       ├── Services/                  # AuthService (JWT + BCrypt)
│       ├── Program.cs
│       ├── appsettings.json
│       └── NotesApp.Api.csproj
└── frontend/
    ├── src/
    │   ├── api/                       # axiosInstance, auth.ts, notes.ts
    │   ├── components/                # LoginRegister.vue, NotesDashboard.vue
    │   ├── types/                     # Shared TS interfaces
    │   ├── App.vue
    │   ├── main.ts
    │   └── style.css
    ├── index.html
    ├── package.json
    ├── vite.config.ts
    ├── tailwind.config.js
    ├── postcss.config.js
    └── tsconfig.json
```

## Getting started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Express edition works fine locally)
- [Node.js](https://nodejs.org/) (includes npm)

### 1. Create the database and run the schema

Using `sqlcmd`:
```bash
sqlcmd -S localhost -U sa -P YOUR_PASSWORD -Q "CREATE DATABASE techbodia"
sqlcmd -S localhost -U sa -P YOUR_PASSWORD -d techbodia -i backend/init.sql
```

Or with SSMS / Azure Data Studio / the VS Code `mssql` extension: create a
`techbodia` database, open `backend/init.sql`, and execute it against that
database.

### 2. Configure the backend

Edit `backend/NotesApp.Api/appsettings.json` directly for local testing, or
use `dotnet user-secrets` (recommended, keeps secrets out of source control):

```bash
cd backend/NotesApp.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:SqlServer" "Server=localhost;Database=techbodia;Integrated Security=True;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Secret" "some-long-random-32-plus-byte-string"
```

If your SQL Server instance uses SQL Authentication instead of Windows
Authentication, use `User Id=...;Password=...` in place of
`Integrated Security=True`.

### 3. Run the backend

```bash
dotnet restore
dotnet run
```

The API listens on the URL printed in the console (typically
`http://localhost:5000`). Swagger UI is available at `/swagger` in
Development.

### 4. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

Open `http://localhost:5173` in your browser. Optionally point the frontend
at a non-default API URL:
```bash
echo "VITE_API_BASE_URL=http://localhost:5000/api" > .env.local
```

## Security notes

- Passwords are hashed with BCrypt (work factor 12) — never stored or logged
  in plaintext.
- Every note query in `NoteRepository` filters explicitly by
  `user_id = @UserId`, where `@UserId` is derived only from the validated
  JWT `sub` claim in `NotesController` — never from client input — so one
  user can never read or modify another user's notes.
- JWTs are signed with HMAC-SHA256; issuer, audience, lifetime, and signing
  key are all validated on every request.
- Unhandled exceptions are caught by `ExceptionHandlingMiddleware` and
  returned as `ProblemDetails` JSON — stack traces are logged server-side
  only, never sent to the client.
