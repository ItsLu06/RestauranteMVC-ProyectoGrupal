using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestauranteMVC.Data;
using RestauranteMVC.Models;

namespace RestauranteMVC.Controllers;

public class PedidosController : Controller
{
    private readonly RestauranteDbContext _context;

    public PedidosController(RestauranteDbContext context)
    {
        _context = context;
    }

    // ---------- LISTA ----------
    // filtro: "activos" (por defecto), "todos" o cualquier estado exacto.
    public async Task<IActionResult> Index(string? filtro)
    {
        filtro = string.IsNullOrWhiteSpace(filtro) ? "activos" : filtro;

        var consulta = _context.Pedidos.Include(p => p.Mesa).Include(p => p.Empleado).AsQueryable();

        if (filtro == "activos")
        {
            consulta = consulta.Where(p => p.Estado == EstadosPedido.EnPreparacion || p.Estado == EstadosPedido.Servido);
        }
        else if (filtro != "todos")
        {
            consulta = consulta.Where(p => p.Estado == filtro);
        }

        ViewBag.Filtro = filtro;
        return View(await consulta.OrderByDescending(p => p.FechaHora).ToListAsync());
    }

    // ---------- DETALLE DEL PEDIDO (aquí se agregan los platos) ----------
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var pedido = await CargarPedidoAsync(id.Value);
        if (pedido == null) return NotFound();

