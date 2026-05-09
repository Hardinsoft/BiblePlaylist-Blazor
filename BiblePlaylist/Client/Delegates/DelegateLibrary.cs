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
    public class DelegateLibrary : IDelegateLibrary
    {
        public event EventHandler<EventArgs<SelectedBookChapter>> ChapterChanged;        
        public void NotifyChapterChanged(object sender, EventArgs<SelectedBookChapter> e)
            => ChapterChanged?.Invoke(sender, e);

        public event EventHandler<EventArgs<bool>> HasPlayedChanged;
        public void NotifyHasPlayedChanged(object sender, EventArgs<bool> e)
            => HasPlayedChanged?.Invoke(sender, e);
    }
}
