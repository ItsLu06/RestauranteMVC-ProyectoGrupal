using System.ComponentModel.DataAnnotations;

namespace RestauranteMVC.Models;

public class Empleado
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    public string Apellido { get; set; } = string.Empty;

    [RegularExpression(@"^[0-9+\-\s()]{6,20}$", ErrorMessage = "Ingresa un teléfono válido (solo números, de 6 a 20 caracteres).")]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [Display(Name = "Cargo")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un cargo.")]
    public int CargoId { get; set; }
    public Cargo? Cargo { get; set; }

    public List<Pedido> Pedidos { get; set; } = new();

    [Display(Name = "Empleado")]
    public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
}
