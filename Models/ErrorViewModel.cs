namespace RestauranteMVC.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public int Codigo { get; set; } = 500;

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public string Titulo => Codigo == 404 ? "No encontramos lo que buscas" : "Algo salió mal";

    public string Mensaje => Codigo == 404
        ? "La página o el registro que intentas abrir no existe o fue eliminado."
        : "Ocurrió un error al procesar tu solicitud. Intenta de nuevo; si persiste, avisa al administrador.";
}
