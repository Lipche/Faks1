using System.Collections.Generic;

namespace PhotoLibrary.Models
{
    public sealed class Album
    {
        public string Name { get; set; } = string.Empty;
        public List<string> PhotoIds { get; set; } = new();
    }
}

