namespace RestauranteMVC.Models;

/// <summary>Estados posibles de un pedido (se guardan como texto en la BD).</summary>
public static class EstadosPedido
{
    public const string EnPreparacion = "En preparación";
    public const string Servido = "Servido";
    public const string Pagado = "Pagado";
    public const string Cancelado = "Cancelado";

    public static readonly string[] Todos = { EnPreparacion, Servido, Pagado, Cancelado };

    /// <summary>Un pedido activo es el que todavía ocupa la mesa (no está pagado ni cancelado).</summary>
    public static bool EsActivo(string? estado) => estado == EnPreparacion || estado == Servido;
}

/// <summary>Estados posibles de una mesa.</summary>
public static class EstadosMesa
{
    public const string Libre = "Libre";
    public const string Ocupada = "Ocupada";
    public const string Reservada = "Reservada";

    public static readonly string[] Todos = { Libre, Ocupada, Reservada };
}
