using System;
using KaraW3B.Interpreters.Interfaces;
using KaraW3B.Server.Songs.Models.Songs;

namespace KaraW3B.Server.Songs.Core.Persistence.Models.Songs
{
    public class DbSongMedley : ISongMedley
    {
        public TimeSpan MedleyStart { get; set; }

        public TimeSpan MedleyEnd { get; set; }

        public SongMedley ToSongMedley()
        {
            return new SongMedley
            {
                MedleyStart = MedleyStart,
                MedleyEnd = MedleyEnd
            };
        }
    }
}