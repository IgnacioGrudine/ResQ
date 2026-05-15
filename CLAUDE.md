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

Es la **tesis universitaria de Ignacio Grudine**.

---

## 2. Actores del sistema

| Rol | Descripción |
|---|---|
| **Consumidor (B2C)** | Busca packs por geolocalización, paga online, retira con código QR/alfanumérico |
| **Comercio (B2B)** | Publica packs sorpresa, valida códigos de retiro, consulta dashboard de métricas |
| **Administrador** | Gestiona usuarios, monitorea operaciones globales, recibe comisión vía `marketplace_fee` de Mercado Pago |

---

## 3. Stack tecnológico

| Capa | Tecnología |
|---|---|
| **Frontend** | Angular (última versión) + **Tailwind CSS**, responsivo |
| **Backend** | .NET 10 (C#), API REST |
| **Base de datos** | **PostgreSQL** |
| **ORM** | Entity Framework Core (driver **Npgsql.EntityFrameworkCore.PostgreSQL**) |
| **Autenticación** | **JWT tradicional** (Access + Refresh Tokens, login con email/password) |
| **Pagos** | Mercado Pago — modelo **Marketplace/OAuth** (cada comercio conecta su propia cuenta MP) |
| **Geolocalización** | Google Maps API |
| **Contenedores** | **Docker** (toda la app dockerizada — API, DB, frontend) |
| **Arquitectura** | N-Layer (N-Capas) clásica |
| **Metodología** | Scrum + Jira |
| **Control de versiones** | Git + GitHub |

> ⚠️ **Sin OAuth2 / Social Login.** La autenticación es 100% tradicional con email + password.

---

## 4. Arquitectura — N-Layer

El backend sigue una arquitectura en capas con separación clara de responsabilidades. Estructura dentro de `Backend/ResQ.API/`:

```
Backend/
├── ResQ.sln
└── ResQ.API/
    ├── Controllers/      ← Endpoints HTTP (capa de presentación)
    ├── Services/         ← Lógica de negocio
    ├── Repositories/     ← Acceso a datos (patrón Repository)
    ├── Models/           ← Entidades del dominio (EF Core)
    ├── DTOs/             ← Data Transfer Objects (request/response)
    ├── Data/             ← DbContext, migraciones, configuraciones EF
    ├── Properties/       ← launchSettings.json
    ├── Program.cs        ← Composition root + pipeline ASP.NET
    └── ResQ.API.csproj   ← Target: net10.0
```

**Regla de oro:** las dependencias fluyen de afuera hacia adentro. Controller → Service → Repository → DbContext. Nunca al revés.

---

## 5. Modelo de datos (5 módulos)

| Módulo | Tablas |
|---|---|
| **1. Auth, roles y perfiles** | `Roles`, `Users`, `UserRoles`, `RefreshTokens`, `ConsumerProfiles`, `MerchantProfiles` |
| **2. Catálogo** | `Categories`, `MerchantCategories`, `Products` |
| **3. Órdenes** | `Orders`, `OrderDetails` |
| **4. Reseñas** | `Reviews` |
| **5. Mercado Pago OAuth** | `MerchantMpCredentials`, `MpTokenRefreshLogs`, `MpWebhookEvents` |

### Decisiones de diseño clave

- **Table-Per-Type para perfiles.** `ConsumerProfiles` y `MerchantProfiles` son tablas separadas (un consumidor no tiene CUIT, un comercio no tiene apellido). Evita columnas NULL.
- **Sin `ON DELETE CASCADE` en `Orders`.** El historial financiero nunca se borra en cascada — la integridad contable es prioritaria.
- **Tokens MP cifrados en reposo (AES-256).** `AccessToken` y `RefreshToken` por comercio se cifran desde la capa de servicio antes de persistir. La clave de cifrado vive en Azure Key Vault, NUNCA en la DB.
- **Idempotencia en webhooks MP.** `MpWebhookEvents.MpNotificationId` con UNIQUE constraint. Si MP envía la misma notificación dos veces, el segundo INSERT falla y devolvemos 200 OK sin reprocesar.
- **`ExternalReference` como ancla de correlación.** Cada `Order` genera un UUID propio que se manda a MP como `external_reference`. Cuando llega el webhook, ese UUID correlaciona el pago con la orden interna.

---

## 6. Estructura del repositorio

```
ResQ/                       ← Repo Git
├── Backend/                ← Proyecto .NET (ver §4)
├── Frontend/               ← Proyecto Angular + Tailwind (pendiente de scaffold)
├── .claude/skills/         ← Skills custom de Claude Code
├── .gitignore              ← Plantilla oficial .NET
├── CLAUDE.md               ← Este archivo
└── README.md
```

---

## 7. Convenciones de código

### Backend (.NET / C#)
- Target framework: **.NET 10** (LTS).
- `Nullable` e `ImplicitUsings` habilitados en el `.csproj`.
- Naming: PascalCase para clases/métodos/propiedades, camelCase para parámetros y variables locales, sufijo `Async` para métodos asíncronos.
- Un archivo `.cs` por clase pública.
- Inyección de dependencias vía constructor (no `ServiceLocator`).
- DTOs separados de entidades — nunca exponer entidades EF directamente desde un Controller.

### Idiomas
- **Código, identificadores, comentarios técnicos:** inglés.
- **Mensajes de commit:** inglés (Conventional Commits).
- **Documentación de dominio:** español (el dominio es local).

---

## 8. Skills custom de Claude Code

Disponibles en `.claude/skills/`:

- **`/commit`** — Genera commits atómicos en formato Conventional Commits y pushea al final. Usar siempre para commitear cambios. Ver `.claude/skills/commit/SKILL.md` para detalles de funcionamiento.

---

## 9. Referencias externas

- **Notion (tesis completa):** https://www.notion.so/31aa841e018d80ddb1f9eb8a825fd485 — idea central, documento técnico, esquema de datos, integraciones, pantallas. Conectarse vía MCP de Notion si se necesita más detalle del dominio.
- **GitHub:** https://github.com/IgnacioGrudine/ResQ

---

## 10. Cómo trabajar conmigo (Claude) en este proyecto

1. **Asumí el contexto completo de este archivo** — no pidas explicaciones de qué es el proyecto.
2. **Respetá la arquitectura N-Layer.** Lógica de negocio en un Service, no en un Controller.
3. **Para commitear, usá siempre `/commit`** — nunca `git add .` ni mensajes informales.
4. **Si necesitás info adicional del dominio**, conectate al MCP de Notion antes de inventar.
5. **Nunca commitees secrets** — connection strings con password, claves de cifrado, API keys de MP/Google viven en `appsettings.Development.json` (ignorado) o user-secrets.
