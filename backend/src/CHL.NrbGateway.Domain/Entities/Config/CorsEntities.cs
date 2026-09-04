namespace CHL.NrbGateway.Domain.Entities.Config;

public class CorsOrigin
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Origin { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
