# EMarketProyecto (MercaUca) — AI-Optimized Context & Documentation

> **AI Agent Directive (Token Optimization):** Este documento contiene el mapa arquitectónico completo del proyecto. Úsalo para ir directamente a los archivos relevantes sin hacer búsquedas recursivas ni inspeccionar `node_modules/`, `bin/`, `obj/` o `.vs/`.

---

## 1. Resumen del Proyecto y Stack

**MercaUca** es una plataforma web de comercio electrónico universitario (compra/venta de productos, carrito de compras, autenticación JWT y verificación por correo SMTP).

| Capa | Tecnología | Ruta Raíz | Puerto Local |
| :--- | :--- | :--- | :--- |
| **Frontend (SPA)** | React 19, Vite 7, React Router DOM 7, CSS nativo | `EMarketProyecto/mi-app/` | `http://localhost:5173` |
| **Backend (API REST)** | ASP.NET Core 8.0 Web API, EF Core 9 (SQL Server), JWT Bearer, Swagger | `EMarketProyecto/ApiWebMarket/ApiWebMarket/` | `http://localhost:5048` / `https://localhost:7044` |
| **Base de Datos** | SQL Server (`MercaUcaContext` - Database First) | `EMarketProyecto/DB/` | SQL Server (según `appsettings.json`) |

---

## 2. Mapa de Directorios Clave

```text
EMarketProyecto/
├── ApiWebMarket/
│   ├── ApiWebMarket.sln
│   └── ApiWebMarket/
│       ├── Program.cs                        # Configuración DI, CORS, JWT, DbContext, Swagger
│       ├── appsettings.json                  # ConnectionStrings ("MercaUcaContext") y config "Jwt"
│       ├── Controllers/
│       │   ├── UsuariosController.cs         # Registro, Login (JWT), Perfil, Verify
│       │   ├── VerificacionController .cs    # Envío y validación de código OTP por email (SMTP)
│       │   ├── ProductosController.cs        # Creación de productos (Multipart/Form-Data -> byte[])
│       │   ├── NovedadesController.cs        # Listado de novedades, slider e info de usuarios
│       │   ├── SearchController.cs           # Búsqueda de productos por prefijo (case-insensitive)
│       │   └── CarritoController.cs          # CRUD carrito + checkout con recibo HTML por SMTP
│       ├── DTO/                              # Modelos de entrada/salida (Requests, Responses, Cards)
│       ├── Models/                           # Entidades EF Core y MercaUcaContext.cs
│       └── VirtualTables/                    # Store en memoria para códigos OTP de verificación
├── DB/
│   ├── BaseDeDAtos.sql                       # Script DDL/DML de la base de datos
│   ├── MercaUca.bacpac                       # Backup exportado de SQL Server
│   └── PruebaSlider.sql                      # Datos de prueba para el slider
└── mi-app/
    ├── package.json
    ├── vite.config.js
    └── src/
        ├── main.jsx                          # Punto de entrada React + BrowserRouter
        ├── App.jsx                           # Definición de rutas y layout condicional
        ├── hooks/
        │   └── useProductos.js               # Custom hook para obtener productos/novedades
        ├── pages/
        │   └── ProductosPage.jsx             # Contenedor de catálogo en Novedades
        ├── utils/
        │   └── img.js                        # Utilidades para manejo/formateo de imágenes Base64
        └── components/
            ├── Header/Topbar.jsx             # Barra superior, buscador, acceso a carrito y perfil
            ├── Navs/Navigation.jsx           # Barra de navegación secundaria
            ├── Login/                        # Login.jsx, Registro.jsx, Login.css
            ├── Productos/                    # Producto_grid.jsx, Producto_card.jsx, Producto_detalle.jsx
            ├── Carrito/carrito.jsx           # Drawer/Modal del carrito de compras y checkout
            ├── Vender/                       # AgregarProducto.jsx, productoService.js
            ├── CV_Card/ProfilePopup.jsx      # Popup de perfil de usuario autenticado
            ├── pages/Novedades.jsx           # Banner/Slider de novedades
            └── Footer/footer.jsx             # Pie de página
```

---

## 3. Mapa de Endpoints del Backend (`ApiWebMarket`)

Base URL local: `http://localhost:5048`

### `UsuariosController` (`/api/usuarios`)
| Método | Ruta | Auth | DTO / Parámetros | Descripción |
| :--- | :--- | :---: | :--- | :--- |
| `POST` | `/api/usuarios/registro` | No | `Body: RegisterRequest` | Registra usuario con `PasswordHasher<Usuario>`. Rol default: `CMP`, Estado: `ACT`. |
| `POST` | `/api/usuarios/login` | No | `Body: LoginRequest` | Valida credenciales y retorna `AuthResponse` (Token JWT + datos de usuario). |
| `GET` | `/api/usuarios/{idUsuario}` | No | `Route: idUsuario` | Obtiene datos públicos del usuario por su ID. |
| `GET` | `/api/usuarios/perfil` | **JWT** | Header `Authorization` | Retorna perfil del usuario extraído de los claims del JWT. |
| `GET` | `/api/usuarios/verify` | **JWT** | Header `Authorization` | Valida si el token JWT actual es válido (`200 OK`). |

### `VerificacionController` (`/api/verificacion`)
| Método | Ruta | Auth | DTO / Parámetros | Descripción |
| :--- | :--- | :---: | :--- | :--- |
| `POST` | `/api/verificacion/enviar-codigo` | No | `Body: EnviarCodigoRequestcs` | Genera código OTP (5 min validez en `ICodigosVerificacionStore`) y lo envía por Gmail SMTP. |
| `POST` | `/api/verificacion/verificar-codigo` | No | `Body: VerificarCodigoRequest` | Verifica y consume el código OTP asociado al correo. |

