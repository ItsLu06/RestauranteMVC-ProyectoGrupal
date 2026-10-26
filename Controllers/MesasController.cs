using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestauranteMVC.Data;
using RestauranteMVC.Models;

namespace RestauranteMVC.Controllers;

public class MesasController : Controller
{
    private readonly RestauranteDbContext _context;

    public MesasController(RestauranteDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.Mesas.OrderBy(m => m.NumeroMesa).ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var mesa = await _context.Mesas.FirstOrDefaultAsync(m => m.Id == id);
        if (mesa == null) return NotFound();

        ViewBag.PedidoAbierto = await _context.Pedidos
            .Where(p => p.MesaId == mesa.Id && (p.Estado == EstadosPedido.EnPreparacion || p.Estado == EstadosPedido.Servido))
            .OrderByDescending(p => p.FechaHora)
            .FirstOrDefaultAsync();
        return View(mesa);
    }

    public IActionResult Create()
    {
        CargarEstados(EstadosMesa.Libre);
        return View(new Mesa());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("NumeroMesa,Capacidad,Estado")] Mesa mesa)
    {
        await ValidarNumeroAsync(mesa);

        if (!ModelState.IsValid)
        {
            CargarEstados(mesa.Estado);
            return View(mesa);
        }

        _context.Add(mesa);
        await _context.SaveChangesAsync();
        TempData["Ok"] = $"Mesa {mesa.NumeroMesa} creada.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var mesa = await _context.Mesas.FindAsync(id);
        if (mesa == null) return NotFound();

        CargarEstados(mesa.Estado);
        return View(mesa);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,NumeroMesa,Capacidad,Estado")] Mesa mesa)
    {
        if (id != mesa.Id) return NotFound();

        await ValidarNumeroAsync(mesa);

        if (!ModelState.IsValid)
        {
            CargarEstados(mesa.Estado);
            return View(mesa);
        }

        try
        {
            _context.Update(mesa);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Mesas.AnyAsync(m => m.Id == mesa.Id)) return NotFound();
            throw;
        }

        TempData["Ok"] = "Mesa actualizada.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var mesa = await _context.Mesas.FirstOrDefaultAsync(m => m.Id == id);
        if (mesa == null) return NotFound();

        ViewBag.Bloqueo = await MotivoBloqueoAsync(mesa.Id);
        return View(mesa);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var mesa = await _context.Mesas.FindAsync(id);
        if (mesa == null) return RedirectToAction(nameof(Index));

        var bloqueo = await MotivoBloqueoAsync(id);
        if (bloqueo != null)
        {
            TempData["Error"] = bloqueo;
            return RedirectToAction(nameof(Index));
        }

        _context.Mesas.Remove(mesa);
        await _context.SaveChangesAsync();
        TempData["Ok"] = "Mesa eliminada.";
        return RedirectToAction(nameof(Index));
    }

    private void CargarEstados(string? seleccionado)
    {
        ViewBag.Estados = new SelectList(EstadosMesa.Todos, seleccionado);
    }

    private async Task ValidarNumeroAsync(Mesa mesa)
    {
        var existe = await _context.Mesas.AnyAsync(m => m.NumeroMesa == mesa.NumeroMesa && m.Id != mesa.Id);
        if (existe) ModelState.AddModelError(nameof(Mesa.NumeroMesa), "Ya existe una mesa con ese número.");
    }

    // Borrar una mesa eliminaría en cascada todos sus pedidos (y alteraría los reportes de ventas).
    private async Task<string?> MotivoBloqueoAsync(int mesaId)
    {
        var n = await _context.Pedidos.CountAsync(p => p.MesaId == mesaId);
        return n == 0 ? null : $"No se puede eliminar: la mesa tiene {n} pedido(s) en el historial. Puedes dejarla como «Reservada» para no usarla.";
    }
}
