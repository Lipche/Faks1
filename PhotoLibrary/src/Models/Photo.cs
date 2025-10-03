using System;
using System.Collections.Generic;

namespace PhotoLibrary.Models
{
    public sealed class Photo
    {
        public string Id { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime DateTaken { get; set; }
        public List<string> Tags { get; set; } = new();
    }
}

