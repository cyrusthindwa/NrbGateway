namespace CHL.NrbGateway.Application.Common.Interfaces;

public interface ICorsOriginManager
{
    bool IsOriginAllowed(string origin);
    void Reload(IEnumerable<string> origins);
    void AddOrEnable(string origin);
    void Remove(string origin);
    IReadOnlyCollection<string> GetActiveOrigins();
}
