# Final Fantasy I 2D-HD Remake
## Trabajo de fin de grado - Desarrollo de Aplicaciones Multiplataforma (DAM)
### Por: Alberto Márquez Flores

## Índice

1.  [Introducción](#1-introducción)
    * [Descripción del proyecto](#descripción-del-proyecto)
    * [Justificación y objetivos](#justificación-y-objetivos)
    * [Motivación](#motivación)
2.  [Funcionalidades del proyecto y tecnologías utilizadas](#2-funcionalidades-del-proyecto-y-tecnologías-utilizadas)
    * [Backend (API REST - Spring Boot)](#backend-api-rest---spring-boot)
        * [Arquitectura general y motor de base de datos](#arquitectura-general-y-motor-de-base-de-datos)
        * [Diseño de la base de datos (`users` y `save_states`)](#diseño-de-la-base-de-datos-users-y-save_states)
        * [Sistema de seguridad (Spring Security & JWT)](#sistema-de-seguridad-spring-security--jwt)
        * [DTOs (Data Transfer Objects)](#dtos-data-transfer-objects)
        * [Controladores (Controllers)](#controladores-controllers)
        * [Servicios (Services)](#servicios-services)
        * [Repositorios (Repositories)](#repositorios-repositories)
        * [Utilidades (JwtUtil)](#utilidades-jwtutil)
    * [Frontend (Juego - Unity)](#frontend-juego---unity)
        * [Arquitectura general y organización](#arquitectura-general-y-organización)
        * [Sistemas principales del juego](#sistemas-principales-del-juego)
            * [Sistema de autenticación de usuarios](#sistema-de-autenticación-de-usuarios)
            * [Sistema de guardado y carga de Partidas](#sistema-de-guardado-y-carga-de-partidas)
            * [Control del jugador y movimiento](#control-del-jugador-y-movimiento)
            * [Sistema de encuentros aleatorios y combate](#sistema-de-encuentros-aleatorios-y-combate)
            * [Gestión de personajes y clases](#gestión-de-personajes-y-clases)
            * [Sistema de habilidades](#sistema-de-habilidades)
            * [Sistema de inventario y objetos](#sistema-de-inventario-y-objetos)
            * [Recompensas de batalla](#recompensas-de-batalla)
            * [Música y sonido](#música-y-sonido)
            * [Transiciones de escena](#transiciones-de-escena)
            * [Gestión de UI y foco](#gestión-de-ui-y-foco)
        * [ScriptableObjects clave](#scriptableobjects-clave)
    * [Integración Frontend-Backend (Unity y Spring Boot API)](#integración-frontend-backend-unity-y-spring-boot-api)
    * [Despliegue y mantenimiento](#despliegue-y-mantenimiento)
3.  [Guía de instalación](#3-guía-de-instalación)
4.  [Guía de uso](#4-guía-de-uso)
    * [Acceso al juego](#acceso-al-juego)
    * [Registro y login](#registro-y-login)
    * [Exploración y combate](#exploración-y-combate)
    * [Guardar y cargar partida](#guardar-y-cargar-partida)
5.  [Enlaces de interés](#5-enlaces-de-interés)
    * [Enlace a la documentación (GitHub)](#enlace-a-la-documentación-github)
    * [Enlace a figma de la interfaz](#enlace-a-figma-de-la-interfaz)
6.  [Conclusión](#6-conclusión)
7.  [Contacto](#7-contacto)

---

## 1. Introducción

### Descripción del proyecto

Este documento detalla el diseño y la implementación de un proyecto de videojuego de rol (RPG) por turnos en 2D-HD, desarrollado como un trabajo de fin de grado (TFG).

Inspirado en el videojuego clásico de Final Fantasy I, el objetivo principal es crear una demo jugable que demuestre la implementación de los sistemas claves desarrollados y la integración robusta entre un cliente de juego (frontend) y un servicio backend para la gestión de usuarios y la persistencia de datos de partida.

El proyecto se estructura en dos componentes principales:
* **Frontend (Cliente)**: Desarrollado en Unity (con Universal Render Pipeline - URP) utilizando C#, incluyendo gráficos en pixel art hechos a mano, animaciones, lógica de juego, sistemas de combate, menús e interacción con el usuario.
* **Backend (API REST)**: Implementado con Spring Boot (Java), este componente se encarga de la lógica de negocio central, la gestión de usuarios, la autenticación y autorización segura, y la persistencia de los estados de partida en una base de datos.

La comunicación entre el frontend y el backend se realiza mediante peticiones HTTP a una API REST, lo que asegura la independencia de ambas partes y la escalabilidad de las funcionalidades de usuario y guardado en la nube.

### Justificación y objetivos

La elección de este proyecto se debe a mi pasión por la saga Final Fantasy y a la necesidad de aplicar los conocimientos adquiridos durante el ciclo de Desarrollo de Aplicaciones Multiplataforma.

Los objetivos principales han sido:
* Diseñar e implementar una arquitectura backend robusta y segura para la gestión de usuarios y datos de partida en la nube.
* Desarrollar un cliente de juego funcional en Unity que integre los sistemas fundamentales de un RPG por turnos.
* Establecer una comunicación eficiente y segura entre el frontend y el backend utilizando una API RESTful.
* Aprender y aplicar las mejores prácticas de desarrollo de software en un entorno de proyecto real.
* Demostrar la capacidad de desarrollar un producto software completo, desde la concepción hasta el despliegue.

### Motivación

La principal motivación detrás de este proyecto es la admiración por los videojuegos RPG desarrollados por Squaresoft y Square Enix, y en particular, por la saga Final Fantasy. He buscado rendir un homenaje a la primera entrega, recreando su esencia jugable y visual con un toque moderno en 2D-HD propio de juegos como Octopath Traveler o Triangle Strategy.

## 2. Funcionalidades del proyecto y tecnologías utilizadas

### Backend (API REST - Spring Boot)

El componente backend es una API RESTful desarrollada en Java con el framework Spring Boot. Su propósito fundamental es ofrecer un conjunto de endpoints seguros y eficientes para gestionar la autenticación de usuarios y la persistencia de los datos de juego, garantizando que el progreso del jugador se almacene de forma centralizada y segura en la nube.

#### Arquitectura general y motor de base de datos

El backend sigue una arquitectura de capas estándar (controladores, servicios, repositorios) para una clara separación de responsabilidades.

* **Framework principal**: Spring Boot (Java), que facilita el desarrollo rápido de aplicaciones basadas en Spring con configuración automática y un servidor Tomcat embebido.
* **Motor de base de datos**: PostgreSQL, un sistema de gestión de bases de datos relacional de código abierto, robusto y potente, ideal para la persistencia de datos estructurados y JSON.
* **Despliegue en la nube**: La base de datos PostgreSQL está alojada en Neon.tech.  Neon.tech ofrece una instancia de base de datos en la nube con características como escalabilidad, alta disponibilidad y una capa gratuita que incluye la pausa automática de su "Compute" (el motor de procesamiento de la DB) en periodos de inactividad para ahorrar recursos.
* **Conexión**: La API se conecta a la base de datos de Neon.tech utilizando el driver JDBC para PostgreSQL. Las credenciales de conexión (URL, usuario, contraseña) se inyectan de forma segura a través de variables de entorno en el entorno de despliegue (Render.com), siguiendo las mejores prácticas de seguridad y flexibilidad, y no están *hardcodeadas* en el código fuente.
* **Servidor Web embebido**: Tomcat (por defecto en Spring Boot), escuchando en el puerto 8080.
* **Despliegue de la API**: La API REST está desplegada en Render.com. Render es una plataforma de despliegue continuo que automatiza la compilación y el despliegue de aplicaciones desde un repositorio de GitHub. Su capa gratuita ofrece un entorno robusto para aplicaciones pequeñas.

#### Diseño de la base de datos (`users` y `save_states`)

La persistencia de datos se gestiona mediante JPA (Java Persistence API) e Hibernate, con mapeo objeto-relacional a las tablas de PostgreSQL.

El diseño de la base de datos es minimalista pero suficiente para las necesidades de autenticación y guardado del juego.

##### Tabla `users` (Mapeada por la entidad `com.almardev.rpgAPI.model.User`)

* **Propósito**: Almacena la información fundamental de cada usuario registrado que puede acceder al juego, siendo el punto de inicio para la autenticación y para vincular las partidas guardadas.
* **Columnas**:
    * `id` (BIGINT, Clave Primaria, Auto-generada): Identificador único y numérico para cada usuario, generado automáticamente por la base de datos al insertar un nuevo registro (`GenerationType.IDENTITY`).
    * `username` (VARCHAR, UNIQUE, NOT NULL): Nombre de usuario elegido por el jugador, único y no nulo, utilizado para el login y la identificación del usuario.
    * `password` (VARCHAR, NOT NULL): Contraseña del usuario, almacenada como un hash cifrado para garantizar la seguridad.
* **Relaciones**:
    * Un usuario (`User`) puede tener múltiples estados de partida guardados (`SaveState`). Esta relación se gestiona mediante una lista (`List<SaveState> saveStates`) dentro de la entidad `User`.
    * `@OneToMany(mappedBy = "user", cascade = CascadeType.ALL, orphanRemoval = true)`:
        * `mappedBy="user"`: Indica que la relación inversa es gestionada por el campo `user` en la entidad `SaveState`.
        * `cascade = CascadeType.ALL`: Las operaciones (persistir, actualizar, eliminar) sobre un `User` se "cascadean" a sus `SaveState` asociados. Por ejemplo, al eliminar un `User`, todos sus `SaveState` también se eliminarán automáticamente.
        * `orphanRemoval = true`: Si un `SaveState` se desvincula de su `User` (se convierte en un "huérfano"), se eliminará automáticamente de la base de datos.

##### Tabla `save_states` (Mapeada por la entidad `com.almardev.rpgAPI.model.SaveState`)

* **Propósito**: Almacena el estado completo de una partida guardada por un usuario en un slot específico. La flexibilidad del formato JSON permite una fácil evolución del esquema de guardado del juego sin requerir cambios en la base de datos.
* **Columnas**:
    * `id` (BIGINT, Clave Primaria, Auto-generada): Identificador único para cada estado de partida guardado.
    * `user_id` (BIGINT, NOT NULL, Clave Foránea a `users.id`): Campo que establece la relación Many-to-One con la tabla `users`, no puede ser nulo, asegurando que cada partida guardada pertenece a un usuario existente.
    * `slot` (INT, NOT NULL): Número del slot de guardado (1, 2, 3). Los usuarios pueden guardar su partida en uno de los tres slots disponibles.
    * `save_data` (VARCHAR/String, `columnDefinition = "jsonb"` con `@JdbcTypeCode(SqlTypes.JSON)`): Almacena el estado completo de la partida en formato JSON. El uso de `jsonb` en PostgreSQL optimiza el almacenamiento y permite consultas eficientes sobre el contenido JSON.
* **Restricciones de Unicidad**:
    * `@Table(name = "save_states", uniqueConstraints = {@UniqueConstraint(columnNames = {"user_id", "slot"})})`: Garantiza que no puede haber dos registros en la tabla `save_states` con el mismo `user_id` Y el mismo `slot`, asegurando que un usuario solo puede tener una partida guardada por cada slot disponible.
* **Relaciones**:
    * `@ManyToOne`: Indica que muchos `SaveState` pueden pertenecer a un solo `User`.
    * `@JoinColumn(name = "user_id", nullable = false)`: Especifica la columna (`user_id`) que se utiliza para unirse a la tabla `users`.

#### Sistema de seguridad (Spring Security & JWT)

El sistema de seguridad es un pilar fundamental del backend, protegiendo los recursos de la API y garantizando que solo los usuarios autenticados puedan acceder a sus datos de juego.
* **Framework principal**: Spring Security, que proporciona un marco de seguridad robusto y altamente configurable para aplicaciones Spring.
* **Autenticación sin estado (Stateless)**: La aplicación se configura para no mantener el estado de sesión en el servidor (`SessionCreationPolicy.STATELESS`), lo que es crucial para la escalabilidad de APIs REST, ya que cada petición HTTP debe incluir la información de autenticación (el token JWT).
* **Cifrado de contraseñas**:
    * Las contraseñas de los usuarios nunca se almacenan en texto plano en la base de datos.
    * `BCryptPasswordEncoder`: Se utiliza para aplicar una función hash unidireccional (BCrypt) a las contraseñas, haciéndolas seguras contra ataques de diccionario y de fuerza bruta, incluso si la base de datos es comprometida. El bean `PasswordEncoder` se define en `SecurityConfig.java`.
* **JSON Web Tokens (JWT)**:
    * **Propósito**: Son tokens compactos y autocontenidos que se utilizan para transmitir de forma segura información entre el cliente (Unity) y el servidor (API). Contienen los datos del usuario (`username`) y una firma digital para verificar su autenticidad.
    * **Generación (`JwtUtil.generateToken`)**: Tras un inicio de sesión exitoso, la API genera un JWT.
    Este token incluye:
        * El sujeto (`username`)
        * La fecha de emisión (`issuedAt`)
        * La fecha de expiración (`expiration`), configurada para 24 horas.
        * Una firma digital creada con la clave secreta del servidor (`JWT_SECRET_KEY`).
    * **Validación (`JwtUtil.validateTokenAndGetUsername`)**: Cuando una petición llega a un endpoint protegido, el filtro JWT valida la firma del token usando la misma clave secreta. Si la firma es válida y el token no ha expirado, se confía en la identidad del usuario.
    * **Clave secreta JWT (`JWT_SECRET_KEY`)**: Es la pieza central de la seguridad JWT. Se gestiona de forma segura a través de una variable de entorno en el entorno de despliegue (Render.com). `JwtUtil.java` está configurado para leer esta clave de `System.getenv("JWT_SECRET_KEY")`, garantizando que la clave nunca esté *hardcodeada* en el código fuente del repositorio público.
* **Filtro de autenticación JWT (`JwtAuthenticationFilter.java`)**:
    * Actúa como un interceptor en la cadena de filtros de Spring Security.
    * **Función**: Examina cada petición HTTP entrante. Si la cabecera `Authorization` contiene un token `Bearer`, lo extrae, lo valida utilizando `JwtUtil`, y si es válido, autentica al usuario en el contexto de seguridad de Spring (`SecurityContextHolder`). Esto permite que los controladores accedan a la información del usuario autenticado (ej., `Authentication authentication`) para realizar operaciones específicas de usuario.
* **Configuración de seguridad (`SecurityConfig.java`)**:
    * Define las reglas de acceso a los endpoints:
        * `requestMatchers("/auth/**").permitAll()`: Las rutas `/auth/register` y `/auth/login` (y cualquier otra bajo `/auth`) son accesibles por cualquier persona, sin necesidad de autenticación.
        * `anyRequest().authenticated()`: Todas las demás rutas de la API (`/game/save-states`) requieren que el usuario esté autenticado con un token JWT válido.
        * `csrf(csrf -> csrf.disable())`: Se deshabilita la protección CSRF (Cross-Site Request Forgery) porque es una API RESTful sin sesiones con estado, donde CSRF no es una preocupación típica.
        * `sessionManagement(...)`: Configura la política de sesión como `STATELESS`.
    * `addFilterBefore(jwtAuthenticationFilter, UsernamePasswordAuthenticationFilter.class)`: Inserta el filtro JWT personalizado al principio de la cadena de filtros de seguridad, antes del filtro de autenticación de nombre de usuario/contraseña estándar de Spring (que no se usa para las peticiones de token).

#### DTOs (Data Transfer Objects)

* `AuthRequest.java`:
    * **Propósito**: Se utiliza para recibir las credenciales de usuario (nombre de usuario y contraseña) desde el cliente Unity para las operaciones de registro y login.
    * **Campos**: `username` (String) y `password` (String).
    * **Validación (`jakarta.validation.constraints`)**: Utiliza anotaciones como `@NotBlank` (no puede ser nulo o vacío) y `@Size` (longitud mínima y máxima) para aplicar reglas de validación básicas directamente a nivel de DTO. Esto ayuda a filtrar entradas inválidas antes de que lleguen a la lógica de negocio.
    * **Lombok (`@Setter`, `@Getter`)**: Simplifica la generación automática de métodos *getter* y *setter* en tiempo de compilación, reduciendo el código *boilerplate*.
* `AuthResponse.java`:
    * **Propósito**: Se utiliza para enviar la respuesta de una autenticación (`/auth/login`) exitosa de vuelta al cliente Unity.
    * **Campo**: `token` (String), que contiene el JWT generado.
    * **Lombok (`@Getter`)**: Genera automáticamente los métodos *getter*.
* `SaveStateDTO.java`:
    * **Propósito**: Se utiliza para transferir información de una partida guardada (`SaveState`) entre la API y el cliente Unity.
    * **Campos**: `id` (Long, el ID de la partida guardada en la DB), `slot` (int, el número del slot de guardado), y `saveData` (String, el contenido JSON de la partida guardada).
    * **Lombok (`@Setter`, `@Getter`)**: Genera automáticamente los métodos *getter* y *setter*.

#### Controladores (Controllers)

* `AuthController.java`:
    * Anotado con `@RestController` y `@RequestMapping("/auth")`, lo que significa que maneja peticiones HTTP y todas sus rutas base comienzan con `/auth`.
    * `@Autowired private UserService userService;`: Inyecta el servicio `UserService` para delegar la lógica de negocio de autenticación.
    * `@PostMapping("/register")`:
        * Maneja peticiones POST a `/auth/register`.
        * `@Valid @RequestBody AuthRequest request`: Recibe un objeto `AuthRequest` del cuerpo JSON de la petición y aplica las validaciones definidas en `AuthRequest.java`.
        * Delega a `userService.registerUser()` para la creación del usuario.
        * Retorna `ResponseEntity.ok("User registered successfully")` en caso de éxito (HTTP 200)].
        * Captura `RuntimeException` (ej., "Username already taken") y devuelve `HttpStatus.BAD_REQUEST` (HTTP 400) con el mensaje de error.
    * `@PostMapping("/login")`:
        * Maneja peticiones POST a `/auth/login`.
        * `@Valid @RequestBody AuthRequest request`: Recibe el `AuthRequest` para login.
        * Delega a `userService.authenticateUser()` para autenticar al usuario y obtener el JWT.
        * Retorna `ResponseEntity.ok(new AuthResponse(token))` con el JWT en caso de éxito (HTTP 200).
        * Retorna `HttpStatus.UNAUTHORIZED` (HTTP 401) si las credenciales son inválidas o el token es nulo.
* `SaveStateController.java`:
    * Anotado con `@RestController` y `@RequestMapping("/game/save-states")`, gestionando las rutas para operaciones de guardado/carga.
    * `@Autowired private UserService userService;`: Inyecta el servicio `UserService` para interactuar con los datos de usuario y partidas guardadas.
    * `private final Gson gson = new Gson();`: Utiliza la librería Google Gson para serializar/deserializar objetos Java a/desde formato JSON. Esto es clave para manejar el campo `save_data` que almacena el estado del juego como JSON.
    * `@GetMapping`:
        * Maneja peticiones GET a `/game/save-states`.
        * `Authentication authentication`: Spring Security inyecta el objeto `Authentication`, que contiene los detalles del usuario autenticado (obtenido del JWT).
        * Obtiene el `username` del `authentication.getPrincipal()` (que es un `UserDetails`).
        * Recupera el `User` y sus `saveStates` asociados.
        * Mapea los `SaveState` a una `List<SaveStateDTO>` y los devuelve como `ResponseEntity.ok()`.
    * `@GetMapping("/{slot}")`:
        * Maneja peticiones GET a `/game/save-states/{slot}`, donde `{slot}` es un número de slot (1 a 3).
        * `@PathVariable int slot`: Captura el número del slot de la URL.
        * Valida que el slot esté entre 1 y 3.
        * Obtiene el usuario autenticado y filtra sus `saveStates` para encontrar el slot específico.
        * Devuelve el `SaveStateDTO` correspondiente (HTTP 200 OK) o `ResponseEntity.notFound().build()` (HTTP 404 Not Found) si el slot no tiene datos guardados para ese usuario.
    * `@PutMapping("/{slot}")`:
        * Maneja peticiones PUT a `/game/save-states/{slot}`.
        * Se usa para guardar o actualizar una partida.
        * `@RequestBody Map<String, Object> saveState`: Recibe el estado del juego como un mapa JSON.
        * Convierte el `Map` a un string JSON usando `gson.toJson(saveState)`.
        * Delega a `userService.saveOrUpdateSaveState()` para persistir el JSON en la base de datos.
        * Devuelve `ResponseEntity.ok("Game saved successfully in slot " + slot)` en caso de éxito.

#### Servicios (Services)

* `UserService.java`:
    * Anotado con `@Service`.
    * `@Autowired` inyecta `UserRepository`, `SaveStateRepository` y `PasswordEncoder`.
    * `registerUser(String username, String rawPassword)`:
        * Verifica si el nombre de usuario ya existe (`userRepository.findByUsername(username).isPresent()`).
        * Si es así, lanza `RuntimeException`.
        * Cifra la `rawPassword` con `passwordEncoder.encode()` y guarda el nuevo `User` en la base de datos.
    * `authenticateUser(String username, String rawPassword)`:
        * Busca el `User` por `username`.
        * Si no lo encuentra, lanza `UsernameNotFoundException`.
        * Verifica si la `rawPassword` coincide con la contraseña cifrada almacenada (`passwordEncoder.matches()`).
        * Si las credenciales son válidas, genera un `JwtUtil.generateToken()` y lo devuelve.
    * `findByUsername(String username)`: Recupera un `User` por su nombre de usuario.
    * `saveOrUpdateSaveState(User user, int slot, String saveData)`:
        * Busca un `SaveState` existente para el `user` y `slot` dados.
        * Si existe, actualiza su `saveData`.
        * Si no existe, crea un nuevo `SaveState`, lo asocia al `User` y lo guarda.
* `CustomUserDetailsService.java`:
    * Implementa la interfaz `UserDetailsService` de Spring Security.
    * `loadUserByUsername(String username)`:
        * Este método es llamado por Spring Security para cargar los detalles del usuario durante el proceso de autenticación.
        * Busca el usuario en `userRepository`. Si no lo encuentra, lanza `UsernameNotFoundException`.
        * Construye y devuelve un objeto `UserDetails` de Spring Security, utilizando el `username` y la contraseña cifrada del `User` de la base de datos, y asigna la autoridad "USER".

#### Repositorios (Repositories)

* `UserRepository.java`: Extiende `JpaRepository<User, Long>`.
    * Permite operaciones CRUD básicas sobre la entidad `User`.
    * `findByUsername(String username)`: Método personalizado para buscar un usuario por su nombre de usuario.
* `SaveStateRepository.java`: Extiende `JpaRepository<SaveState, Long>`. Permite operaciones CRUD básicas sobre la entidad `SaveState`.

#### Utilidades (JwtUtil)

* `JwtUtil.java`:
    * Clase estática para operaciones relacionadas con JWT.
    * `SECRET_KEY`: Lee la clave secreta de la variable de entorno `JWT_SECRET_KEY` para firmar y verificar tokens. Proporciona una lógica de *fallback* por si la variable no está definida (para desarrollo local).
    * `EXPIRATION_TIME`: Define la duración de validez de un JWT (24 horas).
    * `generateToken(String username)`: Crea un nuevo JWT.
    * `validateTokenAndGetUsername(String token)`: Valida un JWT y extrae el nombre de usuario de su contenido. Captura excepciones (`JwtException`) si el token es inválido.

### Frontend (Juego)

Es la capa de presentación que permite al jugador interactuar con el mundo, los menús y el sistema de combate, mientras se comunica con el backend para la gestión de datos persistentes.

#### Arquitectura general y organización

El proyecto de Unity sigue principios de diseño modular y utiliza el patrón Singleton para muchos de sus managers globales, lo que facilita el acceso a sistemas centrales desde cualquier parte del juego.
* **Motor**: Unity (versión con Universal Render Pipeline - URP)
* **Lenguaje**: C#

#### Sistemas principales del juego

##### Sistema de autenticación de usuarios

Permite a los jugadores registrarse y acceder al juego utilizando credenciales seguras, comunicándose directamente con la API REST.
* `LoginManager.cs`:
    * **Función**: Gestiona la UI y la lógica para que un usuario inicie sesión. Recopila el nombre de usuario y la contraseña.
    * **Comunicación con API**: Envía una petición POST a `/auth/login` con las credenciales.
    * **Gestión de token**: Si el login es exitoso, parsea la respuesta (un `LoginResponse` que contiene el JWT) y almacena el token en `PlayerPrefs` y en el `SessionManager`.
    * **UI Feedback**: Muestra mensajes de éxito o error.
    * **Robustez**: Incluye `request.certificateHandler = new CertificateHelper()` para ser más permisivo con certificados HTTPS (común en servicios gratuitos) y `request.timeout = 30` para manejar la "dormancia" de la API.
* `RegisterManager.cs`:
    * **Función**: Gestiona la UI y la lógica para que un nuevo usuario se registre. Recopila nombre de usuario, contraseña y su repetición.
    * **Validación local**: Realiza validaciones básicas de longitud y coincidencia de contraseñas antes de enviar la petición.
    * **Comunicación con API**: Envía una petición POST a `/auth/register` con las credenciales.
    * **Login post-registro**: Si el registro es exitoso, intenta iniciar sesión automáticamente.
    * **UI Feedback**: Muestra mensajes de éxito o error.
    * **Robustez**: Similar a `LoginManager`, incluye el `CertificateHelper` y el `timeout`.
* `SessionManager.cs`:
    * **Tipo**: Singleton global (`DontDestroyOnLoad`).
    * **Función**: Almacena y gestiona el token JWT del usuario actualmente autenticado. Proporciona métodos para obtener el token (`GetToken()`) y para invalidarlo (`Logout()`).
    * **Importancia**: Esencial para todas las peticiones autenticadas a la API (guardar, cargar, etc.).

##### Sistema de guardado y carga de partidas

Permite a los jugadores persistir su progreso en la base de datos en la nube y recuperarlo en cualquier momento.
* `PedestalSaveTrigger.cs`:
    * **Tipo**: `MonoBehaviour` (adjunto a un pedestal/objeto de guardado en el mundo).
    * **Función**: Detecta la interacción del jugador (`KeyCode.Return`) cuando está cerca del pedestal.
    * **Flujo de guardado**: Al interactuar, hace una petición GET a `/game/save-states` para obtener la lista de slots existentes, muestra la `SaveSlotPanelUI` en modo "Save", y al seleccionar un slot, llama a `SaveToSlot()`.
    * `SaveToSlot(int slot)`:
        * Recopila el estado actual del juego (`SaveStateBuilder.CreateSaveState()`).
        * Serializa este estado en un string JSON (`JsonUtility.ToJson()`).
        * Envía una petición PUT a `/game/save-states/{slot}` con el JSON en el cuerpo y el token JWT en el *header* `Authorization`.
        * Maneja la respuesta de la API (éxito o error).
* `LoadGameManager.cs`:
    * **Tipo**: Singleton global (`DontDestroyOnLoad`).
    * **Función**: Gestiona la lógica para cargar partidas guardadas desde la API.
    * **Flujo de Carga**: `WorkspaceAndShowLoadPanel()` hace una petición GET a `/game/save-states` para obtener la lista de slots guardados, muestra la `SaveSlotPanelUI` en modo "Load". Al seleccionar un slot, llama a `LoadGameFromSlot()`.
    * `LoadGameFromSlot(int slot)`:
        * Hace una petición GET a `/game/save-states/{slot}` con el token JWT.
        * Deserealiza el JSON recibido en un `SaveStateData` y lo almacena temporalmente en `TemporarySaveDataBuffer`.
        * Desencadena una transición de escena a la escena guardada (`SceneTransition.LoadScene()`).
        * Una vez que la nueva escena se carga (`OnSceneLoaded`), llama a `ApplySaveDataAfterSceneLoad()`.
    * `ApplySaveDataAfterSceneLoad()`:
        * Recupera el `SaveStateData` del buffer.
        * Reposiciona al jugador en la escena cargada (`player.transform.position`).
        * Actualiza el estado del `GameManager` (*party*, *stats*, equipo, habilidades).
        * Actualiza el `InventorySystem` (Gil, ítems).
* `SaveStateBuilder.cs`:
    * **Tipo**: Clase estática de utilidad.
    * **Función**: Recopila el estado actual de la partida del juego (posición del jugador, miembros del *party*, sus *stats*, equipo, habilidades aprendidas, inventario y Gil) y lo empaqueta en un objeto `SaveStateData` serializable a JSON.
* `SaveStateData.cs`:
    * **Tipo**: Clases serializables (`[System.Serializable]`) que definen la estructura de los datos de la partida (ej., `Vector3Data`, `CharacterSaveData`, `InventoryEntry`).
    * **Función**: Permiten a `JsonUtility` convertir objetos de C# en strings JSON y viceversa para la persistencia.
* `SaveStateDTOWrapper.cs`:
    * **Tipo**: Clases serializables (`[System.Serializable]`) para envolver listas de `SaveStateDTOs` recibidas de la API.
    * **Necesario**: Porque `JsonUtility` no puede deserializar directamente un *array* JSON de la raíz.
* `SaveSlotPanelUI.cs`:
    * **Tipo**: `MonoBehaviour` (UI Panel).
    * **Función**: Muestra la interfaz de usuario para seleccionar un slot de guardado o carga, mostrando el número del slot y una descripción si ya hay una partida guardada.
    * **Importancia**: Actúa como la interfaz visual para `PedestalSaveTrigger` y `LoadGameManager`.
* `TemporarySaveDataBuffer.cs`:
    * **Tipo**: Clase estática.
    * **Función**: Sirve como un *buffer* temporal para almacenar los datos de la partida cargada (`SaveStateData`) mientras se realiza la transición entre escenas.
    * **Importancia**: Permite que los datos estén disponibles después de que la escena antigua se descarga y la nueva se carga, antes de ser aplicados al estado del juego.

##### Control del jugador y movimiento

El jugador explora el mundo a través de un sistema de movimiento basado en el `Transform` y animaciones direccionales.
* `PlayerController.cs`:
    * **Tipo**: `MonoBehaviour` (adjunto al `GameObject` del jugador).
    * **Función**: Procesa la entrada del usuario (`Input.GetAxisRaw`) para el movimiento horizontal y vertical. Actualiza la posición del jugador (`transform.position`).
    * **Animación**: Controla el `Animator` del jugador para reproducir animaciones de caminar (`Walk_Left`, `Walk_Back`, `Walk_Forward`) y de inactividad (`Idle`) basándose en la dirección del movimiento y la última dirección.
    * **Bloqueo de movimiento**: Incluye el método `SetMovementLock(bool locked)` que detiene el movimiento del jugador y fuerza la animación de inactividad cuando es llamado por los managers de UI (ej., `StatusMenuController`, `NPCDialogue`).
    * **Encuentros aleatorios**: Registra la distancia recorrida (`distanceCoveredSinceLastStep`) y notifica al `EncounterManager` cuando se ha cubierto una `distancePerStepForEncounter`.
* `RunController.cs`:
    * **Tipo**: `MonoBehaviour`.
    * **Función**: Gestiona la mecánica de correr del jugador, aplicando un `runMultiplier` a la velocidad de movimiento cuando se presiona la tecla de correr.
    * **Importancia**: Añade una capa de exploración al juego.

##### Sistema de encuentros aleatorios y combate

El corazón de la jugabilidad del RPG, con un sistema de combate por turnos dinámico.
* `EncounterManager.cs`:
    * **Tipo**: Singleton global (`DontDestroyOnLoad`).
    * **Función**: Responsable de los encuentros aleatorios en el mapa. Recibe `RegisterStep()` del `PlayerController`. Calcula cuándo debe ocurrir el siguiente encuentro (`minStepsPerEncounter`, `maxStepsPerEncounter`).
    * **Trigger de combate**: Al activarse un encuentro, selecciona un `EnemyGroupData` aleatorio, prepara los datos para el combate en `CombatSessionData`, y solicita una transición a la escena de combate.
* `CombatEncounterTrigger.cs`:
    * **Tipo**: `MonoBehaviour` (adjunto a un *trigger* en el mundo).
    * **Función**: Permite configurar combates específicos (ej., batallas de jefe) que se activan al colisionar o interactuar con un `GameObject`.
    * **Importancia**: Utilizado para el encuentro con Garland (El jefe final)
* `CombatSceneInitializer.cs`:
    * **Tipo**: `MonoBehaviour` (específico de la escena de combate).
    * **Función**: Se ejecuta al inicio de la escena de combate. Instancia dinámicamente los `AllyWorldAnchor` y `EnemyWorldAnchor` para los personajes y enemigos, posicionándolos en la escena. Asigna el Canvas UI de combate a `DamageEffectsManager`.
    * **Importancia**: Prepara el escenario para la batalla.
* `BattleFlowController.cs`:
    * **Tipo**: Singleton (específico de la escena de combate).
    * **Función**: Orquesta la secuencia de una batalla por turnos. Inicia la fase de comandos (`StartCommandPhase()`). Recopila las acciones de los jugadores. Llama al `TurnManager.BeginTurnExecution()` para ejecutar el turno. Recibe el evento `OnTurnExecutionComplete` de `TurnManager`.
    * **Crucial**: Después de cada turno completado, verifica si el *party* o los *enemies* han sido derrotados. Si la batalla termina, notifica al `CombatEndManager`. Si no, reinicia la fase de comandos.
    * **Importancia**: Es el director de la orquesta de la batalla.
* `TurnManager.cs`:
    * **Tipo**: Singleton (específico de la escena de combate).
    * **Función**: Gestiona la secuencia de acciones dentro de un turno de combate.
    * **Orden de Acción**: Ordena las acciones planificadas (jugadores y enemigos) por agilidad.
    * **Ejecución**: Ejecuta cada `BattleAction` individualmente, esperando las animaciones y efectos.
    * **Efectos de Estado**: Aplica efectos de estado al final de cada acción (ej., daño por veneno, decrementar duración de estados).
    * **Eventos**: Dispara `OnTurnExecutionComplete` y `OnActionExecuted`.
    * **Importancia**: Es el motor que impulsa el combate por turnos.
* `BattleCommandUI.cs`:
    * **Tipo**: `MonoBehaviour` (UI Panel en combate).
    * **Función**: Presenta las opciones de comando al jugador activo (Atacar, Habilidad, Ítem, Defender, Huir). Gestiona la selección con un cursor y la activación de sub-menús.
    * **Importancia**: Principal interfaz de input del jugador durante su turno.
* `AbilitySelectorUI.cs`:
    * **Tipo**: `MonoBehaviour` (UI Panel en combate).
    * **Función**: Permite al jugador seleccionar una habilidad de su lista de habilidades aprendidas. Muestra el coste de MP y la descripción.
    * **Importancia**: Sub-menú para el comando "Ability".
* `ItemSelectorUI.cs`:
    * **Tipo**: `MonoBehaviour` (UI Panel en combate)
    * **Función**: Permite al jugador seleccionar un objeto de su inventario para usar en combate. Filtra los objetos válidos para combate.
    * **Importancia**: Sub-menú para el comando "Ítem" en combate.
* `TargetSelector.cs`:
    * **Tipo**: `MonoBehaviour` (UI Panel en combate).
    * **Función**: Gestiona la interfaz para seleccionar uno o varios objetivos (enemigos o aliados) para habilidades o ítems.
    * **Importancia**: Proporciona la interacción para seleccionar objetivos.
* `BattleUIFocusManager.cs`:
    * **Tipo**: Singleton (específico de la escena de combate).
    * **Función**: Gestiona el foco de la UI durante el combate para asegurar que solo una interfaz de usuario (comando, habilidad, ítem, target) esté activa y reciba input en un momento dado.
    * **Importancia**: Evita conflictos de input en la UI de combate.

##### Gestión de personajes y clases

Los personajes del jugador tienen atributos, clases y progresan a lo largo del juego.
* `CharacterStats.cs`:
    * **Tipo**: Clase serializable (`[System.Serializable]`).
    * **Función**: Define todos los atributos de un personaje (nombre, *job*, nivel, HP/MP actual y máximo, *stats*, equipo, habilidades aprendidas, estados alterados).
    * **Importancia**: Es la representación de datos de cada personaje.
* `CharacterClassData.cs`:
    * **Tipo**: `ScriptableObject`.
    * **Función**: Define las propiedades específicas de cada clase de personaje (*stats* base, crecimiento por nivel, animaciones asociadas, habilidades aprendibles).
    * **Importancia**: Permite balancear y extender fácilmente las clases.
* `ClassDatabase.cs`:
    * **Tipo**: Singleton.
    * **Función**: Carga y proporciona acceso a todos los `CharacterClassData` en el juego.
    * **Importancia**: Centraliza la gestión de datos de clases.
* `ExpCurve.cs`:
    * **Tipo**: `ScriptableObject`.
    * **Función**: Define la experiencia necesaria para alcanzar cada nivel.
    * **Importancia**: Controla la velocidad de progresión del personaje.
* `StatFormulaUtility.cs`:
    * **Tipo**: Clase estática de utilidad.
    * **Función**: Contiene fórmulas para calcular *stats* (HP, MP) y experiencia necesaria para el siguiente nivel.
    * **Importancia**: Centraliza las reglas de progresión y balance.
* `StarterKit.cs`:
    * **Tipo**: `ScriptableObject`.
    * **Función**: Define el equipo y las habilidades iniciales que un personaje recibe al ser creado con una clase específica.
    * **Importancia**: Simplifica la inicialización de nuevos personajes.

##### Sistema de habilidades

Los personajes y monstruos pueden usar habilidades con diversos efectos.
* `AbilityData.cs`:
    * **Tipo**: `ScriptableObject`.
    * **Función**: Define las propiedades de una habilidad (nombre, descripción, coste de MP, tipo, elemento, fórmula de daño/curación, VFX asociado, requisitos de viaje del personaje, sonido de ejecución).
    * **Importancia**: Datos de las habilidades.
* `AbilityExecutor.cs`:
    * **Tipo**: Clase estática de utilidad.
    * **Función**: Contiene la lógica para aplicar el efecto de una habilidad de personaje a sus objetivos (cálculo de daño/curación, aplicación de estados alterados).
    * **Importancia**: Motor de ejecución de habilidades de personajes.
* `MonsterAbilityData.cs`:
    * **Tipo**: `ScriptableObject`.
    * **Función**: Define las propiedades de una habilidad específica de monstruo.
    * **Importancia**: Datos de las habilidades de monstruos.
* `MonsterAbilityExecutor.cs`:
    * **Tipo**: Clase estática de utilidad.
    * **Función**: Contiene la lógica para aplicar el efecto de una habilidad de monstruo a sus objetivos.
    * **Importancia**: Motor de ejecución de habilidades de monstruos.

##### Sistema de inventario y objetos

Gestiona la colección de ítems del jugador y su equipamiento.
* `InventorySystem.cs`:
    * **Tipo**: Singleton global (`DontDestroyOnLoad`).
    * **Función**: Almacena todos los ítems del jugador, gestiona su cantidad (`inventoryItems`). Proporciona métodos para añadir, quitar, obtener la cantidad de ítems. También gestiona el Gil.
    * **Importancia**: Es el inventario central del juego.
* `ItemBase.cs`:
    * **Tipo**: Clase base para todos los ítems del juego.
    * **Función**: Define propiedades comunes a todos los ítems (ID, nombre, descripción, precio, icono).
    * **Importancia**: Proporciona una jerarquía para los ítems.
* `ConsumableItem.cs`:
    * **Tipo**: `ScriptableObject` (hereda de `ItemBase`).
    * **Función**: Define ítems que se pueden consumir (restaurar HP/MP, revivir, curar estados). Contiene la lógica `ApplyEffect` para aplicar sus efectos a un `CharacterStats`.
    * **Importancia**: Define la funcionalidad de pociones, éteres, etc..
* `EquipmentItem.cs`:
    * **Tipo**: Clase base para ítems que se pueden equipar (hereda de `ItemBase`).
    * **Función**: Define propiedades comunes a todo el equipo (tipo de slot, trabajos permitidos, bonificaciones de *stats*).
    * **Importancia**: Base para armas y armaduras.
* `WeaponItem.cs`:
    * **Tipo**: `ScriptableObject` (hereda de `EquipmentItem`).
    * **Función**: Define propiedades específicas de armas (tipo de arma, poder de ataque).
    * **Importancia**: Define las armas del juego.
* `ArmorItem.cs`:
    * **Tipo**: `ScriptableObject` (hereda de `EquipmentItem`).
    * **Función**: Define propiedades específicas de armaduras (tipo de armadura, poder de defensa).
    * **Importancia**: Define las armaduras del juego.
* `ItemDatabase.cs`:
    * **Tipo**: Singleton.
    * **Función**: Carga y proporciona acceso a todos los `ItemBase` (consumibles, armas, armaduras) desde Resources.
    * **Importancia**: Centraliza la gestión de datos de ítems.
* `EquipmentUtils.cs`:
    * **Tipo**: Clase estática de utilidad.
    * **Función**: Proporciona métodos para mapear tipos de `EquipmentItem` a `EquipmentSlots` (ej., saber si una espada va en `RightHand`).
    * **Importancia**: Ayuda a la lógica de equipamiento.

##### Recompensas de batalla

Gestiona la distribución de experiencia, Gil y objetos después de una victoria en combate.
* `BattleRewardProcessor.cs`:
    * **Tipo**: Singleton.
    * **Función**: Calcula la experiencia total y el Gil de los enemigos derrotados. Distribuye la EXP entre los miembros vivos del *party*. Calcula y añade los ítems obtenidos al inventario del jugador.
    * **Importancia**: Componente principal del sistema de recompensas

##### Música y sonido

* `MusicManager.cs`:
    * **Tipo**: Singleton global (`DontDestroyOnLoad`).
    * **Función**: Es el gestor central de toda la música de fondo del juego. Permite reproducir música de mapa, batalla, victoria, derrota y la pista especial de agradecimiento, con transiciones suaves (*fades*) entre ellas.
    * **Importancia**: Crucial para la ambientación del juego.

##### Transiciones de escena

* `SceneTransition.cs`:
    * **Tipo**: Singleton global (`DontDestroyOnLoad`).
    * **Función**: Proporciona transiciones visuales (una pantalla negra que se cierra y abre como un telón) al cargar nuevas escenas. Gestiona diferentes contextos de transición (genérico, a combate, desde combate) y puede ejecutar *callbacks* en puntos intermedios.
    * **Importancia**: Mejora la experiencia de usuario al cambiar de escena.

##### Gestión de UI y foco

* `BattleUIFocusManager.cs`:
    * **Tipo**: Singleton (específico de la escena de combate).
    * **Función**: Gestiona el foco de input entre los diferentes elementos de la UI de combate para evitar conflictos y asegurar que solo el menú activo reciba comandos.
    * **Importancia**: Esencial para la usabilidad en combate.
* `DamageEffectsManager.cs`:
    * **Tipo**: Singleton global (`DontDestroyOnLoad`).
    * **Función**: Orquesta la visualización de efectos en el mundo para el feedback de daño o curación. Instancia textos flotantes (números de daño/curación) y VFX sobre los objetivos.
    * **Importancia**: Feedback visual crítico para el combate.
* `FloatingDamageText.cs`:
    * **Tipo**: `MonoBehaviour` (adjunto al *prefab* de texto flotante).
    * **Función**: Controla la animación y el comportamiento de los números de daño/curación que aparecen sobre los personajes/enemigos. Se fija al objeto en el mundo, crece de pequeño a grande, se eleva y se desvanece.
    * **Importancia**: Componente visual clave para el feedback de combate.
* `ThankYouScreenUI.cs`:
    * **Tipo**: Singleton (específico de la escena de combate, pero persiste para la transición).
    * **Función**: Gestiona la pantalla final de agradecimiento de la demo. Muestra un texto de *lore* con *autoscroll* y velocidad acelerada por input, reproduce una música específica, luego muestra un mensaje de agradecimiento final y coordina la transición de salida de la demo (*fade* a negro y regreso al menú principal con limpieza de sesión).
    * **Importancia**: Mensaje de agradecimiento y fin de la demo.
* `GameOverPanel.cs`:
    * **Tipo**: Singleton (específico de la escena de combate).
    * **Función**: Muestra la pantalla de *Game Over* cuando el *party* del jugador es derrotado en combate. Permite al jugador cargar una partida guardada o salir al menú principal, limpiando la sesión.
    * **Importancia**: Gestiona el fin de la partida por derrota.

#### ScriptableObjects clave

Los `ScriptableObjects` son activos de Unity que permiten almacenar grandes cantidades de datos reutilizables y configurables fuera de las instancias de `MonoBehaviour`.

* `AbilityData`, `ArmorItem`, `CharacterClassData`, `EnemyData`, `EnemyGroupData`, `ExpCurve`, `ItemBase`, `MonsterAbilityData`, `StarterKit`, `WeaponItem`.
* **Importancia**: Facilitan el diseño de contenido, el balanceo del juego y la gestión de datos sin necesidad de recompilar código.


### Integración Frontend-Backend (Unity y Spring Boot API)

* **Tecnología de peticiones**: `UnityWebRequest` en C# para el frontend-
* **Formato de datos**: JSON se utiliza para el intercambio de datos entre Unity y la API. `JsonUtility.ToJson()` y `JsonUtility.FromJson()` en Unity serializan/deserializan DTOs de C# a/desde JSON, mientras que Gson en Spring Boot hace lo mismo en el backend.
* **Autenticación**:
    * Unity envía `AuthRequest` (*username*/`password`) a `/auth/register` o `/auth/login` (POST).
    * La API responde con un JWT (`AuthResponse`) si el login es exitoso.
    * Unity almacena el JWT en `SessionManager` y `PlayerPrefs`.
    * Para peticiones subsiguientes a endpoints protegidos (ej., guardar/cargar partidas), Unity adjunta el JWT en la cabecera `Authorization: Bearer <token>`.
* **Guardado y carga de Partidas**:
    * Unity serializa el estado del juego (`SaveStateData`) a JSON.
    * Envía el JSON con una petición PUT a `/game/save-states/{slot}` (para guardar).
    * Hace una petición GET a `/game/save-states` o `/game/save-states/{slot}` para obtener datos de partidas guardadas.
    * La API persiste/recupera el JSON en el campo `save_data` de la tabla `save_states`.
* **Manejo de HTTPS y dormancia**:
    * `CertificateHelper.cs`: Unity utiliza un `CertificateHandler` permisivo para aceptar certificados HTTPS de servicios en la nube sin problemas de validación estricta, común en entornos de desarrollo/demo.
    * `request.timeout = 30`: Se ha aumentado el tiempo de espera de las peticiones en Unity para permitir que los servicios gratuitos en la nube (Render.com, Neon.tech) se "despierten" de la dormancia antes de que la petición expire, evitando errores de "Server unreachable".

### Despliegue y mantenimiento

El proyecto está diseñado para ser completamente funcional y accesible de forma independiente, sin necesidad de ejecutar componentes en la máquina local.
* **Base de datos (Neon.tech)**:
    * **Alojamiento**: Instancia de PostgreSQL en Neon.tech (AWS eu-central-1).
    * **Conexión**: La API se conecta usando una *Connection String* segura inyectada vía variables de entorno.
    * **Mantenimiento**: La "Compute" se pausa automáticamente por inactividad, conservando horas gratuitas. Se "despierta" en la primera petición.
* **API (Render.com)**:
    * **Alojamiento**: Servicio web de Spring Boot en Render.com (región de Frankfurt para cercanía a la DB).
    * **Despliegue continuo**: Integración con GitHub para despliegues automáticos al hacer `git push`.
    * **Variables de entorno**: las credenciales de la base de datos y la clave JWT se gestionan de forma segura como variables de entorno en Render.com.
    * **Contenerización**: Utiliza un Dockerfile para definir un entorno de ejecución Java 17 robusto y ligero (basado en `openjdk:17-jdk-slim` para *build* y `eclipse-temurin:17-jre-focal` para *runtime*).
    * **Mantenimiento**: El servicio se "duerme" por inactividad y se "despierta" en la primera petición, ideal para ahorrar horas en una demo.

## 3. Guía de instalación

Para descargar el juego tendrás que seguir los pasos de mi página de itch.io

[https://almarquezdev.itch.io/final-fantasy-1-hd-2d-remake]

Para poder acceder es necesario una contraseña, esto es así para evitar que otros usuarios se puedan descargar el proyecto (y saturar de esta forma los servicios gratuitos de Neon y Render, ya que necesito que funcionen para la defensa del TFG)

## 4. Guía de uso

### Acceso al Juego

1.  **Ejecutable**: Si tienes un ejecutable del juego, simplemente haz doble clic en él.

### Registro y Login

1.  En la pantalla de inicio, selecciona "Register" para crear una nueva cuenta. Ingresa un nombre de usuario y una contraseña.
2.  Si ya tienes una cuenta, selecciona "Login" e ingresa tus credenciales.
3.  Al iniciar sesión con éxito, accederás al mundo del juego.

### Exploración y combate

1.  **Movimiento**: Usa las flechas del teclado o `W`, `A`, `S`, `D` para mover a tu personaje.
2.  **Correr**: Mantén presionada la tecla `Shift` para correr.
3.  **Encuentros aleatorios**: Al explorar el mapa, te encontrarás con enemigos de forma aleatoria, lo que te llevará a la escena de combate.
4.  **Combate por turnos**:
    * Selecciona las acciones de tu personaje (Atacar, Habilidad, Ítem, Defender, Huir) usando el cursor de la UI.
    * Selecciona los objetivos para tus ataques o habilidades.
    * Observa cómo se desarrolla el turno según la agilidad de los combatientes.
    * Si ganas, recibirás experiencia y Gil.

### Guardar y cargar Partida

1.  **Guardar**: Busca los pedestales de guardado en el mundo. Interactúa con ellos (`Enter`) para abrir el menú de guardado. Elige uno de los tres slots disponibles para guardar tu progreso.
2.  **Cargar**: Desde el menú principal del juego o en la pantalla de game over, selecciona "Load Game". Elige el slot de guardado que deseas cargar para restaurar tu partida.

## 5. Enlaces de interés

### Enlace a la API

[https://github.com/AlMarquezDev/rpgAPI]

### Enlace a la página del juego

[https://almarquezdev.itch.io/final-fantasy-1-hd-2d-remake]

## 6. Conclusión

El proyecto ha logrado los objetivos del TFG de demostrar un juego RPG 2D-HD por turnos con sistemas de juego clave y una integración cliente-servidor robusta y desplegada en la nube.

La implementación de sistemas de combate, personajes, inventario y progresión se ha realizado con éxito, al igual que la integración con una API RESTful para autenticación y persistencia de datos, así como el despliegue completo del backend (API y base de datos) en plataformas gratuitas en la nube como Render.com y Neon.tech.

Además, se ha demostrado un manejo efectivo de las limitaciones inherentes a los servicios gratuitos (como la dormancia y los *timeouts*), asegurando una experiencia funcional para el usuario.

Este TFG no solo valida los conocimientos adquiridos en el ciclo DAM, sino que también sirve como una base sólida para futuras expansiones y mejoras.

## 7. Contacto

* **Nombre**: [Alberto Márquez Flores]
* **Email**: [al.marquez.dev@gmail.com]
* **LinkedIn**: [https://www.linkedin.com/in/almarquezdev/]
* **GitHub**: [https://github.com/AlMarquezDev]