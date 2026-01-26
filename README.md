# OLX Backend API

Backend API untuk aplikasi marketplace OLX Clone menggunakan ASP.NET Core 9.0.

## Tech Stack

- **Framework**: ASP.NET Core 9.0
- **Database**: PostgreSQL (Entity Framework Core)
- **Authentication**: JWT Bearer Token
- **Object Storage**: ImageKit
- **Payment Gateway**: Midtrans
- **Email**: SMTP (Gmail)
- **Maps**: Google Maps API
- **Real-time**: SignalR (Chat Hub)

## Quick Start

### 1. Prerequisites

- .NET 9.0 SDK
- PostgreSQL 12+
- Akun ImageKit (gratis)
- Akun Midtrans Sandbox (gratis)
- Gmail App Password

### 2. Clone Repository

```bash
git clone <repository-url>
cd olx-be-api
```

### 3. Setup Environment Variables

```bash
cp .env.example .env
```

Edit file `.env` dan isi semua credentials.

### 4. Restore Packages

```bash
dotnet restore
```

### 5. Setup Database

```bash
dotnet ef database update
```

### 6. Run Application

```bash
dotnet run
```

API akan berjalan di `https://localhost:8080`

## Documentation

- **Swagger UI** - Tersedia di `/swagger` saat aplikasi running

## API Endpoints

Setelah aplikasi berjalan, buka Swagger UI untuk melihat semua endpoints:

```
https://localhost:8080/swagger
```

### Main Features

- **Auth**: Register, Login, JWT Authentication
- **Products**: CRUD products dengan upload gambar
- **Profile**: Update user profile dan avatar
- **Chat**: Real-time chat menggunakan SignalR
- **Payment**: Integrasi Midtrans payment gateway
- **Maps**: Geocoding menggunakan Google Maps API

## Environment Variables

Lihat file `.env.example` untuk template dan **ENV_SETUP.md** untuk panduan lengkap.

### Required Variables

- `ConnectionStrings__DefaultConnection` - PostgreSQL connection string
- `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience` - JWT configuration
- `ImageKit__PrivateKey`, `ImageKit__PublicKey`, `ImageKit__UrlEndpoint` - Object storage
- `SmtpSettings__*` - Email configuration
- `GoogleMaps__ApiKey` - Maps API
- `Midtrans__*` - Payment gateway

## Project Structure

```
olx-be-api/
├── Controllers/        # API Controllers
├── Services/          # Business logic & external services
├── Models/            # Database models
├── DTO/              # Data Transfer Objects
├── Helpers/          # Utility helpers
├── Middleware/       # Custom middleware
├── Hubs/            # SignalR hubs
├── Data/            # DbContext dan seeding
├── Migrations/      # EF Core migrations
└── Program.cs       # Application entry point
```

## Development

### Run in Development Mode

```bash
dotnet run --environment Development
```

### Database Migrations

```bash
# Add new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Rollback migration
dotnet ef database update PreviousMigrationName
```

### Seed Database

```bash
dotnet run seed
```

## Deployment

### Docker

```bash
docker build -t olx-be-api .
docker run -p 8080:8080 --env-file .env olx-be-api
```

## Support

Untuk pertanyaan atau issue, silakan hubungi saya melalui instagram/linkedin.
