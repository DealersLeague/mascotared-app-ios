# 📱 README — Mascotared

## ¿Qué es Mascotared?

**Mascotared** es una aplicación móvil multiplataforma (Android, iOS, macOS, Windows) construida con **.NET MAUI** que conecta a **propietarios de mascotas** con **cuidadores de confianza**. Funciona como una red social especializada en el cuidado animal, combinando búsqueda de servicios, chat en tiempo real, feed social y gestión completa del perfil de cada mascota.

---

## 🛠 Tecnologías

| Tecnología | Versión | Uso |
|---|---|---|
| .NET MAUI | 10.0.41 | Framework UI multiplataforma |
| C# / XAML | .NET 10 | Lenguaje y definición de UI |
| OneSignal SDK | 5.2.2 | Notificaciones push |
| Plugin.Fingerprint | 2.1.5 | Autenticación biométrica |
| Plugin.LocalNotification | 13.0.0 | Notificaciones locales |
| System.Text.Json | Integrado | Serialización JSON |

**Plataformas objetivo:** `net10.0-android` · `net10.0-ios` · `net10.0-maccatalyst` · `net10.0-windows10.0.19041.0`

---

## 🏗 Arquitectura

```
App (MAUI Shell)
├── Autenticación: LogIn / Register / Onboarding
├── AppShell (TabBar + Flyout)
│   ├── MainPage          → Feed principal + cuidadores cercanos
│   ├── Cuidadores        → Explorar cuidadores/propietarios
│   ├── TaskPopup         → Mis solicitudes y ofertas
│   ├── PerfilConfigUser  → Perfil del usuario
│   ├── Favoritos         → Cuidadores y ofertas guardadas
│   └── MisMascotasLista  → Mis mascotas
├── Services/
│   ├── ApiService        → Cliente HTTP centralizado (REST)
│   ├── UsuarioService    → Singleton con estado del usuario en memoria
│   ├── OneSignalService  → Notificaciones push
│   └── Repositorios      → Mascotas (JSON local), Tareas, Conversaciones
└── Views/
    ├── Chatpage          → Chat directo entre usuarios
    ├── Momentos          → Feed social (publicaciones)
    ├── MisMascotas       → Detalle completo de una mascota
    └── ... (más pantallas)
```

**Patrón:** MVVM simplificado · **Persistencia:** Preferences MAUI (sesión) + JSON local (mascotas) + API REST (fuente de verdad)

**API Backend:** `http://91.98.163.114:5000/api`  
**Autenticación:** JWT Bearer Token

---

## ✨ Funcionalidades principales

- 🔐 **Autenticación** — Login/registro con email, biometría, recuperación de contraseña, verificación por email
- 📍 **Geolocalización** — Muestra cuidadores cercanos ordenados por distancia (radio de 5 km)
- 💬 **Chat en tiempo real** — Mensajería directa entre usuarios con polling automático
- 📋 **Ofertas y solicitudes** — Cuidadores publican disponibilidad, propietarios solicitan servicios, sistema de aceptación/rechazo/finalización
- 🐾 **Gestión de mascotas** — Perfil exhaustivo con salud, alimentación, comportamiento, veterinario, documentación
- 📸 **Feed social (Momentos)** — Publicaciones con fotos, likes y comentarios
- ⭐ **Reseñas** — Sistema de valoración por estrellas con histograma
- ❤️ **Favoritos** — Guardar cuidadores y ofertas de interés
- 🔔 **Push notifications** — OneSignal vinculado al usuario autenticado
- 🌙 **Tema claro/oscuro** — Adaptación completa de la UI

---

## 📂 Estructura del proyecto

