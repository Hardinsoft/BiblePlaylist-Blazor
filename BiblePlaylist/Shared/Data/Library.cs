using BiblePlaylist.Shared.Playlist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiblePlaylist.Shared.Data
{
    public class Library
    {
        public string Key {  get; set; }
        public string UPN { get; set; }
        public string Code { get; set; }
        public List<Playlist.Playlist> Playlists {  get; set; }
        public Library() { }
        
    }
}
