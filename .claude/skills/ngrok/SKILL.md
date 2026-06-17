---
name: ngrok
description: Levanta, verifica o reinicia el túnel ngrok para exponer el stack Docker de ResQ a internet (necesario para el callback OAuth de Mercado Pago). Úsalo cuando el usuario pida levantar ngrok, cuando el callback de MP falle por error de conexión, o cuando el túnel esté apuntando al puerto incorrecto.
allowed-tools: Bash(curl *), PowerShell(*)
shell: powershell
---

## Estado actual del túnel

!`curl -s http://localhost:4040/api/tunnels 2>&1`

---

## Tu trabajo

Sos el encargado de levantar y mantener el túnel ngrok para el proyecto ResQ. Seguí los pasos en orden.

---

### Arquitectura que tenés que tener en mente

```
Internet
   │
   ▼
https://backtalk-railcar-faculty.ngrok-free.dev   ← dominio fijo de ngrok (FREE tier)
   │
   ▼  (túnel ngrok)
localhost:80   ← nginx Docker
   ├── /api/       → backend .NET (puerto 8080 interno)
   ├── /storage/   → MinIO (puerto 9000 interno)
   └── /           → frontend Angular
```

**Regla crítica: ngrok SIEMPRE debe tunear al puerto 80, nunca a otro.**
- El puerto 80 es donde escucha el nginx que actúa de reverse proxy.
- Si ngrok apunta a 5004, 8080, 9000 u otro puerto, el callback de MP rompe con 404/500.

---

### Paso 1 — Verificar si ngrok ya está corriendo

Mirá el output del `curl` de arriba.

**Escenario A — ngrok no está corriendo** (el curl da error de conexión):
→ Saltar directamente al Paso 3.

**Escenario B — ngrok está corriendo pero apunta al puerto INCORRECTO** (addr ≠ `http://localhost:80`):
→ Matar el proceso y relanzar (Paso 2 + Paso 3).

**Escenario C — ngrok está corriendo y apunta a `http://localhost:80`**:
→ No hacer nada, el túnel ya está bien. Confirmárselo al usuario.

---

### Paso 2 — Matar ngrok si está corriendo con configuración incorrecta

```powershell
Get-Process ngrok -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1
```

---

### Paso 3 — Levantar ngrok correctamente

```powershell
Start-Process -FilePath "ngrok" -ArgumentList "http --url=backtalk-railcar-faculty.ngrok-free.dev 80" -WindowStyle Hidden
Start-Sleep -Seconds 3
```

Verificar que arrancó bien:

```powershell
(Invoke-WebRequest -Uri "http://localhost:4040/api/tunnels" -UseBasicParsing).Content
```

Confirmar que el campo `addr` del JSON es `"http://localhost:80"` y que `public_url` es `"https://backtalk-railcar-faculty.ngrok-free.dev"`.

---

### Paso 4 — Verificar que el stack Docker está corriendo

Si ngrok arrancó pero el stack no está levantado, las requests van a fallar igual.

```powershell
docker --context desktop-linux compose -f "C:\Users\Ignacio Grudine\OneDrive\Escritorio\ResQ\ResQ\docker-compose.yml" ps --format "table {{.Name}}\t{{.Status}}"
```

Todos los contenedores deben estar en estado `Up`. Si alguno está `Exited` o `Restarting`, reportarlo al usuario.

---

### Paso 5 — Confirmación final

Reportar al usuario:

```
✅ Túnel ngrok activo:
   https://backtalk-railcar-faculty.ngrok-free.dev → localhost:80

   Dashboard ngrok: http://localhost:4040
   Callback MP:     https://backtalk-railcar-faculty.ngrok-free.dev/api/auth/mp/callback
```

---

## Reglas y restricciones

| Regla | Detalle |
|---|---|
| **Puerto siempre 80** | nginx escucha en 80. Nunca tunear a 8080, 5004, 9000 ni ningún otro. |
| **URL fija de dominio** | El dominio `backtalk-railcar-faculty.ngrok-free.dev` está configurado en Mercado Pago como `redirect_uri`. Cambiar la URL rompe el OAuth. |
| **No levantar sin Docker** | ngrok sin el stack Docker levantado no sirve de nada — las requests llegan pero no hay quién las procese. Siempre verificar los contenedores. |
| **No dejar 2 instancias** | Si hay un ngrok corriendo apuntando al puerto equivocado, matar primero y relanzar. Dos instancias de ngrok con el mismo dominio fijo causan conflicto. |
| **Plan FREE de ngrok** | El dominio fijo es de cuenta FREE. No intentar cambiar el dominio, URL, ni agregar flags de TLS personalizado — rompe el plan. |

## Consejos útiles

- **Dashboard**: `http://localhost:4040` — desde acá se ven todas las requests que llegan al túnel, los status codes y los bodies. Muy útil para debuggear el callback de MP.
- **El callback de MP va al backend directamente**: la URL de redirect está configurada como `https://backtalk-railcar-faculty.ngrok-free.dev/api/auth/mp/callback`. Nunca va al frontend.
- **Si el ngrok da error 401/403**: el token de autenticación de ngrok puede estar vencido. El usuario debe loguear con `ngrok login` manualmente.
- **Si el backend tarda en arrancar**: el callback puede llegar antes de que el backend esté listo. Docker tarda ~5s en levantar. Si ves 502 en el dashboard de ngrok, esperar y reintentar el flujo de MP.
- **Contexto Docker**: siempre usar `--context desktop-linux` en los comandos docker para Docker Desktop en Windows.
