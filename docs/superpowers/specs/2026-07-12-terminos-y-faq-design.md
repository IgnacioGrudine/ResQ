# Términos y Condiciones + Contáctanos/FAQ — Diseño

## Contexto

La tutora de la PPS (Práctica Profesional Supervisada) exige, con carácter obligatorio para todos
los proyectos activos y a incluir en el último sprint, dos secciones nuevas: **Términos y
Condiciones** y **Contáctanos / Preguntas Frecuentes (FAQ)**, siguiendo el modelo provisto por la
cátedra (`TUP_PPS_Contáctenos_Términosycondiones.pdf`). El modelo es un ejemplo genérico
tipo banco (clave personal, débitos en cuenta, suscripciones); este documento adapta esa
estructura al negocio real de ResQ.

El footer del landing (`Frontend/src/app/features/landing/landing.component.html:244-245`) ya
tiene dos links muertos ("Términos y condiciones", "Política de privacidad") anticipando este
apartado.

## Alcance

- Dos páginas estáticas nuevas, sin backend ni CMS: Términos y Condiciones, y FAQ/Contáctanos.
- Accesibles públicamente (sin login) y también enlazadas desde dentro de la app ya logueada.
- Contenido en español, tono y jerga de ResQ (no bancario).
- **Fuera de alcance** (features reales que hoy no existen en el producto y que el contenido
  legal/FAQ debe reflejar honestamente, no aspiracionalmente): recuperación de contraseña por
  email, cancelación de una reserva ya pagada. Quedan anotadas como tarea separada
  (`task_740378a4`), no se implementan acá.

## Contenido — Términos y Condiciones

Ruta: `/legal/terminos`. Estructura (adaptada del molde de la cátedra):

1. **Bienvenida y aceptación** — Qué es ResQ (plataforma que conecta comercios gastronómicos
   con consumidores para vender excedentes de comida como "packs sorpresa" a precio reducido).
   Usar la app implica aceptar estos Términos y la Política de Privacidad.
2. **Rol de la plataforma** — ResQ es un intermediario tecnológico entre comercios y
   consumidores; no es un comercio gastronómico ni garantiza disponibilidad de stock; el
   comercio es responsable de la calidad e inocuidad de los alimentos que publica.
3. **Registro y cuentas** — Existen dos tipos de cuenta (Consumidor, Comercio); el usuario es
   responsable de la confidencialidad de su contraseña y de la actividad realizada con su cuenta.
4. **Operaciones habilitadas** — Reservar un pack, pagarlo a través de Mercado Pago, y retirarlo
   presentando el código alfanumérico dentro de la franja horaria publicada por el comercio.
5. **Costos y forma de pago** — El consumidor paga el precio publicado por el comercio a través
   de Mercado Pago; ResQ cobra una comisión de plataforma al comercio por cada venta procesada;
   ResQ no es parte de la relación de consumo entre comercio y consumidor más allá de facilitar
   el pago.
6. **Retiro y cancelaciones** — El retiro debe hacerse dentro de la franja horaria publicada; si
   no se retira a tiempo, el pack se pierde sin reembolso. Hoy no existe cancelación de una
   reserva ya pagada (ver Fuera de alcance).
7. **Vigencia y modificaciones** — ResQ puede modificar estos Términos, notificando con
   antelación razonable; el usuario puede dar de baja su cuenta en cualquier momento.
8. **Propiedad intelectual** — El código, diseño y marca de ResQ están protegidos por la Ley
   11.723 de Propiedad Intelectual.
9. **Privacidad de la información** (`id="privacidad"`, ancla para el link del footer) — Datos
   personales y de ubicación tratados con estándares de seguridad; no se venden a terceros; se
   usan solo para operar el servicio (geolocalización de packs, procesamiento de pagos vía
   Mercado Pago).
10. **Contacto** — Remite a `/legal/faq` para dudas, reclamos o sugerencias.

## Contenido — Contáctanos / FAQ

Ruta: `/legal/faq`. Lista plana (sin separar visualmente por rol), formato pregunta/respuesta,
acordeón simple:

1. **¿Qué es ResQ y cómo funciona?** Plataforma que conecta comercios gastronómicos con
   consumidores para vender excedentes de comida como "packs sorpresa" a precio reducido,
   cerca tuyo.
2. **¿Cómo me registro como consumidor?** Desde "Crear cuenta", con email y contraseña.
3. **¿Cómo me registro como comercio?** Flujo de registro de comercio con datos del local y
   ubicación; luego se conecta la cuenta de Mercado Pago para poder cobrar.
4. **Olvidé mi contraseña, ¿cómo la recupero?** Por ahora no hay recuperación automática por
   email; si quedás afuera de tu cuenta, escribinos y te ayudamos a restablecerla manualmente.
5. **¿Cómo reservo y pago un pack?** Elegís un pack en el mapa o el feed, pagás con Mercado
   Pago y recibís un código de retiro.
6. **¿Cómo retiro mi pack?** Mostrás el código alfanumérico en el comercio, dentro de la
   franja horaria publicada.
7. **¿Qué pasa si no retiro a tiempo?** El pack queda perdido, sin reembolso, pasado el horario
   de retiro.
8. **¿Puedo cancelar una reserva ya pagada?** Por el momento no; una vez pagado, el pack queda
   confirmado hasta el horario de retiro.
9. **¿Qué medios de pago acepta ResQ?** Los habilitados por Mercado Pago.
10. **¿ResQ cobra comisión?** Sí, una comisión de plataforma al comercio por cada venta
    procesada; el consumidor paga solo el precio publicado del pack.
11. **¿Cómo dejo una reseña?** Después de retirar un pack, la app te pide calificar tu
    experiencia con el comercio.
12. **¿Tenés una duda, reclamo o sugerencia?** Contacto directo (email de soporte) al pie de la
    sección.

## Ubicación técnica

- Dos rutas top-level nuevas en `Frontend/src/app/app.routes.ts`, mismo patrón que `login` /
  `register` (standalone, `loadComponent`, sin guard):
  - `legal/terminos` → `TerminosComponent`
  - `legal/faq` → `FaqComponent`
- Componentes nuevos en `Frontend/src/app/features/legal/`:
  - `terminos/terminos.component.ts` + `.html`
  - `faq/faq.component.ts` + `.html` (acordeón vía signal, sin llamadas a la API)
- Reemplazar los links muertos del footer (`landing.component.html:244-245`):
  - "Términos y condiciones" → `routerLink="/legal/terminos"`
  - "Política de privacidad" → `routerLink="/legal/terminos"` `fragment="privacidad"`
- Agregar, al pie de la página de perfil (`consumer-layout` y panel de comercio), dos links
  chicos a `/legal/terminos` y `/legal/faq` — sin agregar ítems nuevos a sidebar/bottom-nav.
- Todo estático: sin backend, sin CMS, sin i18n adicional (ya está en español).
- Estilo visual: Tailwind con la paleta de marca ya definida (`evergreen`, `hunter`, `fern`,
  `lime`), consistente con el resto de la app.

## Fuera de alcance (anotado aparte)

Recuperación de contraseña por email y cancelación de reservas pagadas — features reales que
faltan, identificadas durante esta conversación, pero que son trabajo de backend/frontend
independiente y no lo que pidió la cátedra para este sprint. Tarea de background creada:
`task_740378a4`.
