# RealEstateManagement

ASP.NET Core MVC application for managing and publishing rental and sale properties.

## Tech Stack

- .NET 10
- ASP.NET Core MVC with Razor Views
- ASP.NET Core Areas for Admin UI
- ASP.NET Core Identity
- Entity Framework Core
- SQLite
- Tailwind CSS and the existing JavaScript pipeline
- xUnit

## Main Routes

Public:

- `/`
- `/properties`
- `/properties/{id}`
- `/sales`
- `/sales/{id}`
- `/contact`

Admin:

- `/admin/login`
- `/admin/dashboard`
- `/admin/properties`
- `/admin/landlord-contracts`
- `/admin/tenant-contracts`
- `/admin/leads`
- `/admin/staff`

## Development

Restore, build, and test:

```powershell
dotnet restore
dotnet build RealEstateManagement.slnx --no-restore
dotnet test RealEstateManagement.slnx --no-build
```

Run the web app:

```powershell
dotnet run --project src/RealEstateManagement.Web
```

Apply migrations:

```powershell
dotnet ef database update `
  --project src/RealEstateManagement.Infrastructure `
  --startup-project src/RealEstateManagement.Web
```

Seed development data:

```powershell
dotnet run --project src/RealEstateManagement.Web -- --seed-development-data
```

Development accounts are seeded only in `Development`:

- Admin: `admin@anphurealestate.local`
- Sale:
  - `sale.tham@anphurealestate.local`
  - `sale.thuy@anphurealestate.local`
  - `sale.tuan@anphurealestate.local`
  - `sale.linh@anphurealestate.local`
  - `sale.huy@anphurealestate.local`

The development password is configured in `appsettings.Development.json`.

## Notes

- SQLite remains the local database.
- Existing migration history is preserved.
- Warning `NU1903` for `SQLitePCLRaw.lib.e_sqlite3` is known and should be handled in a separate package-upgrade task.
