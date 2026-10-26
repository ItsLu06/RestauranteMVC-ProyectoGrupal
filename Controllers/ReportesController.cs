using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestauranteMVC.Data;
using RestauranteMVC.Models;

namespace RestauranteMVC.Controllers;

public class ReportesController : Controller
{
    private readonly RestauranteDbContext _context;

    public ReportesController(RestauranteDbContext context)
    {
        _context = context;
    }

    // Ventas del período. "Recaudado" cuenta solo pedidos pagados; los activos se muestran como "por cobrar";
    // los cancelados no aparecen.
    public async Task<IActionResult> VentasPorFecha(DateTime? fechaInicio, DateTime? fechaFin)
    {
        var inicio = (fechaInicio ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
        var fin = (fechaFin ?? DateTime.Today).Date;
        if (fin < inicio) (inicio, fin) = (fin, inicio);

        var limite = fin.AddDays(1); // fin inclusivo, sin usar .Date en la consulta (aprovecha índices)

        var pedidos = await _context.Pedidos
            .Include(p => p.Mesa)
            .Include(p => p.Empleado)
            .Where(p => p.FechaHora >= inicio && p.FechaHora < limite && p.Estado != EstadosPedido.Cancelado)
            .OrderByDescending(p => p.FechaHora)
            .ToListAsync();

        var pagados = pedidos.Where(p => p.Estado == EstadosPedido.Pagado).ToList();

        var vm = new ReporteVentasViewModel
        {
            Inicio = inicio,
            Fin = fin,
            Pedidos = pedidos,
            CantidadPedidos = pedidos.Count,
            TotalRecaudado = pagados.Sum(p => p.Total),
            PorCobrar = pedidos.Where(p => EstadosPedido.EsActivo(p.Estado)).Sum(p => p.Total),
            TicketPromedio = pagados.Count == 0 ? 0 : pagados.Sum(p => p.Total) / pagados.Count,
            PorDia = pagados
                .GroupBy(p => p.FechaHora.Date)
                .Select(g => new VentaDia { Fecha = g.Key, Pedidos = g.Count(), Total = g.Sum(p => p.Total) })
                .OrderBy(d => d.Fecha)
                .ToList()
        };

        return View(vm);
    }

    // Ranking de platos. Fechas opcionales: si se dejan vacías cuenta todo el historial. No incluye cancelados.
    public async Task<IActionResult> PlatosMasVendidos(DateTime? fechaInicio, DateTime? fechaFin)
    {
        var consulta = _context.DetallePedidos.Where(d => d.Pedido!.Estado != EstadosPedido.Cancelado);

        if (fechaInicio != null)
        {
            var desde = fechaInicio.Value.Date;
            consulta = consulta.Where(d => d.Pedido!.FechaHora >= desde);
        }
        if (fechaFin != null)
        {
            var hasta = fechaFin.Value.Date.AddDays(1);
            consulta = consulta.Where(d => d.Pedido!.FechaHora < hasta);
        }

        // Se agrupa por Id del plato (no por nombre) para no mezclar platos distintos con el mismo nombre.
        var platos = await (
            from d in consulta
            join p in _context.Platos on d.PlatoId equals p.Id
            group new { d.Cantidad, p.Precio } by new { p.Id, p.Nombre } into g
            select new PlatoVendido
            {
                PlatoId = g.Key.Id,
                Plato = g.Key.Nombre,
                Cantidad = g.Sum(x => x.Cantidad),
                Total = g.Sum(x => x.Cantidad * x.Precio)
            })
            .OrderByDescending(x => x.Cantidad)
            .ThenBy(x => x.Plato)
            .ToListAsync();

        return View(new ReportePlatosViewModel { Inicio = fechaInicio, Fin = fechaFin, Platos = platos });
    }
}
