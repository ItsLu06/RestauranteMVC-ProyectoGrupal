using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestauranteMVC.Data;
using RestauranteMVC.Models;

namespace RestauranteMVC.Controllers;

public class PlatosController : Controller
{
    private readonly RestauranteDbContext _context;

    public PlatosController(RestauranteDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? categoriaId)
    {
        var consulta = _context.Platos.Include(p => p.Categoria).AsQueryable();
        if (categoriaId != null) consulta = consulta.Where(p => p.CategoriaId == categoriaId);

        ViewBag.Categorias = await _context.Categorias.OrderBy(c => c.Nombre).ToListAsync();
        ViewBag.CategoriaId = categoriaId;

        return View(await consulta.OrderBy(p => p.Categoria!.Nombre).ThenBy(p => p.Nombre).ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var plato = await _context.Platos.Include(p => p.Categoria).FirstOrDefaultAsync(p => p.Id == id);
        if (plato == null) return NotFound();
        return View(plato);
    }

    public async Task<IActionResult> Create()
    {
        await CargarCategoriasAsync(null);
        return View(new Plato());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nombre,Precio,Disponible,CategoriaId")] Plato plato)
    {
        await NormalizarYValidarAsync(plato);

        if (!ModelState.IsValid)
        {
            await CargarCategoriasAsync(plato.CategoriaId);
            return View(plato);
        }

        _context.Add(plato);
        await _context.SaveChangesAsync();
        TempData["Ok"] = $"Plato «{plato.Nombre}» agregado al menú.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var plato = await _context.Platos.FindAsync(id);
        if (plato == null) return NotFound();

        await CargarCategoriasAsync(plato.CategoriaId);
        return View(plato);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Precio,Disponible,CategoriaId")] Plato plato)
    {
        if (id != plato.Id) return NotFound();

        await NormalizarYValidarAsync(plato);

        if (!ModelState.IsValid)
        {
            await CargarCategoriasAsync(plato.CategoriaId);
            return View(plato);
        }

        try
        {
            _context.Update(plato);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Platos.AnyAsync(p => p.Id == plato.Id)) return NotFound();
            throw;
        }

        TempData["Ok"] = "Plato actualizado.";
        return RedirectToAction(nameof(Index));
    }

    // Cambio rápido de disponibilidad desde la lista (útil cuando se acaba un ingrediente).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarDisponibilidad(int id)
    {
        var plato = await _context.Platos.FindAsync(id);
        if (plato == null) return NotFound();

        plato.Disponible = !plato.Disponible;
        await _context.SaveChangesAsync();
        TempData["Ok"] = plato.Disponible ? $"«{plato.Nombre}» vuelve a estar disponible." : $"«{plato.Nombre}» marcado como agotado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var plato = await _context.Platos.Include(p => p.Categoria).FirstOrDefaultAsync(p => p.Id == id);
        if (plato == null) return NotFound();

        ViewBag.Bloqueo = await MotivoBloqueoAsync(plato.Id);
        return View(plato);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var plato = await _context.Platos.FindAsync(id);
        if (plato == null) return RedirectToAction(nameof(Index));

        var bloqueo = await MotivoBloqueoAsync(id);
        if (bloqueo != null)
        {
            TempData["Error"] = bloqueo;
            return RedirectToAction(nameof(Index));
        }

        _context.Platos.Remove(plato);
        await _context.SaveChangesAsync();
        TempData["Ok"] = "Plato eliminado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task NormalizarYValidarAsync(Plato plato)
    {
        plato.Nombre = plato.Nombre?.Trim() ?? string.Empty;

        if (plato.CategoriaId > 0 && !await _context.Categorias.AnyAsync(c => c.Id == plato.CategoriaId))
        {
            ModelState.AddModelError(nameof(Plato.CategoriaId), "La categoría seleccionada ya no existe.");
        }

        var duplicado = await _context.Platos.AnyAsync(p => p.Nombre == plato.Nombre && p.Id != plato.Id);
        if (duplicado) ModelState.AddModelError(nameof(Plato.Nombre), "Ya existe un plato con ese nombre.");
    }

    private async Task CargarCategoriasAsync(int? seleccionada)
    {
        var categorias = await _context.Categorias.OrderBy(c => c.Nombre).ToListAsync();
        ViewBag.Categorias = new SelectList(categorias, "Id", "Nombre", seleccionada);
    }

    // Borrar un plato eliminaría en cascada sus líneas en pedidos ya registrados.
    private async Task<string?> MotivoBloqueoAsync(int platoId)
    {
        var n = await _context.DetallePedidos.CountAsync(d => d.PlatoId == platoId);
        return n == 0 ? null : "No se puede eliminar: el plato aparece en pedidos registrados. Márcalo como «Agotado» para que no se pueda pedir.";
    }
}
