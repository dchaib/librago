namespace Librago.Connectors;

public sealed class LibraryConnectorResolver(IEnumerable<ILibraryConnector> connectors)
{
    private readonly Dictionary<string, ILibraryConnector> _connectors = connectors
        .ToDictionary(connector => connector.Network.Identifier, StringComparer.OrdinalIgnoreCase);

    public ILibraryConnector Resolve(string network) =>
        _connectors.TryGetValue(network, out var connector)
            ? connector
            : throw new InvalidOperationException($"No library connector is registered for '{network}'.");
}
