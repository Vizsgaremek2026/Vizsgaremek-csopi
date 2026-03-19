# Music Finder ♪

Full-stack music application with a React/Vite frontend, ASP.NET Core Web API backend, and a MySQL database (XAMPP).

---

## Prerequisites

| Tool | Version |
|---|---|
| [XAMPP](https://www.apachefriends.org/) | Any recent (MySQL 8 / MariaDB 10.4+) |
| [.NET SDK](https://dotnet.microsoft.com/) | 8.0+ |
| [Node.js](https://nodejs.org/) | 18+ |
| npm | 9+ |

---

## 1. Database setup (XAMPP)

1. **Start XAMPP** → start **Apache** and **MySQL**.
2. Open **phpMyAdmin**: `http://localhost/phpmyadmin`
3. Create a new database named **`music_finder`**.
4. Select the database and click **Import**.
5. Choose `music_finder (1).sql` from the repo root and click **Go**.

> By default XAMPP uses `root` with an empty password on port `3306`.  
> If you have a password set, update the connection string in the next step.

---

## 2. Backend (MusicFinder.Api)

### Configure the connection string

Edit `MusicFinder.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "MusicFinderDb": "Server=127.0.0.1;Port=3306;Database=music_finder;User=root;Password=;SslMode=None;"
  }
}
```

Replace `Password=` with your MySQL root password if you have one set.

### Run the API

```bash
cd MusicFinder.Api
dotnet run
```

The API will start (by default) on `http://localhost:5000`.  
Swagger UI is available at `http://localhost:5000/swagger`.

---

## 3. Frontend (React / Vite)

### Configure the API URL

Copy `.env.example` to `.env`:

```bash
cp .env.example .env
```

Edit `.env` to point to the running backend:

```env
VITE_API_BASE_URL=http://localhost:5000
```

### Install dependencies

```bash
npm install
```

### Run the dev server

```bash
npm run dev
```

The frontend will start on `http://localhost:5173`.

---

## 4. Admin desktop app (AdminApp)

The AdminApp is a WPF (.NET Framework 4.7.2) application.  
It reads a `logs.txt` file written by the backend (optional integration) and displays log entries.

**To build:**
1. Open `AdminApp.sln` in **Visual Studio 2019+** (Windows only).
2. Select **AdminApp** project and build.
3. Run from `AdminApp/bin/Debug/AdminApp.exe`.

> Note: The WPF app requires Windows and the .NET Framework 4.7.2 runtime.  
> It does **not** directly connect to MySQL; it reads the shared `logs.txt` file.

---

## 5. API endpoints reference

### Auth
| Method | Endpoint | Body |
|--------|----------|------|
| `POST` | `/api/auth/register` | `{ "email": "...", "password": "..." }` |
| `POST` | `/api/auth/login` | `{ "email": "...", "password": "..." }` |

All other endpoints require the `X-User-Email: <email>` header.

### Library
| Method | Endpoint | Body |
|--------|----------|------|
| `GET` | `/api/library` | — |
| `POST` | `/api/library` | `{ "youtubeVideoId": "...", "title": "...", "artist": "...", "artworkUrl": "..." }` |
| `DELETE` | `/api/library/{songId}` | — |

### Favorites
| Method | Endpoint |
|--------|----------|
| `GET` | `/api/favorites` |
| `POST` | `/api/favorites/{songId}` |
| `DELETE` | `/api/favorites/{songId}` |

### Resume times
| Method | Endpoint | Body |
|--------|----------|------|
| `GET` | `/api/resume` | — |
| `PUT` | `/api/resume/{songId}` | `{ "positionSeconds": 42 }` |

### Search history
| Method | Endpoint | Body |
|--------|----------|------|
| `POST` | `/api/search-history` | `{ "query": "..." }` |

---

## 6. Database schema

See `music_finder (1).sql` for the full schema. Tables:

- `users` – registered users (bcrypt-hashed passwords)
- `songs` – YouTube video metadata
- `user_library` – per-user saved songs
- `user_favorites` – per-user favorites
- `user_resume` – per-user playback positions
- `user_search_history` – per-user search queries

---

## 7. Running tests

```bash
npm test
```
