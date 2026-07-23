# CLAUDE.md — Contexto del proyecto ResQ

> Contexto principal del proyecto. Claude Code lee este archivo automáticamente en cada sesión.
> Toda nueva información clave sobre el dominio, arquitectura o decisiones de diseño debe vivir acá.

---

## 1. Qué es ResQ

**ResQ** es una plataforma web (B2B + B2C, tipo *Too Good To Go*) que conecta comercios gastronómicos locales de Córdoba (Argentina) con consumidores, para vender excedentes de comida del día como **"Packs Sorpresa"** a precio reducido.

**Triple impacto:**
- 🌱 **Ambiental** — reduce desperdicio alimenticio y huella de carbono.
- 💰 **Comercial** — los comercios recuperan costos de productos que de otro modo descartarían.
- 🤝 **Social** — los consumidores acceden a comida de calidad a precios accesibles.

Es la **tesis universitaria de Ignacio Grudine** (Tecnicatura Universitaria en Programación, UTN Facultad Regional Córdoba).

---

## 2. Actores del sistema

| Rol | Descripción |
|---|---|
| **Consumidor (B2C)** | Busca packs por geolocalización/feed, paga online, retira con código alfanumérico, deja reseñas |
| **Comercio (B2B)** | Publica packs sorpresa, gestiona stock y categorías, valida códigos de retiro, consulta dashboard de métricas y reseñas, conecta su propia cuenta de Mercado Pago |
| **Administrador** | Gestiona comercios y usuarios, monitorea operaciones globales vía dashboard, exporta reportería, recibe comisión vía `marketplace_fee` de Mercado Pago |

---

## 3. Stack tecnológico