        await CargarPlatosAsync();
        return View(pedido);
    }

    // ---------- CREAR ----------
    public async Task<IActionResult> Create(int? mesaId)
    {
        await CargarCombosAsync(mesaId, null);
        return View(new Pedido { MesaId = mesaId ?? 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MesaId,EmpleadoId")] Pedido pedido)
    {
        var mesa = await _context.Mesas.FindAsync(pedido.MesaId);
        if (pedido.MesaId > 0 && mesa == null)
        {
            ModelState.AddModelError(nameof(Pedido.MesaId), "La mesa seleccionada no existe.");
        }
        else if (mesa != null && mesa.Estado == EstadosMesa.Ocupada)
        {
            ModelState.AddModelError(nameof(Pedido.MesaId), $"La mesa {mesa.NumeroMesa} ya está ocupada. Agrega los platos a su pedido abierto.");
        }

        if (pedido.EmpleadoId > 0 && !await _context.Empleados.AnyAsync(e => e.Id == pedido.EmpleadoId))
        {
            ModelState.AddModelError(nameof(Pedido.EmpleadoId), "El empleado seleccionado no existe.");
        }

        if (!ModelState.IsValid || mesa == null)
        {
            await CargarCombosAsync(pedido.MesaId, pedido.EmpleadoId);
            return View(pedido);
        }

        // La fecha, el estado y el total los decide el sistema, no el formulario.
        pedido.FechaHora = DateTime.Now;
        pedido.Estado = EstadosPedido.EnPreparacion;
        pedido.Total = 0;
        mesa.Estado = EstadosMesa.Ocupada;

        _context.Add(pedido);
        await _context.SaveChangesAsync();

        TempData["Ok"] = $"Pedido abierto para la mesa {mesa.NumeroMesa}. Agrega los platos.";
        return RedirectToAction(nameof(Details), new { id = pedido.Id });
    }

    // ---------- EDITAR (quién atiende y estado) ----------
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var pedido = await CargarPedidoAsync(id.Value, soloCabecera: true);
        if (pedido == null) return NotFound();

        await CargarEmpleadosAsync(pedido.EmpleadoId);
        ViewBag.Estados = new SelectList(EstadosPedido.Todos, pedido.Estado);
        return View(pedido);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, int empleadoId, string estado)
    {
        var pedido = await _context.Pedidos.Include(p => p.Mesa).Include(p => p.Empleado).FirstOrDefaultAsync(p => p.Id == id);
        if (pedido == null) return NotFound();

        if (!await _context.Empleados.AnyAsync(e => e.Id == empleadoId))
        {
            ModelState.AddModelError("empleadoId", "Selecciona quién atiende el pedido.");
        }
        if (!EstadosPedido.Todos.Contains(estado))
        {
            ModelState.AddModelError("estado", "El estado no es válido.");
        }
        else if (estado == EstadosPedido.Pagado && !await _context.DetallePedidos.AnyAsync(d => d.PedidoId == id))
        {
            ModelState.AddModelError("estado", "No se puede marcar como pagado un pedido sin platos.");
        }

        if (!ModelState.IsValid)
        {
            await CargarEmpleadosAsync(empleadoId);
            ViewBag.Estados = new SelectList(EstadosPedido.Todos, estado);
            return View(pedido);
        }

        pedido.EmpleadoId = empleadoId;
        pedido.Estado = estado;
        await _context.SaveChangesAsync();
        await SincronizarMesaAsync(pedido.MesaId);

        TempData["Ok"] = "Pedido actualizado.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---------- LÍNEAS DEL PEDIDO ----------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarDetalle(int pedidoId, int platoId, int cantidad)
    {
        var pedido = await CargarPedidoAsync(pedidoId);
        if (pedido == null) return NotFound();

        if (!EstadosPedido.EsActivo(pedido.Estado))
        {
            TempData["Error"] = "Este pedido ya está cerrado; no se pueden agregar platos.";
            return RedirectToAction(nameof(Details), new { id = pedidoId });
        }
        if (cantidad < 1 || cantidad > 99)
        {
            TempData["Error"] = "La cantidad debe estar entre 1 y 99.";
            return RedirectToAction(nameof(Details), new { id = pedidoId });
        }

        var plato = await _context.Platos.FindAsync(platoId);
        if (plato == null || !plato.Disponible)
        {
            TempData["Error"] = "Ese plato no existe o está agotado.";
            return RedirectToAction(nameof(Details), new { id = pedidoId });
        }

        var linea = pedido.Detalles.FirstOrDefault(d => d.PlatoId == platoId);
        if (linea == null)
        {
            pedido.Detalles.Add(new DetallePedido { PlatoId = platoId, Plato = plato, Cantidad = cantidad });
        }
        else
        {
            linea.Cantidad = Math.Min(99, linea.Cantidad + cantidad);
        }

        // Si ya estaba servido y piden algo más, vuelve a cocina.
        if (pedido.Estado == EstadosPedido.Servido) pedido.Estado = EstadosPedido.EnPreparacion;

        RecalcularTotal(pedido);
        await _context.SaveChangesAsync();

        TempData["Ok"] = $"{plato.Nombre} agregado.";
        return RedirectToAction(nameof(Details), new { id = pedidoId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarCantidad(int detalleId, int delta)
    {
        var detalle = await _context.DetallePedidos.FindAsync(detalleId);
        if (detalle == null) return NotFound();

        var pedido = await CargarPedidoAsync(detalle.PedidoId);
        if (pedido == null) return NotFound();

        if (EstadosPedido.EsActivo(pedido.Estado))
        {
            var linea = pedido.Detalles.First(d => d.Id == detalleId);
            linea.Cantidad = Math.Clamp(linea.Cantidad + (delta >= 0 ? 1 : -1), 1, 99);
            RecalcularTotal(pedido);
            await _context.SaveChangesAsync();
        }
        else
        {
            TempData["Error"] = "Este pedido ya está cerrado.";
        }

        return RedirectToAction(nameof(Details), new { id = pedido.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuitarDetalle(int detalleId)
    {
        var detalle = await _context.DetallePedidos.FindAsync(detalleId);
        if (detalle == null) return NotFound();

        var pedido = await CargarPedidoAsync(detalle.PedidoId);
        if (pedido == null) return NotFound();

        if (EstadosPedido.EsActivo(pedido.Estado))
        {
            var linea = pedido.Detalles.First(d => d.Id == detalleId);
            pedido.Detalles.Remove(linea);
            _context.DetallePedidos.Remove(linea);
            RecalcularTotal(pedido);
            await _context.SaveChangesAsync();
        }
        else
        {
            TempData["Error"] = "Este pedido ya está cerrado.";
        }

        return RedirectToAction(nameof(Details), new { id = pedido.Id });
    }

    // ---------- CAMBIO DE ESTADO ----------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, string estado)
    {
        var pedido = await CargarPedidoAsync(id);
        if (pedido == null) return NotFound();

        if (!EstadosPedido.EsActivo(pedido.Estado))
        {
            TempData["Error"] = "El pedido ya está cerrado y no puede cambiar de estado.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (!EstadosPedido.Todos.Contains(estado))
        {
            TempData["Error"] = "Estado no válido.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if ((estado == EstadosPedido.Servido || estado == EstadosPedido.Pagado) && pedido.Detalles.Count == 0)
        {
            TempData["Error"] = "Agrega al menos un plato antes de continuar.";
            return RedirectToAction(nameof(Details), new { id });
        }

        pedido.Estado = estado;
        RecalcularTotal(pedido);
        await _context.SaveChangesAsync();
        await SincronizarMesaAsync(pedido.MesaId);

        TempData["Ok"] = estado switch
        {
            EstadosPedido.Servido => "Pedido marcado como servido.",
            EstadosPedido.Pagado => $"Pedido cobrado: {pedido.Total:0.00} Bs. La mesa quedó libre.",
            EstadosPedido.Cancelado => "Pedido cancelado. La mesa quedó libre.",
            _ => "Estado actualizado."
        };
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---------- ELIMINAR ----------
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var pedido = await CargarPedidoAsync(id.Value);
        if (pedido == null) return NotFound();

        ViewBag.Bloqueo = MotivoBloqueo(pedido);
        return View(pedido);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var pedido = await _context.Pedidos.FindAsync(id);
        if (pedido == null) return RedirectToAction(nameof(Index));

        var bloqueo = MotivoBloqueo(pedido);
        if (bloqueo != null)
        {
            TempData["Error"] = bloqueo;
            return RedirectToAction(nameof(Details), new { id });
        }

        var mesaId = pedido.MesaId;
        _context.Pedidos.Remove(pedido);
        await _context.SaveChangesAsync();
        await SincronizarMesaAsync(mesaId);

        TempData["Ok"] = "Pedido eliminado.";
        return RedirectToAction(nameof(Index));
    }

    // =====================  AUXILIARES  =====================

    private static string? MotivoBloqueo(Pedido pedido) =>
        pedido.Estado == EstadosPedido.Pagado
            ? "Los pedidos pagados forman parte del historial de ventas y no se pueden eliminar."
            : null;

    private async Task<Pedido?> CargarPedidoAsync(int id, bool soloCabecera = false)
    {
        var consulta = _context.Pedidos.Include(p => p.Mesa).Include(p => p.Empleado).AsQueryable();
        if (!soloCabecera)
        {
            consulta = consulta.Include(p => p.Detalles).ThenInclude(d => d.Plato).ThenInclude(pl => pl!.Categoria);
        }
        return await consulta.FirstOrDefaultAsync(p => p.Id == id);
    }

    private static void RecalcularTotal(Pedido pedido)
    {
        pedido.Total = pedido.Detalles.Sum(d => d.Cantidad * (d.Plato?.Precio ?? 0));
    }

    // La mesa está "Ocupada" mientras tenga un pedido activo; si no, queda "Libre".
    private async Task SincronizarMesaAsync(int mesaId)
    {
        var mesa = await _context.Mesas.FindAsync(mesaId);
        if (mesa == null) return;

        var ocupada = await _context.Pedidos.AnyAsync(p => p.MesaId == mesaId &&
            (p.Estado == EstadosPedido.EnPreparacion || p.Estado == EstadosPedido.Servido));

        var nuevoEstado = ocupada ? EstadosMesa.Ocupada : EstadosMesa.Libre;
        if (mesa.Estado != nuevoEstado)
        {
            mesa.Estado = nuevoEstado;
            await _context.SaveChangesAsync();
        }
    }

    private async Task CargarCombosAsync(int? mesaId, int? empleadoId)
    {
        // Solo se ofrecen mesas libres (más la que viene preseleccionada si es válida).
        var mesas = await _context.Mesas
            .Where(m => m.Estado == EstadosMesa.Libre || m.Id == mesaId)
            .OrderBy(m => m.NumeroMesa)
            .ToListAsync();

        ViewBag.Mesas = mesas
            .Select(m => new SelectListItem(
                $"Mesa {m.NumeroMesa} · {m.Capacidad} personas" + (m.Estado != EstadosMesa.Libre ? $" ({m.Estado})" : ""),
                m.Id.ToString(),
                m.Id == mesaId))
            .ToList();

        await CargarEmpleadosAsync(empleadoId);
    }

    private async Task CargarEmpleadosAsync(int? empleadoId)
    {
        var empleados = await _context.Empleados.Include(e => e.Cargo).OrderBy(e => e.Nombre).ToListAsync();
        ViewBag.Empleados = empleados
            .Select(e => new SelectListItem(
                $"{e.NombreCompleto}" + (e.Cargo != null ? $" · {e.Cargo.NombreCargo}" : ""),
                e.Id.ToString(),
                e.Id == empleadoId))
            .ToList();
    }

    private async Task CargarPlatosAsync()
    {
        var platos = await _context.Platos
            .Include(p => p.Categoria)
            .Where(p => p.Disponible)
            .OrderBy(p => p.Categoria!.Nombre).ThenBy(p => p.Nombre)
            .ToListAsync();

        var grupos = new Dictionary<string, SelectListGroup>();
        ViewBag.Platos = platos.Select(p =>
        {
            var nombreGrupo = p.Categoria?.Nombre ?? "Otros";
            if (!grupos.TryGetValue(nombreGrupo, out var grupo))
            {
                grupo = new SelectListGroup { Name = nombreGrupo };
                grupos[nombreGrupo] = grupo;
            }
            return new SelectListItem($"{p.Nombre} — Bs {p.Precio:0.00}", p.Id.ToString()) { Group = grupo };
        }).ToList();
    }
}
