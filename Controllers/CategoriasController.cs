using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestauranteMVC.Data;
using RestauranteMVC.Models;

namespace RestauranteMVC.Controllers;

public class CategoriasController : Controller
{
    private readonly RestauranteDbContext _context;

    public CategoriasController(RestauranteDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var categorias = await _context.Categorias.Include(c => c.Platos).OrderBy(c => c.Nombre).ToListAsync();
        return View(categorias);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var categoria = await _context.Categorias.Include(c => c.Platos).FirstOrDefaultAsync(c => c.Id == id);
        if (categoria == null) return NotFound();
        return View(categoria);
    }

    public IActionResult Create() => View(new Categoria());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nombre,Descripcion")] Categoria categoria)
    {
        Normalizar(categoria);
        await ValidarDuplicadoAsync(categoria);

        if (!ModelState.IsValid) return View(categoria);

        _context.Add(categoria);
        await _context.SaveChangesAsync();
        TempData["Ok"] = $"Categoría «{categoria.Nombre}» creada.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var categoria = await _context.Categorias.FindAsync(id);
        if (categoria == null) return NotFound();
        return View(categoria);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Descripcion")] Categoria categoria)
    {
        if (id != categoria.Id) return NotFound();

        Normalizar(categoria);
        await ValidarDuplicadoAsync(categoria);

        if (!ModelState.IsValid) return View(categoria);

        try
        {
            _context.Update(categoria);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Categorias.AnyAsync(c => c.Id == categoria.Id)) return NotFound();
            throw;
        }

        TempData["Ok"] = "Categoría actualizada.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var categoria = await _context.Categorias.FirstOrDefaultAsync(c => c.Id == id);
        if (categoria == null) return NotFound();

        ViewBag.Bloqueo = await MotivoBloqueoAsync(categoria.Id);
        return View(categoria);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var categoria = await _context.Categorias.FindAsync(id);
        if (categoria == null) return RedirectToAction(nameof(Index));

        var bloqueo = await MotivoBloqueoAsync(id);
        if (bloqueo != null)
        {
            TempData["Error"] = bloqueo;
            return RedirectToAction(nameof(Index));
        }

        _context.Categorias.Remove(categoria);
        await _context.SaveChangesAsync();
        TempData["Ok"] = "Categoría eliminada.";
        return RedirectToAction(nameof(Index));
    }

    private static void Normalizar(Categoria c)
    {
        c.Nombre = c.Nombre?.Trim() ?? string.Empty;
        c.Descripcion = string.IsNullOrWhiteSpace(c.Descripcion) ? null : c.Descripcion.Trim();
    }

    private async Task ValidarDuplicadoAsync(Categoria c)
    {
        var existe = await _context.Categorias.AnyAsync(x => x.Nombre == c.Nombre && x.Id != c.Id);
        if (existe) ModelState.AddModelError(nameof(Categoria.Nombre), "Ya existe una categoría con ese nombre.");
    }

    // La relación Plato → Categoría es Restrict: sin esta comprobación el borrado terminaría en un error 500.
    private async Task<string?> MotivoBloqueoAsync(int categoriaId)
    {
        var n = await _context.Platos.CountAsync(p => p.CategoriaId == categoriaId);
        return n == 0 ? null : $"No se puede eliminar: la categoría tiene {n} plato(s). Muévelos a otra categoría primero.";
    }
}
