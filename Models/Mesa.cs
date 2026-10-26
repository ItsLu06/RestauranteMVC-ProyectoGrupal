using System.ComponentModel.DataAnnotations;

namespace RestauranteMVC.Models;

public class Mesa
{
    public int Id { get; set; }

    [Range(1, 999, ErrorMessage = "El número de mesa debe estar entre 1 y 999.")]
    [Display(Name = "Número de mesa")]
    public int NumeroMesa { get; set; }

    [Range(1, 50, ErrorMessage = "La capacidad debe estar entre 1 y 50 personas.")]
    [Display(Name = "Capacidad (personas)")]
    public int Capacidad { get; set; }

    [Required(ErrorMessage = "Selecciona el estado de la mesa.")]
    public string Estado { get; set; } = EstadosMesa.Libre;

    public List<Pedido> Pedidos { get; set; } = new();
}
