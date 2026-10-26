using System.Globalization;
using RestauranteMVC.Models;

namespace RestauranteMVC.Helpers;

/// <summary>Formatos de presentación (moneda, fechas y colores de estado) usados en las vistas.</summary>
public static class Formato
{
    // El servidor interpreta números con cultura invariante (ver Program.cs);
    // aquí solo decidimos cómo MOSTRARLOS: 1.250,50 (convención boliviana).
    private static readonly CultureInfo CulturaVista = ObtenerCulturaVista();

    private static CultureInfo ObtenerCulturaVista()
    {
        try { return CultureInfo.GetCultureInfo("es-BO"); }
        catch (CultureNotFoundException) { return CultureInfo.InvariantCulture; }
    }

    public static string Bs(decimal monto) => "Bs " + monto.ToString("N2", CulturaVista);

    public static string FechaHora(DateTime fecha) => fecha.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

    public static string FechaLarga(DateTime fecha) => fecha.ToString("dddd d 'de' MMMM", CulturaVista);

    public static string Fecha(DateTime fecha) => fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public static string ClaseEstadoPedido(string? estado) => estado switch
    {
        EstadosPedido.EnPreparacion => "b-amber",
        EstadosPedido.Servido => "b-blue",
        EstadosPedido.Pagado => "b-green",
        _ => "b-gray"
    };

    public static string ClaseEstadoMesa(string? estado) => estado switch
    {
        EstadosMesa.Libre => "b-green",
        EstadosMesa.Ocupada => "b-red",
        EstadosMesa.Reservada => "b-amber",
        _ => "b-gray"
    };
}
