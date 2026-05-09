using BiblePlaylist.Client.Delegates;
using BiblePlaylist.Shared.DTO;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiblePlaylist.Client.Events
{
    public interface IDelegateLibrary
    {
        event EventHandler<EventArgs<SelectedBookChapter>> ChapterChanged;
        void NotifyChapterChanged(object sender, EventArgs<SelectedBookChapter> e);
        event EventHandler<EventArgs<bool>> HasPlayedChanged;
        void NotifyHasPlayedChanged(object sender, EventArgs<bool> e);
    }
}