```
Mascotared/
├── App.xaml.cs                  # Entrada: restauración de sesión
├── AppShell.xaml                # Navegación principal
├── LogIn.xaml.cs                # Login + registro OneSignal
├── Register.xaml.cs             # Registro de nuevos usuarios
├── MainPage.xaml.cs             # Home: feed + cuidadores cercanos
├── Services/
│   ├── ApiServices.cs           # Todos los endpoints (1 archivo ~1000 líneas)
│   ├── TaskService.cs           # Modelos de datos
│   ├── UsuarioService.cs        # Singleton de sesión
│   ├── OneSignalService.cs      # Push notifications
│   └── Repositories/            # Persistencia local (JSON)
├── Views/
│   ├── Chatpage.xaml.cs         # Chat directo
│   ├── Momentos.xaml.cs         # Feed social
│   ├── Tasks.xaml.cs            # Crear/editar ofertas
│   ├── TaskPopup.xaml.cs        # Mis solicitudes
│   ├── Favoritos.xaml.cs        # Cuidadores favoritos
│   ├── FavoritosTask.xaml.cs    # Ofertas favoritas
│   └── MisMascotas.xaml.cs      # Detalle de mascota
├── Cuidadores/                  # Pantallas de cuidadores
├── Propietarios/                # Pantallas de propietarios
├── Localization/                # Recursos de texto para internacionalización
├── Models/                      # Items de datos (Usuario, Mascota, Oferta, Solicitud, Mensaje, Reseña)
└── Perfil/                      # Perfil y edición
```

---
---

# 📖 Manual de Usuario — Mascotared

---

## 1. Primeros pasos

### 1.1 Registrarse

1. Abre la app → pulsa **"Regístrate"**
2. Rellena: nombre completo, email, contraseña (mínimo 8 caracteres) y fecha de nacimiento (debes ser mayor de 18 años)
3. Elige tu género (opcional)
4. Selecciona tu rol — puedes marcar **Propietario**, **Cuidador** o ambos
5. Pulsa **Crear cuenta** → recibirás un email de verificación en tu correo
6. Verifica el email antes de acceder a todas las funciones

### 1.2 Iniciar sesión

1. Escribe tu email y contraseña
2. Pulsa **Entrar**
3. La sesión queda guardada — la próxima vez que abras la app entrarás directamente sin necesidad de volver a logarte

### 1.3 ¿Olvidaste la contraseña?

1. En la pantalla de login, escribe tu email en el campo correspondiente
2. Pulsa **"¿Olvidaste tu contraseña?"**
3. Recibirás un enlace de restablecimiento en tu correo

---

## 2. Pantalla principal (Home)

Al entrar verás tres secciones:

### 🗺 Cuidadores cercanos
- La app solicita permiso de ubicación
- Muestra cuidadores a menos de **5 km** de ti ordenados por distancia
- Cada tarjeta muestra: foto, nombre, valoración ⭐, tarifa/hora y distancia
- Pulsa en una tarjeta para ver el perfil completo del cuidador

### 🐾 Mascotas destacadas
- Mascotas públicas de propietarios cercanos
- Pulsa **"Ver todas"** para acceder a la pantalla de mascotas por dueño, con buscador

### 📸 Momentos (feed)
- Publicaciones de la comunidad con fotos y descripciones
- Puedes dar ❤️ like y ver 💬 comentarios directamente desde el feed

---

## 3. Explorar cuidadores y propietarios

### 3.1 Pestaña Cuidadores
Muestra todos los usuarios registrados como **cuidadores** con su oferta de servicios:

| Campo | Descripción |
|---|---|
| 🕐 Disponibilidad | Días de la semana y franja horaria |
| 💰 Tarifa | Precio por hora |
| 🐕 Animales | Tipos de animales que cuida |
| 📍 Ubicación | Ciudad o zona |
| ⭐ Valoración | Media de reseñas recibidas |

Pulsa en una tarjeta para ver el **popup completo** con toda la información y el botón para **enviar mensaje** o **solicitar servicio**.

### 3.2 Pestaña Propietarios
Muestra propietarios que buscan cuidador para sus mascotas:

- Verás sus mascotas, fechas requeridas y presupuesto
- Pulsa para ver el perfil completo y contactarles

---

## 4. Ofertas y solicitudes

### 4.1 Crear una oferta

Accede desde la pestaña **Task** (icono central) → pulsa el botón de crear.

**Si eres cuidador**, defines:
- Título y descripción del servicio
- Tags (paseos, guardería, veterinario...)
- Días disponibles y franja horaria
- Número máximo de mascotas que puedes atender
- Tipos de animales que aceptas
- Si tienes experiencia con necesidades especiales

**Si eres propietario**, defines:
- Descripción de lo que necesitas
- Fechas de inicio y fin
- Horarios exactos o franjas
- Presupuesto total
- Las mascotas concretas que necesitan cuidado
- Cuidados especiales requeridos

