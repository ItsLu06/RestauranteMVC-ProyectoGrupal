using System.ComponentModel.DataAnnotations;

namespace RestauranteMVC.Models;

public class Categoria
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre de la categoría es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    public List<Plato> Platos { get; set; } = new();
}
