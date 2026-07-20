using KaraW3B.Client.Songs.Connectors.Collections;
using KaraW3B.Client.Songs.Connectors.Songs;

namespace KaraW3B.Client.Songs.Connectors
{
    public interface IKaraW3BConnector
    {
        ILibrariesConnector Libraries { get; }
        ISongsConnector Songs { get; }
    }
}