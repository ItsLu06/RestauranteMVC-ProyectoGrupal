namespace RestauranteMVC.Models;

public class DetallePedido
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public Pedido? Pedido { get; set; }
    public int PlatoId { get; set; }
    public Plato? Plato { get; set; }
    public int Cantidad { get; set; }

    // Calculado (no es columna de la BD): cantidad por el precio actual del plato.
    public decimal Subtotal => Cantidad * (Plato?.Precio ?? 0);
}
