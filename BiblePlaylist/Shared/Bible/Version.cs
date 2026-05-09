using System;
using System.Collections.Generic;
using System.Text;

namespace BiblePlaylist.Shared.Bible
{
    public class Version
    {
        [System.ComponentModel.DataAnnotations.Key]
        public string Code { get; set; }
        public string Name { get; set; }
        public IList<Book> Books { get; set; }
    }
}
