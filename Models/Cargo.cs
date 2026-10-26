using System.ComponentModel.DataAnnotations;

namespace RestauranteMVC.Models;

public class Cargo
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del cargo es obligatorio.")]
    [Display(Name = "Cargo")]
    public string NombreCargo { get; set; } = string.Empty;

    public List<Empleado> Empleados { get; set; } = new();
}
