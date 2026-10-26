# RestauranteMVC (versión beta)

## Descripción del proyecto
Sistema de gestión para un restaurante desarrollado en **ASP.NET Core MVC (.NET 10)** y **Entity Framework Core** con SQL Server. Permite administrar categorías, platos, mesas, cargos, empleados y pedidos, y consultar reportes de ventas. Es una versión beta ligera (MVP) pensada para el uso diario de un restaurante pequeño.

Flujo principal: **abrir pedido en una mesa libre → agregar platos → marcar como servido → cobrar**. El total se calcula solo y la mesa pasa a *Ocupada* / *Libre* automáticamente.

## Integrantes del equipo
Lucas Boris Gomez Giles — GitHub: Its.Lu06

## Tecnologías
* ASP.NET Core MVC (.NET 10) y Razor
* Entity Framework Core con SQL Server
* Bootstrap 5.3, CSS propio y JavaScript básico
* Tipografías Inter y Fraunces, e iconos SVG incrustados (sin librerías de iconos externas)

## Requisitos previos
* .NET SDK 10.0
* SQL Server (LocalDB o Express)
* Visual Studio con la carga de trabajo de ASP.NET (o `dotnet` por consola)

## Cómo ejecutar
1. Clona el repositorio y abre `RestauranteMVC.slnx`.
2. Revisa la cadena de conexión `RestauranteDB` en `appsettings.json` (por defecto usa LocalDB).
3. Ejecuta con `F5`. Al iniciar, la aplicación **aplica las migraciones sola** (no hace falta `Update-Database`) y, en entorno *Development*, **carga datos de ejemplo** (cargos, menú boliviano, 8 mesas y 3 empleados) solo si las tablas están vacías.

## Módulos
* **Tablero:** indicadores del día (mesas libres, pedidos en curso, total cobrado y platos disponibles), tarjetas de mesas con acción directa («Abrir pedido» o «Ver pedido») y lista de pedidos en curso.
* **Pedidos:** líneas de detalle con cantidades (botones + / −), total automático, filtros por estado y estados *En preparación → Servido → Pagado* o *Cancelado*. Confirmación antes de cobrar o cancelar.
* **Mesas:** número, capacidad y estado (*Libre*, *Ocupada*, *Reservada*), con número de mesa único.
* **Platos:** filtro por categoría y cambio rápido entre *Disponible* y *Agotado* desde la lista.
* **Categorías, Empleados y Cargos:** CRUD con validaciones en español y detección de nombres duplicados.
* **Protección del historial:** no permite eliminar mesas, empleados, cargos, categorías o platos que tengan registros asociados, y explica el motivo. Los pedidos pagados no se pueden eliminar.

## Reportes
1. **Ventas por fecha:** rango de fechas; muestra total cobrado (pedidos pagados), por cobrar (en curso), cantidad de pedidos, ticket promedio, gráfico de barras de lo cobrado por día y detalle de pedidos. Los cancelados no cuentan.
2. **Platos más vendidos:** ranking por unidades vendidas y dinero generado, con barras de popularidad y filtro opcional de fechas. Los cancelados no cuentan.

## Interfaz y experiencia de usuario
* Diseño moderno de aplicación web: barra lateral oscura con acento terracota y ámbar, fondo cálido y tarjetas limpias.
* Totalmente responsiva: en móvil el menú lateral se despliega con un botón y las tablas se apilan en tarjetas.
* Insignias de color para los estados de pedidos y mesas.
* Mensajes de éxito y error visibles, y validaciones de formularios en español (cliente y servidor).
* Páginas amigables para errores 404 y 500.
* Protección contra doble clic al enviar formularios.
* Montos mostrados en bolivianos con formato local (Bs 1.250,50).

## Pendientes recomendados (siguiente iteración)
* Guardar el **precio unitario** en cada línea del pedido (hoy el total usa el precio actual del plato; si cambias un precio, los pedidos aún abiertos se recalculan).
* Cambiar los borrados en cascada de la BD a `Restrict` (hoy el sistema lo impide desde los controladores).
* Autenticación y roles (administrador / mesero / cajero).