namespace StockMind.Alerts.Domain.Entities;

public enum AlertStatus { Pendiente, Atendida, Descartada }
public sealed class ReplenishmentAlert
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ProductoId { get; set; } = string.Empty;
    public int CantidadSugerida { get; set; }
    public AlertStatus Estado { get; set; } = AlertStatus.Pendiente;
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
}

