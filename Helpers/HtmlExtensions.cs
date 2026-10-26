using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RestauranteMVC.Helpers;

public static class HtmlExtensions
{
    /// <summary>Dibuja un icono del sprite definido en Views/Shared/_Iconos.cshtml.</summary>
    public static IHtmlContent Icono(this IHtmlHelper html, string nombre, string? clase = null)
    {
        var extra = string.IsNullOrWhiteSpace(clase) ? "" : " " + clase;
        return new HtmlString($"<svg class=\"ic{extra}\" aria-hidden=\"true\"><use href=\"#i-{nombre}\"></use></svg>");
    }
}
