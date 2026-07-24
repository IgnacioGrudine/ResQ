# ResQ

**Rescatá comida. Ahorrá dinero. Cuidá el planeta.**

ResQ es una plataforma web (B2B + B2C, en la línea de *Too Good To Go*) que conecta comercios gastronómicos de Córdoba (Argentina) con consumidores que quieren comprar los excedentes de comida del día a precio reducido, en forma de **"Packs Sorpresa"**.

Es el Trabajo Final Integrador de la Tecnicatura Universitaria en Programación (UTN Facultad Regional Córdoba) de **Ignacio Grudine**.

**Triple impacto:**
- 🌱 **Ambiental** — reduce el desperdicio alimenticio y la huella de carbono.
- 💰 **Comercial** — los comercios recuperan costos de productos que de otro modo descartarían.
- 🤝 **Social** — los consumidores acceden a comida de calidad a precios accesibles.

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
![Angular 21](https://img.shields.io/badge/Angular-21-DD0031?logo=angular)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-4169E1?logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)
![Tests](https://img.shields.io/badge/backend%20tests-495%20passing-brightgreen)

---

## Índice

- [¿Qué resuelve?](#qué-resuelve)
- [Funcionalidades](#funcionalidades)
- [Stack tecnológico](#stack-tecnológico)
- [Arquitectura](#arquitectura)
- [Estructura del repositorio](#estructura-del-repositorio)
- [Cómo levantar el proyecto](#cómo-levantar-el-proyecto)
  - [Opción A — Docker Compose](#opción-a--docker-compose-recomendada)
  - [Opción B — Backend y Frontend por separado](#opción-b--backend-y-frontend-por-separado)
- [Variables de entorno](#variables-de-entorno)
- [Testing](#testing)
- [Documentación de la API](#documentación-de-la-api)
- [Exponer el entorno a internet (Mercado Pago OAuth)](#exponer-el-entorno-a-internet-mercado-pago-oauth)
- [Metodología](#metodología)
- [Documentación adicional](#documentación-adicional)
- [Autor](#autor)

---

## ¿Qué resuelve?

Todos los días, los comercios gastronómicos terminan la jornada con comida en buen estado que no van a vender — panadería del día, packs de sushi, menús armados, postres — y la mayoría termina en la basura. ResQ le da a ese excedente una segunda oportunidad: el comercio lo publica como un pack a precio reducido, el consumidor lo compra desde la app, y lo retira en el horario indicado con un código alfanumérico.

## Funcionalidades

### 👤 Consumidor
- Feed de packs cercanos con filtro por categoría, precio máximo y distancia, más un mini-mapa de comercios.
- Detalle de pack y de comercio, con reseñas filtrables por cantidad de estrellas.
- Checkout con Mercado Pago (Checkout Pro).
- Historial de órdenes propias, con confirmación de retiro por código.
- Reseña post-retiro (1 a 5 estrellas + comentario opcional, una por orden).
- Login tradicional (email + password) o con Google.

### 🏪 Comercio
- Alta y gestión de packs sorpresa (nombre, precio original/con descuento, stock, horario de retiro, foto obligatoria).
- Conexión de su propia cuenta de Mercado Pago (modelo marketplace vía OAuth) — sin eso, no puede publicar ni cobrar.
- Validación de retiro por código.
- Dashboard de métricas (ganancias, órdenes, rating) con gráfico de actividad y exportación de reportes en PDF/Excel.
- Gestión de categorías propias, perfil del comercio y reseñas recibidas.
- Notificaciones in-app (pago recibido, orden cancelada, etc.).

### 🛠️ Administrador
- Dashboard global con filtro de fechas y granularidad (día/semana/mes): ganancias, órdenes, comercios activos, alertas de tokens de Mercado Pago por vencer.
- Gestión de comercios y usuarios (activar/desactivar).
- Gestión de categorías globales.
- Exportación de reportes (global, ranking de comercios, por comercio) en PDF y Excel.

## Stack tecnológico

| Capa | Tecnología |
|---|---|
| Frontend | Angular 21 (standalone components, signals) + Tailwind CSS 4 |
| Backend | .NET 10 (C#), API REST, arquitectura N-Capas |
| Base de datos | PostgreSQL 17 |
| ORM | Entity Framework Core (Npgsql) |
| Autenticación | JWT (access + refresh tokens) + login con Google |
| Pagos | Mercado Pago — modelo Marketplace/OAuth, Checkout Pro |
| Almacenamiento de imágenes | MinIO (S3-compatible) |
| Reverse proxy | Nginx |
| Geolocalización | Google Maps API |
| Jobs en background | Hangfire + Hangfire.PostgreSql |
| Reportería | ClosedXML (Excel) + QuestPDF (PDF) |
| Email | MailKit (SMTP) |
| Validaciones | FluentValidation |
| Manejo de resultados | FluentResults (`Result<T>`) |
| Testing backend | xUnit + Moq + EF Core InMemory + coverlet |
| Contenedores | Docker Compose |

## Arquitectura

El backend sigue una arquitectura en capas (N-Layer) con las dependencias fluyendo siempre de afuera hacia adentro:

```
Controller  →  recibe la request HTTP, valida entrada, devuelve respuesta
    ↓
Service     →  lógica de negocio, reglas del dominio (devuelve Result<T>)
    ↓
Repository  →  acceso a datos (patrón Repository sobre EF Core)
    ↓
DbContext   →  EF Core habla con PostgreSQL
```

Un Controller nunca toca la base de datos directamente, y un Repository nunca contiene reglas de negocio.

En producción/desarrollo dockerizado, Nginx actúa como punto de entrada único: sirve el build estático del frontend, proxea `/api` al backend y `/storage` a MinIO.

## Estructura del repositorio

```
ResQ/
├── Backend/
│   ├── ResQ.API/            ← API REST (.NET 10)
│   │   ├── Controllers/
│   │   ├── Services/
│   │   ├── Repositories/
│   │   ├── Models/
│   │   ├── DTOs/
│   │   └── Data/            ← DbContext, migraciones, seeder
│   └── ResQ.Tests/          ← xUnit + Moq + EF Core InMemory
├── Frontend/                ← Angular 21 + Tailwind CSS 4
│   └── src/app/
│       ├── core/            ← guards, interceptors, models, services
│       ├── features/        ← admin, auth, consumer, home, landing, legal, merchant
│       ├── layouts/         ← layout por rol (admin, auth, consumer, merchant, public)
│       └── shared/          ← directivas y componentes reutilizables
├── nginx/                   ← configuración del reverse proxy
├── docker-compose.yml
├── .env.example
└── CLAUDE.md                ← contexto extendido del proyecto
```

## Cómo levantar el proyecto

### Requisitos previos
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (solo si vas a correr el backend fuera de Docker)
- [Node.js 20+](https://nodejs.org/) y npm (solo si vas a correr el frontend fuera de Docker)

### Opción A — Docker Compose (recomendada)

Levanta todo el stack (PostgreSQL, backend, frontend, Nginx, MinIO) con un solo comando.

```bash
# 1. Cloná el repo
git clone https://github.com/IgnacioGrudine/ResQ.git
cd ResQ

# 2. Copiá el archivo de variables de entorno y completá los valores
cp .env.example .env

# 3. Levantá todo
docker compose up -d
```

La app queda disponible en:

| Servicio | URL |
|---|---|
| App (frontend vía Nginx) | http://localhost |
| API | http://localhost/api |
| Swagger | http://localhost/swagger |
| Consola de MinIO | http://localhost:9001 |

> Si reconstruís `backend` o `frontend` y `nginx` no se recrea en el mismo paso, puede quedar resolviendo la IP vieja del contenedor (`502 Bad Gateway`). Solución: `docker restart resq-nginx`.

### Opción B — Backend y Frontend por separado

Útil para desarrollar con hot-reload.

**Backend:**

```bash
# Levantar solo PostgreSQL en Docker (puerto 5433, evita el conflicto con Postgres nativo en 5432)
docker run -d --name resq-postgres --restart unless-stopped \
  -e POSTGRES_USER=resq -e POSTGRES_PASSWORD=resq_dev -e POSTGRES_DB=resq_db \
  -p 5433:5432 postgres:17-alpine

cd Backend/ResQ.API
dotnet ef database update   # aplica las migraciones
dotnet run                   # https://localhost:7107
```

**Frontend:**

```bash
cd Frontend
npm install
npm start   # ng serve --proxy-config proxy.conf.json → http://localhost:4200
```

El proxy de desarrollo (`proxy.conf.json`) redirige `/api` a `https://localhost:7107`, así que el backend standalone tiene que estar corriendo en paralelo.

## Variables de entorno

Todas las variables viven en `.env` (gitignoreado — nunca commitear credenciales reales). Punto de partida: `.env.example`.

| Variable | Descripción |
|---|---|
| `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB` | Credenciales de PostgreSQL |
| `MINIO_ACCESS_KEY` / `MINIO_SECRET_KEY` | Credenciales de MinIO (almacenamiento de imágenes) |
| `GOOGLE_MAPS_API_KEY` | Se inyecta en el build del frontend (mini-mapa) |
| `GOOGLE_CLIENT_ID` | Client ID de Google usado para "Continuar con Google" |
| `MP_CLIENT_ID` / `MP_CLIENT_SECRET` / `MP_PUBLIC_KEY` | Credenciales de la app de Mercado Pago |
| `MP_ADMIN_ACCESS_TOKEN` | Access token de la cuenta admin de MP |
| `MP_REDIRECT_URI` | Callback OAuth de MP (debe apuntar al dominio público, ver ngrok más abajo) |
| `MP_NOTIFICATION_URL` | URL pública que recibe los webhooks de pago de MP |
| `MP_FRONTEND_BASE_URL` | Dominio público actual (local, ngrok, o producción) — también se usa para resolver URLs de imágenes |
| `MP_USE_TEST_MODE` | `true`/`false` — modo sandbox de Mercado Pago |
| `ENCRYPTION_KEY` | Clave AES-256 en Base64 para cifrar los tokens de MP en reposo. Generar con: <br>`$bytes = New-Object byte[] 32; (New-Object System.Security.Cryptography.RNGCryptoServiceProvider).GetBytes($bytes); [Convert]::ToBase64String($bytes)` |
| `SMTP_HOST` / `SMTP_PORT` / `SMTP_USERNAME` / `SMTP_PASSWORD` | Credenciales del servidor SMTP para el envío de emails |
| `SMTP_FROM_EMAIL` / `SMTP_FROM_NAME` | Remitente de los emails salientes |

## Testing

El backend tiene una suite de **~500 tests** (xUnit + Moq + EF Core InMemory) cubriendo Services, Repositories y Controllers.

```bash
dotnet test Backend/ResQ.sln
```

Con reporte de cobertura:

```bash
dotnet test Backend/ResQ.sln --collect:"XPlat Code Coverage"
```

> Un puñado de tests de Repository están marcados `Skip` con el motivo documentado en el propio atributo — cubren métodos que usan `ExecuteUpdateAsync`, no soportado por el proveedor EF Core InMemory, y necesitan un test de integración contra PostgreSQL real.

## Documentación de la API

Swagger UI expone todos los endpoints de forma interactiva:

| Modo | URL |
|---|---|
| `dotnet run` (local) | https://localhost:7107/swagger |
| Docker Compose | http://localhost/swagger |

El botón **"Authorize"** acepta el `accessToken` devuelto por `POST /api/auth/login` (pegar solo el token, sin el prefijo `Bearer`).

## Exponer el entorno a internet (Mercado Pago OAuth)

El flujo de conexión de Mercado Pago por comercio requiere que MP pueda alcanzar tu callback (`MP_REDIRECT_URI`) y tu endpoint de webhooks (`MP_NOTIFICATION_URL`) desde internet — `localhost` no sirve. Para desarrollo, el proyecto usa [ngrok](https://ngrok.com/) para exponer el stack Docker:

```bash
ngrok http 80
```

Actualizá `MP_REDIRECT_URI`, `MP_NOTIFICATION_URL` y `MP_FRONTEND_BASE_URL` en `.env` con la URL pública que te da ngrok, y recreá el backend para que tome los nuevos valores:

```bash
docker compose up -d --force-recreate backend
```

## Metodología

ResQ se desarrolla con **Scrum simplificado**: sprints de 2 semanas (Sprint 0 de 1 semana), backlog priorizado y un entregable funcional al cierre de cada sprint. Al ser un proyecto de tesis individual, los roles de Scrum Master y Product Owner los ejerce el mismo desarrollador.

| Sprint | Foco |
|---|---|
| ⚙️ Sprint 0 | Setup — repo, Docker + PostgreSQL, solución N-Layer |
| 🔐 Sprint 1 | Auth completo (registro/login/JWT) + schema de base de datos + landing |
| 🛒 Sprint 2 | Catálogo, panel del comercio, vista del consumidor + Google Maps |
| 💳 Sprint 3 | Mercado Pago completo — OAuth, Checkout Pro, webhooks, código de retiro |
| ⭐ Sprint 4 | Reseñas + dashboard de métricas + background jobs |
| 🌟 Sprint 5 | Frontend completo + testing E2E + pulido |

## Documentación adicional

- **[CLAUDE.md](./CLAUDE.md)** — contexto técnico extendido del proyecto (decisiones de arquitectura, convenciones de código, gotchas de testing).
- **Notion** — idea central, documento técnico, esquema de datos completo, diseño visual y planificación por sprint (link disponible en `CLAUDE.md`).

## Autor

**Ignacio Grudine** — Tecnicatura Universitaria en Programación, UTN Facultad Regional Córdoba.
