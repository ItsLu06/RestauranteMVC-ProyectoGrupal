using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestauranteMVC.Data;
using RestauranteMVC.Models;

namespace RestauranteMVC.Controllers;

public class HomeController : Controller
{
    private readonly RestauranteDbContext _context;

    public HomeController(RestauranteDbContext context)
    {
        _context = context;
    }

    // Tablero: resumen del día y estado de las mesas.
    public async Task<IActionResult> Index()
    {
        var hoy = DateTime.Today;

        var mesas = await _context.Mesas.OrderBy(m => m.NumeroMesa).ToListAsync();

        var activos = await _context.Pedidos
            .Include(p => p.Empleado)
            .Include(p => p.Mesa)
            .Where(p => p.Estado == EstadosPedido.EnPreparacion || p.Estado == EstadosPedido.Servido)
            .OrderBy(p => p.FechaHora)
            .ToListAsync();

        var vm = new DashboardViewModel
        {
            MesasTotal = mesas.Count,
            MesasLibres = mesas.Count(m => m.Estado == EstadosMesa.Libre),
            PedidosActivos = activos.Count,
            PedidosHoy = await _context.Pedidos.CountAsync(p => p.FechaHora >= hoy && p.Estado != EstadosPedido.Cancelado),
            VentasHoy = await _context.Pedidos
                .Where(p => p.FechaHora >= hoy && p.Estado == EstadosPedido.Pagado)
                .SumAsync(p => (decimal?)p.Total) ?? 0m,
            PlatosDisponibles = await _context.Platos.CountAsync(p => p.Disponible),
            PedidosRecientes = activos.OrderByDescending(p => p.FechaHora).Take(6).ToList(),
            Mesas = mesas.Select(m => new MesaTablero
            {
                Mesa = m,
                PedidoAbierto = activos.FirstOrDefault(p => p.MesaId == m.Id)
            }).ToList()
        };

        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? codigo)
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Codigo = codigo ?? 500
        });
    }
}