### `ProductosController` (`/api/Productos`)
| Método | Ruta | Auth | DTO / Parámetros | Descripción |
| :--- | :--- | :---: | :--- | :--- |
| `POST` | `/api/Productos` | No | `Form: CrearProductoRequest` | Crea registro en `Producto` (imagen en `byte[]`), `ProductoMedium` (`Novedades=true`) y `VariantesProducto`. Genera ID corto en Base64 URL-safe (22 chars). |

### `NovedadesController` (`/api/Novedades`)
| Método | Ruta | Auth | DTO / Parámetros | Descripción |
| :--- | :--- | :---: | :--- | :--- |
| `GET` | `/api/Novedades/novedades` | No | - | Lista productos con `ProductoMedia.Novedades == true` como `Producto_card` (incluye `FotoBase64`). |
| `GET` | `/api/Novedades/productosSlider` | No | - | Retorna imágenes en Base64 de productos cuyo `IdVendedor == "ProductoMediaSlider"`. |
| `GET` | `/api/Novedades/AllUsers` | No | - | Lista todos los usuarios. |
| `GET` | `/api/Novedades/{id}/nombre` | No | `Route: id` | Retorna `{ Nombre }` del usuario. |
| `GET` | `/api/Novedades/{id}/email` | No | `Route: id` | Retorna `{ Email }` del usuario. |

### `SearchController` (`/api/Search`)
| Método | Ruta | Auth | DTO / Parámetros | Descripción |
| :--- | :--- | :---: | :--- | :--- |
| `GET` | `/api/Search/buscar` | No | `Query: ?query=texto` | Busca productos donde `TituloProducto` inicia con `query` (case-insensitive). Retorna `ProductoBusquedaDto[]`. |

### `CarritoController` (`/api/Carrito`) — **Requiere `[Authorize]` (JWT) en todo el controlador**
| Método | Ruta | Auth | DTO / Parámetros | Descripción |
| :--- | :--- | :---: | :--- | :--- |
| `GET` | `/api/Carrito/{usuario}` | **JWT** | `Route: usuario` | Lista ítems del carrito del comprador (`CarritoItemDto[]`). |
| `GET` | `/api/Carrito/cantidad-articulos/{idUsuario}` | **JWT** | `Route: idUsuario` | Retorna `int` con la cantidad de artículos en el carrito. |
| `POST` | `/api/Carrito/agregar-articulo` | **JWT** | `Body: AgregarArticuloCarritoDto` | Crea carrito si no existe y agrega el artículo (valida duplicados). |
| `POST` | `/api/Carrito/finalizar-compra/{idUsuario}` | **JWT** | `Route: idUsuario` | Envía factura HTML por correo SMTP al usuario y vacía/elimina su carrito. |
| `DELETE` | `/api/Carrito/eliminar-articulo/{idUsuario}/{idProducto}` | **JWT** | `Route: idUsuario, idProducto` | Elimina un producto específico del carrito del usuario. |

---

## 4. Rutas y Flujo del Frontend (`mi-app`)

Definidas en `EMarketProyecto/mi-app/src/App.jsx`:

- `/` → Redirige a `/Novedades`.
- `/Novedades` → Renderiza `<Novedades />` (slider), `<PaginaNovedades />` (grid de productos) y `<Footer />`.
- `/vender` → Renderiza `<AgregarProductos />` (formulario multipart para publicar producto).
- `/producto/:id` → Renderiza `<ProductDetail />` (vista detallada y botón de agregar al carrito; oculta layout global).
- `/Login` → Renderiza `<LoginPage />` (oculta layout global).
- `/Login/Registro` → Renderiza `<RegistroPage />` (registro con verificación de correo; oculta layout global).

**Layout condicional:** `<Topbar />`, `<Navigation />` y `<Cart />` se muestran en todas las vistas **excepto** cuando la ruta inicia con `/login`, `/login/registro` o `/producto/`.

---

## 5. Modelo de Datos Principal (`MercaUcaContext`)

Entidades principales en `EMarketProyecto/ApiWebMarket/ApiWebMarket/Models/`:
- **Usuarios y Seguridad:** `Usuario`, `Role`, `EstadoUsuario`, `Direccione`, `Pai`.
- **Catálogo:** `Producto` (almacena imagen en `byte[] Foto`), `ProductoMedium` (flags de galería/novedades), `VariantesProducto` (talla, color, stock), `Categorium`, `CondicionProducto`, `Review`.
- **Carrito y Órdenes:** `Carrito`, `ArticulosCarrito`, `Orden`, `ArticulosOrden`, `EstadosOrden`, `EstadoArticulosOrden`, `LineaEnvioDireccion`.
- **Pagos:** `Payment`, `EstadoPayment`, `ProveedorPayment`, `Payout`, `PayoutEstado`, `MetodoAbono`.

---

## 6. Guía Rápida de Ejecución

1. **Base de Datos:** Importar `EMarketProyecto/DB/MercaUca.bacpac` o ejecutar `BaseDeDAtos.sql` en SQL Server y ajustar la cadena `ConnectionStrings:MercaUcaContext` en `appsettings.json`.
2. **Backend (.NET 8):**
   ```bash
   cd EMarketProyecto/ApiWebMarket/ApiWebMarket
   dotnet run
   ```
   *(Para exponer con Ngrok: `ngrok http 5048`)*
3. **Frontend (React + Vite):**
   ```bash
   cd EMarketProyecto/mi-app
   npm install
   npm run dev
   ```
   *(Configurar `VITE_API_URL` en `mi-app/.env.local` apuntando a `http://localhost:5048` o a la URL de Ngrok).*
