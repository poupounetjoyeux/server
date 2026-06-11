using System;

namespace KaraWeb.Shared.Models.Songs.Medleys
{
    public interface ISongMedley
    {
        TimeSpan MedleyStart { get; }
        TimeSpan MedleyEnd { get; }
    }
}
