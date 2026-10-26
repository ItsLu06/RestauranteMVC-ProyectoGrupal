using System.ComponentModel.DataAnnotations;

namespace RestauranteMVC.Models;

public class Plato
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del plato es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    [Range(0.01, 99999999.99, ErrorMessage = "El precio debe ser mayor a 0.")]
    [Display(Name = "Precio (Bs)")]
    public decimal Precio { get; set; }

    public bool Disponible { get; set; } = true;

    [Display(Name = "Categoría")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una categoría.")]
    public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public List<DetallePedido> Detalles { get; set; } = new();
}
