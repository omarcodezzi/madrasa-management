# মাদরাসা ম্যানেজমেন্ট সিস্টেম

Angular + .NET 8 + Microsoft SQL Server. Built from [`docs/UI_SPEC.md`](docs/UI_SPEC.md); the approved look is [`docs/prototype.html`](docs/prototype.html).

| Folder | What |
|---|---|
| `backend/` | .NET 8 Web API, clean architecture: `Domain` → `Application` → `Infrastructure` (EF Core, SQL Server, Identity) → `Api` |
| `frontend/` | Angular 22 app: Tailwind CSS tokens from the prototype, Bangla/English (Transloco), shared UI library |
| `database/` | `MMS.sql` — full script for the **MMS** database (tables, seed data, views, stored procedures); see [`docs/DATABASE.md`](docs/DATABASE.md) |
| `docs/` | Spec, prototype, Phase 1 plan (table → screen map), screenshots |

## Run locally

**1. Database + API (Docker)**
```bash
cp .env.example .env        # then edit the values
docker compose up -d --build
```
The API applies migrations on start, creates 9 roles with permissions and the first admin (`admin` / `ADMIN_PASSWORD`).
In Development it also adds sample classes, 18 students and a notice. Swagger: http://localhost:5080/swagger

**Without Docker (Visual Studio / `dotnet run`):** `appsettings.Development.json` already has a dev JWT key, admin password `Admin12345` and a Windows-auth connection string to LocalDB `(localdb)\MSSQLLocalDB` (installed with Visual Studio), database `MMS`. Edit it if you use `.\SQLEXPRESS` or a sa password. Then
```bash
cd backend && dotnet run --project src/Madrasa.Api
```

**2. Frontend**
```bash
cd frontend && npm install && npm start     # http://localhost:4200
```
Sign in with `admin` and the password you set. The design-system page is at `/design-system`.

## Production
Docker Compose with SQL Server, the API, Caddy (automatic HTTPS) and nightly backups: see [`docs/DEPLOY.md`](docs/DEPLOY.md) (Bangla).
```bash
cd deploy && ./setup.sh && docker compose up -d --build
```

## Tests
```bash
cd backend && dotnet test          # grading (Befaq division) rules
cd frontend && npm test            # number / money formatting
```

## Database changes
```bash
cd backend
dotnet tool restore
dotnet ef migrations add <Name> -p src/Madrasa.Infrastructure -s src/Madrasa.Infrastructure -o Persistence/Migrations
cd .. && bash database/build.sh   # regenerates database/schema.sql and database/MMS.sql
```

## Phases
1. ✅ Analysis: schema + table → screen map ([docs/PHASE1_PLAN.md](docs/PHASE1_PLAN.md))
2. ✅ Design system, i18n, base components, `/design-system` page; backend foundation (entities, migrations, login)
3. ✅ App shell: login, sidebar, top bar, Ctrl+K search, phone layout
4. Dashboard → Students → Attendance → Fees
5. Classes/Routine → Hifz → Exams & Results
6. Teachers, Hostel, Library, Notices, Reports, Settings, Parent portal, public site
7. Polish: responsive, dark mode, print templates, accessibility