### 4.2 Solicitar un servicio

Desde el perfil de un cuidador o propietario:
1. Pulsa **"Solicitar"** o **"Enviar solicitud"**
2. Puedes añadir un mensaje opcional
3. El otro usuario recibirá una notificación

### 4.3 Gestionar solicitudes recibidas

En la pestaña **Task**:
- Verás las solicitudes que has recibido en tus ofertas
- Cada solicitud muestra: nombre del solicitante, su foto y mensaje
- Pulsa **Aceptar** o **Rechazar**
- Al aceptar, la solicitud pasa a estado "Aceptada" y podéis coordinar por chat

### 4.4 Ver el estado de tus solicitudes enviadas

También en la pestaña **Task** → mis solicitudes:
- **Pendiente** — aún sin respuesta
- **Aceptada** ✅ — el cuidador/propietario ha confirmado
- **Rechazada** ❌ — no ha sido aceptada

### 4.5 Finalizar una oferta

Cuando el servicio se haya completado:
1. Entra en tu oferta
2. Pulsa **Finalizar**
3. La oferta se marca como completada y desaparece del chat

---

## 5. Chat

### 5.1 Cómo acceder al chat

- Desde el perfil de un usuario → pulsa el icono de mensaje
- Desde la pestaña **Mensajes** → lista de todas tus conversaciones

### 5.2 Interfaz del chat

```
┌─────────────────────────────────────┐
│  ← Nombre del contacto       ···    │
│     ⭐ Valoración · Activo          │
├─────────────────────────────────────┤
│  [Oferta vinculada]            →    │  ← tarea solicitada
│  ✅ Solicitud aceptada              │  ← estado actual
├─────────────────────────────────────┤
│                                     │
│  Mensaje recibido                   │
│  11:14                              │
│                    Mensaje enviado  │
│                          11:15 ✓✓  │
│                                     │
├─────────────────────────────────────┤
│  Escribe un mensaje...         ↑    │
└─────────────────────────────────────┘
```

- Los mensajes tuyos aparecen a la **derecha** (azul)
- Los mensajes del otro usuario aparecen a la **izquierda** (gris)
- ✓✓ indica mensaje leído
- La oferta vinculada aparece en la parte superior; desaparece cuando la oferta se finaliza

### 5.3 Notificaciones de nuevos mensajes

- Recibirás una notificación push cuando alguien te escriba
- El icono de mensajes en el home mostrará un badge con el número de mensajes no leídos

---

## 6. Momentos (feed social)

### 6.1 Ver publicaciones

En el **Home** o en la pestaña de Momentos verás el feed con publicaciones de la comunidad:
- Foto + descripción
- Nombre del autor y tiempo transcurrido
- Botón ❤️ para dar like
- Botón 💬 para ver y añadir comentarios

### 6.2 Publicar un momento

1. Pulsa el botón **"+"** (Nuevo momento)
2. Selecciona una foto de tu galería
3. Escribe una descripción
4. Pulsa **Publicar**

### 6.3 Gestionar tus publicaciones

En tu perfil o en el feed puedes:
- ✏️ Editar la descripción de una publicación tuya
- 🗑️ Eliminar una publicación tuya

---

## 7. Mis mascotas

### 7.1 Añadir una mascota

1. Ve a la pestaña **Mascotas** → pulsa **"+"**
2. Rellena la información por secciones:

| Sección | Información |
|---|---|
| 🏷 Básica | Nombre, especie, raza, sexo, fecha de nacimiento, peso |
| 🔍 Identificación | Número de microchip, anilla, CITES, estado reproductivo |
| 💊 Salud | Alergias, condiciones médicas, medicación, horario de dosis |
| 🧠 Comportamiento | Miedos, reactividad, si convive bien con niños u otras especies |
| 🍖 Alimentación | Nivel de energía, frecuencia, alimentos prohibidos |
| 🏥 Veterinario | Nombre, dirección, teléfono de tu vet habitual |
| 🚨 Emergencias | Centro de urgencias, teléfono, presupuesto de emergencia |
| 📄 Documentación | Vacunas al día, seguro de responsabilidad civil |

3. Añade una foto de tu mascota
4. Pulsa **Guardar**

### 7.2 Editar una mascota

En la lista de tus mascotas → pulsa la tarjeta → edita los campos que necesites → **Guardar**