| Capa | Tecnología |
|---|---|
| **Frontend** | Angular 21 (standalone components, signals, control flow `@if`/`@for`) + **Tailwind CSS 4**, responsivo |
| **Backend** | .NET 10 (C#), API REST |
| **Base de datos** | **PostgreSQL** |
| **ORM** | Entity Framework Core (driver **Npgsql.EntityFrameworkCore.PostgreSQL**) |
| **Autenticación** | **JWT** (Access + Refresh Tokens, login con email/password) **+ login con Google** (`Google.Apis.Auth`) |
| **Pagos** | Mercado Pago — modelo **Marketplace/OAuth** (cada comercio conecta su propia cuenta MP), Checkout Pro |
| **Almacenamiento de imágenes** | MinIO (S3-compatible), servido a través de Nginx (`/storage/`) |
| **Reverse proxy** | Nginx — sirve el frontend estático, proxea `/api` al backend y `/storage` a MinIO |
| **Geolocalización** | Google Maps API (mini-mapa + distancia en el feed) |
| **Jobs en background** | Hangfire + Hangfire.PostgreSql (renovación de tokens MP, procesamiento de webhooks) |
| **Reportería** | ClosedXML (Excel) + QuestPDF (PDF) — reportes globales, por ranking de comercios y por comercio |
| **Email** | MailKit (SMTP) |
| **Validaciones** | FluentValidation |
| **Resultado de operaciones** | FluentResults (`Result<T>` en toda la capa de Services, con errores tipados: `NotFoundError`, `ValidationError`, `ConflictError`, `UnauthorizedError`) |
| **Testing backend** | xUnit + Moq + EF Core InMemory + coverlet (ver §7 y §11) |
| **Contenedores** | Docker Compose — servicios `postgres`, `backend`, `frontend`, `nginx`, `minio` |
| **Túnel de desarrollo** | ngrok — expone el stack local a internet, necesario para el callback OAuth de Mercado Pago (ver skill `ngrok`) |
| **Arquitectura** | N-Layer (N-Capas) clásica en el backend |
| **Metodología** | Scrum simplificado — sprints de 2 semanas (Sprint 0 de 1 semana), backlog priorizado, Product Owner y Scrum Master ejercidos por el mismo desarrollador (proyecto de tesis individual) |
| **Control de versiones** | Git + GitHub |

> ℹ️ La autenticación tradicional (JWT + email/password) es el mecanismo principal; el login con Google es un método adicional, no un reemplazo.

---

## 4. Arquitectura — N-Layer

El backend sigue una arquitectura en capas con separación clara de responsabilidades. Estructura dentro de `Backend/`:

```
Backend/
├── ResQ.sln
├── ResQ.API/
│   ├── Controllers/      ← Endpoints HTTP (capa de presentación)
│   ├── Services/         ← Lógica de negocio (devuelven FluentResults.Result<T>)
│   ├── Repositories/     ← Acceso a datos (patrón Repository + GenericRepository base)
│   ├── Models/           ← Entidades del dominio (EF Core)
│   ├── DTOs/             ← Data Transfer Objects (request/response)
│   ├── Data/              ← DbContext, migraciones, configuraciones EF (Fluent API), DatabaseSeeder
│   ├── Common/Errors/     ← Tipos de error de FluentResults
│   ├── Properties/        ← launchSettings.json
│   ├── Program.cs         ← Composition root + pipeline ASP.NET
│   └── ResQ.API.csproj    ← Target: net10.0
└── ResQ.Tests/            ← xUnit + Moq + EF Core InMemory (ver §7 y §11)
    ├── Services/
    ├── Repositories/
    └── Controllers/
```

**Regla de oro:** las dependencias fluyen de afuera hacia adentro. Controller → Service → Repository → DbContext. Nunca al revés.

---

## 5. Modelo de datos (6 módulos)

| Módulo | Tablas |
|---|---|
| **1. Auth, roles y perfiles** | `Roles`, `Users`, `UserRoles`, `RefreshTokens`, `PasswordResetTokens`, `ConsumerProfiles`, `MerchantProfiles` |
| **2. Catálogo** | `Categories`, `MerchantCategories`, `Products` |
| **3. Órdenes** | `Orders`, `OrderDetails` |
| **4. Reseñas** | `Reviews` |
| **5. Mercado Pago OAuth** | `MerchantMpCredentials`, `MpTokenRefreshLogs`, `MpWebhookEvents` |
| **6. Notificaciones** | `MerchantNotifications` (avisos in-app para el comercio: pago recibido, orden cancelada, etc.) |

### Decisiones de diseño clave

- **Table-Per-Type para perfiles.** `ConsumerProfiles` y `MerchantProfiles` son tablas separadas (un consumidor no tiene CUIT, un comercio no tiene apellido). Evita columnas NULL.
- **Sin `ON DELETE CASCADE` en `Orders`.** El historial financiero nunca se borra en cascada — la integridad contable es prioritaria.
- **Tokens MP cifrados en reposo (AES-256).** `AccessToken` y `RefreshToken` por comercio se cifran desde la capa de servicio (`IEncryptionService`) antes de persistir. La clave de cifrado vive en configuración/user-secrets, NUNCA en la DB.
- **Idempotencia en webhooks MP.** `MpWebhookEvents.MpNotificationId` con UNIQUE constraint. Si MP envía la misma notificación dos veces, el segundo INSERT falla y devolvemos 200 OK sin reprocesar. (Nota de testing: esta rama de excepción específica de Postgres no se puede reproducir con el proveedor EF Core InMemory — se testea a nivel del servicio de ingestión, no del repository.)
- **`ExternalReference` como ancla de correlación.** Cada `Order` genera un UUID propio que se manda a MP como `external_reference`. Cuando llega el webhook, ese UUID correlaciona el pago con la orden interna.
- **Categorías canónicas idempotentes.** `DatabaseSeeder.EnsureCategories` corre en cada arranque (no solo en DB vacía) para que categorías nuevas lleguen a bases que ya tienen datos reales.
- **URL de imagen resuelta dinámicamente, no guardada.** `ImageStorageService.ResolvePublicUrl` computa la URL pública a partir del path relativo guardado + la config actual (`PublicBaseUrl`), en vez de persistir la URL absoluta — así sobrevive a rotaciones del dominio (ej. ngrok) sin necesitar parches manuales en la DB.

---

## 6. Estructura del repositorio

```
ResQ/                       ← Repo Git
├── Backend/                ← Proyecto .NET (ver §4)
│   ├── ResQ.API/
│   └── ResQ.Tests/
├── Frontend/                ← Proyecto Angular 21 + Tailwind CSS 4
│   └── src/app/
│       ├── core/            ← guards, interceptors, models, services (compartidos, sin UI)
│       ├── features/        ← admin, auth, consumer, home, landing, legal, merchant
│       ├── layouts/          ← admin-layout, auth-layout, consumer-layout, merchant-layout, public-layout
│       └── shared/           ← directives (ej. SafeImgDirective), ui (componentes reutilizables)
├── nginx/                   ← nginx.conf (reverse proxy)
├── docker-compose.yml        ← postgres, backend, frontend, nginx, minio
├── .claude/skills/            ← Skills custom de Claude Code
├── .gitignore                ← Plantilla oficial .NET
├── CLAUDE.md                  ← Este archivo
└── README.md
```

---

## 7. Convenciones de código

### Backend (.NET / C#)
- Target framework: **.NET 10** (LTS).
- `Nullable` e `ImplicitUsings` habilitados en el `.csproj`.
- Naming: PascalCase para clases/métodos/propiedades, camelCase para parámetros y variables locales, sufijo `Async` para métodos asíncronos.
- Un archivo `.cs` por clase pública.
- Inyección de dependencias vía constructor (primary constructors de C#, no `ServiceLocator`).
- DTOs separados de entidades — nunca exponer entidades EF directamente desde un Controller.
- Los Services devuelven `FluentResults.Result<T>`; los Controllers lo mapean a `IActionResult` (helpers tipo `ToActionResult()`).

### Frontend (Angular / Tailwind)
- Standalone components únicamente (sin `NgModule`).
- Signals para estado (`signal`, `computed`, `effect`) — se prefieren por sobre RxJS para estado de componente simple.
- Sintaxis de control de flujo nueva: `@if`, `@for`, `@else` (no `*ngIf`/`*ngFor`).
- Paginación client-side consistente en toda la app: `PAGE_SIZE` constante + `page` signal + `pagedX`/`totalPages` computed + `setPage()`, replicado en cada lista que puede crecer (packs, órdenes, reseñas).
- Asterisco rojo (`<span class="text-red-500">*</span>`) en labels de campos obligatorios — convención uniforme en todos los formularios.
- `SafeImgDirective` (`[safeImg]` en vez de `[src]`) para imágenes servidas desde `/storage/` — evita el interstitial de ngrok en desarrollo.

### Testing backend
- Un archivo de test por clase (`XServiceTests.cs`, `XRepositoryTests.cs`, `XControllerTests.cs`), mismo namespace-shape que el código bajo test.
- **Services/Controllers:** Moq — `Mock<IRepository>` como campos privados, un método `CreateSut()` que arma la instancia, bloques `// Arrange` / `// Act` / `// Assert`, nombre de test `MetodoBajoTest_Escenario_ComportamientoEsperado`.
- **Repositories:** EF Core InMemory (`UseInMemoryDatabase(Guid.NewGuid().ToString())`), clase `IDisposable`, helpers `SeedXAsync(...)`.
- ⚠️ **Gotcha de seeding:** `ConsumerProfile`/`MerchantProfile` tienen una navegación `User` requerida (FK `UserId` no nullable). Si el repository bajo test hace `.Include(...).ThenInclude(u => u.User)`, hay que sembrar y linkear un `User` real primero — si no, InMemory excluye silenciosamente esas filas del resultado.
- ⚠️ **Gotcha de proveedor:** `ExecuteUpdateAsync`/`ExecuteUpdate` (bulk update de EF Core 7+) **no está soportado por el proveedor InMemory** — lanza `InvalidOperationException` sin importar la query. Los métodos de repository que lo usan (ej. `MarkAllAsReadAsync`) se marcan `[Fact(Skip = "...")]` con el motivo documentado; necesitan un test de integración contra Postgres real.
- Secciones agrupadas con banners `// ═══...═══` por método bajo test.

### Idiomas
- **Código, identificadores, comentarios técnicos:** inglés.
- **Mensajes de commit:** inglés (Conventional Commits).
- **Documentación de dominio:** español (el dominio es local).

---

## 8. Skills custom de Claude Code

Disponibles en `.claude/skills/`:

- **`/commit`** — Genera commits atómicos en formato Conventional Commits y pushea al final. Verifica que el proyecto compile (backend y/o frontend según corresponda) antes de proponer el plan de commits. Nunca mezcla buckets lógicos distintos en un mismo commit. Usar siempre para commitear cambios. Ver `.claude/skills/commit/SKILL.md`.
- **`ngrok`** — Levanta, verifica o reinicia el túnel ngrok que expone el stack Docker de ResQ a internet, necesario para el callback OAuth de Mercado Pago. Usar cuando el callback de MP falle por error de conexión o el túnel apunte al puerto incorrecto.

---

## 9. Referencias externas

- **Notion (tesis completa):** https://www.notion.so/31aa841e018d80ddb1f9eb8a825fd485 — idea central, documento técnico, esquema de datos, integraciones, pantallas, sprints (Sprint 0 a Sprint 5), diseño visual, colección de prompts. Conectarse vía MCP de Notion si se necesita más detalle del dominio en vez de inventar.
- **GitHub:** https://github.com/IgnacioGrudine/ResQ

---

## 10. Levantar el entorno de desarrollo local

### Opción A — Docker Compose (recomendada, refleja producción)

```bash
docker compose up -d
```

Levanta `postgres`, `backend`, `frontend`, `nginx` y `minio`. La app queda disponible en `http://localhost` (Nginx sirve el frontend y proxea `/api` y `/storage`).

Después de reconstruir `backend`/`frontend`, si `nginx` no fue recreado en el mismo paso puede quedar resolviendo la IP vieja del contenedor (`502 Bad Gateway`) — en ese caso, `docker restart resq-nginx`.

Si necesitás exponer el stack a internet (callback OAuth de Mercado Pago), usar la skill `ngrok`.

### Opción B — Backend standalone

```bash
# 1. Levantar PostgreSQL en Docker (puerto 5433 — evita conflicto con Postgres nativo de Windows en 5432)
docker run -d --name resq-postgres --restart unless-stopped \
  -e POSTGRES_USER=resq -e POSTGRES_PASSWORD=resq_dev -e POSTGRES_DB=resq_db \
  -p 5433:5432 postgres:17-alpine

# 2. Aplicar migraciones
cd Backend/ResQ.API
dotnet ef database update

# 3. Levantar la API
dotnet run
```

### Correr los tests del backend

```bash
dotnet test Backend/ResQ.sln
```

### Swagger UI

| Modo | URL | Notas |
|---|---|---|
| **`dotnet run` (local)** | **https://localhost:7107/swagger** ✅ | Usar HTTPS — HTTP redirige |
| **Docker Compose** | **http://localhost/swagger** ✅ | A través del reverse proxy Nginx |

El botón **"Authorize"** en Swagger acepta el `accessToken` devuelto por `/api/auth/login`.
Formato: pegar solo el token (sin el prefijo `Bearer`).

> ℹ️ En Docker, `UseHttpsRedirection` se deshabilita automáticamente mediante `DOTNET_RUNNING_IN_CONTAINER=true`.
> TLS termination es responsabilidad del reverse proxy en producción.

---

## 11. Cómo trabajar conmigo (Claude) en este proyecto

1. **Asumí el contexto completo de este archivo** — no pidas explicaciones de qué es el proyecto.
2. **Respetá la arquitectura N-Layer.** Lógica de negocio en un Service, no en un Controller.
3. **Para commitear, usá siempre `/commit`** — nunca `git add .` ni mensajes informales.
4. **Si necesitás info adicional del dominio**, conectate al MCP de Notion antes de inventar.
5. **Nunca commitees secrets** — connection strings con password, claves de cifrado, API keys de MP/Google viven en `appsettings.Development.json` (ignorado) o user-secrets.
6. **Un feature no está "terminado" hasta que compila y los tests pasan.** Para cambios de backend, correr `dotnet build`/`dotnet test` antes de proponer un commit. Para cambios de frontend, `ng build --configuration=development` y, si el cambio es visible, verificar en el navegador.
7. **Al agregar tests nuevos**, seguir las convenciones de §7 (Moq para Services/Controllers, EF Core InMemory para Repositories) y respetar los dos gotchas documentados ahí (navegación `User` requerida, `ExecuteUpdateAsync` no soportado por InMemory).
