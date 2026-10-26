using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestauranteMVC.Data;
using RestauranteMVC.Models;

namespace RestauranteMVC.Controllers;

public class EmpleadosController : Controller
{
    private readonly RestauranteDbContext _context;

    public EmpleadosController(RestauranteDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var empleados = await _context.Empleados
            .Include(e => e.Cargo)
            .OrderBy(e => e.Apellido).ThenBy(e => e.Nombre)
            .ToListAsync();
        return View(empleados);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var empleado = await _context.Empleados.Include(e => e.Cargo).FirstOrDefaultAsync(e => e.Id == id);
        if (empleado == null) return NotFound();

        ViewBag.TotalPedidos = await _context.Pedidos.CountAsync(p => p.EmpleadoId == empleado.Id);
        return View(empleado);
    }

    public async Task<IActionResult> Create()
    {
        await CargarCargosAsync(null);
        return View(new Empleado());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nombre,Apellido,Telefono,CargoId")] Empleado empleado)
    {
        await NormalizarYValidarAsync(empleado);

        if (!ModelState.IsValid)
        {
            await CargarCargosAsync(empleado.CargoId);
            return View(empleado);
        }

        _context.Add(empleado);
        await _context.SaveChangesAsync();
        TempData["Ok"] = $"Empleado {empleado.NombreCompleto} registrado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var empleado = await _context.Empleados.FindAsync(id);
        if (empleado == null) return NotFound();

        await CargarCargosAsync(empleado.CargoId);
        return View(empleado);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Apellido,Telefono,CargoId")] Empleado empleado)
    {
        if (id != empleado.Id) return NotFound();

        await NormalizarYValidarAsync(empleado);

        if (!ModelState.IsValid)
        {
            await CargarCargosAsync(empleado.CargoId);
            return View(empleado);
        }

        try
        {
            _context.Update(empleado);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Empleados.AnyAsync(e => e.Id == empleado.Id)) return NotFound();
            throw;
        }

        TempData["Ok"] = "Empleado actualizado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var empleado = await _context.Empleados.Include(e => e.Cargo).FirstOrDefaultAsync(e => e.Id == id);
        if (empleado == null) return NotFound();

        ViewBag.Bloqueo = await MotivoBloqueoAsync(empleado.Id);
        return View(empleado);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var empleado = await _context.Empleados.FindAsync(id);
        if (empleado == null) return RedirectToAction(nameof(Index));

        var bloqueo = await MotivoBloqueoAsync(id);
        if (bloqueo != null)
        {
            TempData["Error"] = bloqueo;
            return RedirectToAction(nameof(Index));
        }

        _context.Empleados.Remove(empleado);
        await _context.SaveChangesAsync();
        TempData["Ok"] = "Empleado eliminado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task NormalizarYValidarAsync(Empleado e)
    {
        e.Nombre = e.Nombre?.Trim() ?? string.Empty;
        e.Apellido = e.Apellido?.Trim() ?? string.Empty;
        e.Telefono = string.IsNullOrWhiteSpace(e.Telefono) ? null : e.Telefono.Trim();

        if (e.CargoId > 0 && !await _context.Cargos.AnyAsync(c => c.Id == e.CargoId))
        {
            ModelState.AddModelError(nameof(Empleado.CargoId), "El cargo seleccionado ya no existe.");
        }
    }

    private async Task CargarCargosAsync(int? seleccionado)
    {
        var cargos = await _context.Cargos.OrderBy(c => c.NombreCargo).ToListAsync();
        ViewBag.Cargos = new SelectList(cargos, "Id", "NombreCargo", seleccionado);
    }

    // Borrar un empleado eliminaría en cascada todos los pedidos que atendió.
    private async Task<string?> MotivoBloqueoAsync(int empleadoId)
    {
        var n = await _context.Pedidos.CountAsync(p => p.EmpleadoId == empleadoId);
        return n == 0 ? null : $"No se puede eliminar: atendió {n} pedido(s) que quedarían sin registro. Mejor conserva el historial.";
    }
}
