using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestauranteMVC.Data;
using RestauranteMVC.Models;

namespace RestauranteMVC.Controllers;

public class CargosController : Controller
{
    private readonly RestauranteDbContext _context;

    public CargosController(RestauranteDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var cargos = await _context.Cargos.Include(c => c.Empleados).OrderBy(c => c.NombreCargo).ToListAsync();
        return View(cargos);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var cargo = await _context.Cargos.Include(c => c.Empleados).FirstOrDefaultAsync(c => c.Id == id);
        if (cargo == null) return NotFound();
        return View(cargo);
    }

    public IActionResult Create() => View(new Cargo());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("NombreCargo")] Cargo cargo)
    {
        cargo.NombreCargo = cargo.NombreCargo?.Trim() ?? string.Empty;
        await ValidarDuplicadoAsync(cargo);

        if (!ModelState.IsValid) return View(cargo);

        _context.Add(cargo);
        await _context.SaveChangesAsync();
        TempData["Ok"] = $"Cargo «{cargo.NombreCargo}» creado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var cargo = await _context.Cargos.FindAsync(id);
        if (cargo == null) return NotFound();
        return View(cargo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,NombreCargo")] Cargo cargo)
    {
        if (id != cargo.Id) return NotFound();

        cargo.NombreCargo = cargo.NombreCargo?.Trim() ?? string.Empty;
        await ValidarDuplicadoAsync(cargo);

        if (!ModelState.IsValid) return View(cargo);

        try
        {
            _context.Update(cargo);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Cargos.AnyAsync(c => c.Id == cargo.Id)) return NotFound();
            throw;
        }

        TempData["Ok"] = "Cargo actualizado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var cargo = await _context.Cargos.FirstOrDefaultAsync(c => c.Id == id);
        if (cargo == null) return NotFound();

        ViewBag.Bloqueo = await MotivoBloqueoAsync(cargo.Id);
        return View(cargo);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var cargo = await _context.Cargos.FindAsync(id);
        if (cargo == null) return RedirectToAction(nameof(Index));

        var bloqueo = await MotivoBloqueoAsync(id);
        if (bloqueo != null)
        {
            TempData["Error"] = bloqueo;
            return RedirectToAction(nameof(Index));
        }

        _context.Cargos.Remove(cargo);
        await _context.SaveChangesAsync();
        TempData["Ok"] = "Cargo eliminado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidarDuplicadoAsync(Cargo cargo)
    {
        var existe = await _context.Cargos.AnyAsync(c => c.NombreCargo == cargo.NombreCargo && c.Id != cargo.Id);
        if (existe) ModelState.AddModelError(nameof(Cargo.NombreCargo), "Ya existe un cargo con ese nombre.");
    }

    // Eliminar un cargo borraría en cascada a sus empleados y todos sus pedidos: lo impedimos.
    private async Task<string?> MotivoBloqueoAsync(int cargoId)
    {
        var n = await _context.Empleados.CountAsync(e => e.CargoId == cargoId);
        return n == 0 ? null : $"No se puede eliminar: hay {n} empleado(s) con este cargo. Reasígnalos primero.";
    }
}