### 7.3 Mascotas públicas

Puedes hacer públicas las mascotas de tu perfil para que otros usuarios las vean en el feed de "Mascotas por dueño" desde el home.

---

## 8. Perfil de usuario

### 8.1 Información que puedes configurar

Accede desde la pestaña **Perfil**:

- 📸 **Foto de perfil** — elige desde galería; se sube automáticamente al servidor
- 👤 **Nombre y descripción personal**
- 📍 **Localización** — ciudad o barrio
- 💰 **Tarifa por hora** (si eres cuidador)
- 📅 **Disponibilidad** — días y franjas horarias
- 🌐 **Idioma** de la app (Español por defecto)
- 🎨 **Tema** — Claro u Oscuro
- 🔤 **Tamaño de letra**

### 8.2 Ver tu perfil público

Pulsa **"Perfil Público"** en el menú para ver exactamente cómo te ven otros usuarios, incluyendo tus reseñas y mascotas públicas.

### 8.3 Cambiar contraseña

Perfil → Ajustes de seguridad → **Cambiar contraseña** → introduce la actual y la nueva

### 8.4 Eliminar cuenta

Perfil → Ajustes → **Eliminar cuenta** → confirmación requerida. Esta acción es irreversible.

---

## 9. Reseñas

### 9.1 Recibir reseñas

Cuando completes un servicio como cuidador, el propietario puede dejarte una valoración de 1 a 5 estrellas con comentario.

### 9.2 Ver tus reseñas

En tu perfil verás:
- ⭐ Media general
- Histograma de distribución (cuántas reseñas de 1, 2, 3, 4 y 5 estrellas)
- Lista de comentarios con nombre del autor y fecha

### 9.3 Dejar una reseña

Desde el perfil de un cuidador con quien hayas trabajado → **"Dejar reseña"** → elige estrellas y escribe un comentario.

---

## 10. Favoritos

### 10.1 Guardar un cuidador

Desde el perfil o tarjeta de cualquier cuidador → pulsa el ❤️ → queda guardado en tu lista de favoritos.

### 10.2 Guardar una oferta

Desde cualquier oferta → pulsa el icono de guardar → accesible desde **Favoritos → Ofertas**.

### 10.3 Acceder a favoritos

Pestaña **Favoritos** en el menú:
- **Cuidadores** — tus cuidadores guardados con tarifa y valoración
- **Ofertas** — ofertas que has marcado como interesantes

---

## 11. Notificaciones

La app envía notificaciones push para:

| Evento | Notificación |
|---|---|
| Nuevo mensaje recibido | "Tienes un nuevo mensaje de [nombre]" |
| Solicitud aceptada | "Tu solicitud ha sido aceptada" |
| Solicitud rechazada | "Tu solicitud no ha sido aceptada" |
| Nueva solicitud recibida | "Alguien ha solicitado tu oferta" |

Para que funcionen correctamente:
- Acepta los permisos de notificación cuando la app los solicite
- Mantén la sesión iniciada (la app gestiona esto automáticamente)

---

## 12. Preguntas frecuentes

**¿Puedo ser propietario y cuidador a la vez?**
Sí. Durante el registro (o editando tu perfil) puedes activar ambos roles.

**¿La sesión expira?**
El token de sesión tiene caducidad. Si al abrir la app te pide iniciar sesión de nuevo, introduce tus credenciales; los datos del perfil y mascotas se recuperan automáticamente.

**¿Mis mascotas son visibles para todos?**
Solo si las marcas como "públicas". Por defecto son privadas.

**¿Cómo sé que un cuidador es de confianza?**
Fíjate en su valoración ⭐, número de reseñas, reseñas con comentarios detallados y si tiene el badge de verificado ✅.

**¿Puedo cancelar una solicitud enviada?**
Sí, desde la pestaña Task → mis solicitudes → pulsa **Cancelar** en la solicitud que esté en estado "Pendiente".

**No me llegan notificaciones, ¿qué hago?**
1. Comprueba que los permisos de notificación estén activados en los ajustes del teléfono para Mascotared
2. Cierra la app completamente y vuelve a abrirla (esto re-registra tu dispositivo en el servidor de notificaciones)

---

*Mascotared — Conectando mascotas y cuidadores de confianza* 🐾