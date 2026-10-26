using System.ComponentModel.DataAnnotations;

namespace RestauranteMVC.Models;

public class Pedido
{
    public int Id { get; set; }

    [Display(Name = "Fecha y hora")]
    public DateTime FechaHora { get; set; } = DateTime.Now;

    [Display(Name = "Mesa")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una mesa.")]
    public int MesaId { get; set; }
    public Mesa? Mesa { get; set; }

    [Display(Name = "Empleado")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona quién atiende el pedido.")]
    public int EmpleadoId { get; set; }
    public Empleado? Empleado { get; set; }

    public string Estado { get; set; } = EstadosPedido.EnPreparacion;

    public decimal Total { get; set; }

    public List<DetallePedido> Detalles { get; set; } = new();
}
