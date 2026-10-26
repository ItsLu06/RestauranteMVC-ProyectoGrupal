namespace RestauranteMVC.Models;

// ----- Tablero (Home) -----
public class MesaTablero
{
    public Mesa Mesa { get; set; } = null!;
    public Pedido? PedidoAbierto { get; set; }
}

public class DashboardViewModel
{
    public int MesasTotal { get; set; }
    public int MesasLibres { get; set; }
    public int PedidosActivos { get; set; }
    public int PedidosHoy { get; set; }
    public decimal VentasHoy { get; set; }
    public int PlatosDisponibles { get; set; }
    public List<MesaTablero> Mesas { get; set; } = new();
    public List<Pedido> PedidosRecientes { get; set; } = new();
}

// ----- Reportes -----
public class VentaDia
{
    public DateTime Fecha { get; set; }
    public int Pedidos { get; set; }
    public decimal Total { get; set; }
}

public class ReporteVentasViewModel
{
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }
    public decimal TotalRecaudado { get; set; }
    public decimal PorCobrar { get; set; }
    public int CantidadPedidos { get; set; }
    public decimal TicketPromedio { get; set; }
    public List<VentaDia> PorDia { get; set; } = new();
    public List<Pedido> Pedidos { get; set; } = new();
}

public class PlatoVendido
{
    public int PlatoId { get; set; }
    public string Plato { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal Total { get; set; }
}

public class ReportePlatosViewModel
{
    public DateTime? Inicio { get; set; }
    public DateTime? Fin { get; set; }
    public List<PlatoVendido> Platos { get; set; } = new();
}
