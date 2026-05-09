using BiblePlaylist.Shared.Audio;
using BiblePlaylist.Shared.Bible;
using System.Collections.Generic;

namespace BiblePlaylist.Server.Data
{
    public interface IAudioTimingProcessor
    {
        Shared.Bible.Version SetVerseTimings(Shared.Bible.Version partialVersion, Transcription transcription);
    }
}
