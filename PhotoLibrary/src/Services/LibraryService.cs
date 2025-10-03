using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PhotoLibrary.Models;

namespace PhotoLibrary.Services
{
    public sealed class LibraryService
    {
        private readonly string _libraryFilePath;
        private readonly List<Photo> _photos;

        private static readonly string[] SupportedExtensions = new[]
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tif", ".tiff", ".webp"
        };

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private LibraryService(string libraryFilePath, List<Photo> photos)
        {
            _libraryFilePath = libraryFilePath;
            _photos = photos;
        }

        public static LibraryService Load(string libraryFilePath)
        {
            if (!File.Exists(libraryFilePath))
            {
                return new LibraryService(libraryFilePath, new List<Photo>());
            }

            try
            {
                var json = File.ReadAllText(libraryFilePath);
                var data = JsonSerializer.Deserialize<LibraryData>(json, SerializerOptions);
                return new LibraryService(libraryFilePath, data?.Photos ?? new List<Photo>());
            }
            catch
            {
                // If the file is corrupted, start fresh rather than throwing on every operation
                return new LibraryService(libraryFilePath, new List<Photo>());
            }
        }

        public void Save()
        {
            var data = new LibraryData { Photos = _photos };
            var json = JsonSerializer.Serialize(data, SerializerOptions);
            Directory.CreateDirectory(Path.GetDirectoryName(_libraryFilePath) ?? ".");
            File.WriteAllText(_libraryFilePath, json);
        }

        public IReadOnlyList<Photo> ListPhotos()
        {
            return _photos
                .OrderBy(p => p.DateTaken)
                .ThenBy(p => p.FileName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public int ImportFromDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return 0;
            }

            int added = 0;
            foreach (var filePath in Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                if (!IsSupported(filePath))
                {
                    continue;
                }

                var fullPath = Path.GetFullPath(filePath);
                var id = GeneratePhotoIdFromPath(fullPath);
                if (_photos.Any(p => string.Equals(p.Id, id, StringComparison.Ordinal)))
                {
                    continue;
                }

                var fileInfo = new FileInfo(fullPath);
                var dateTaken = GetBestGuessDateTaken(fileInfo);

                var photo = new Photo
                {
                    Id = id,
                    FilePath = fullPath,
                    FileName = fileInfo.Name,
                    DateTaken = dateTaken,
                    Tags = new List<string>()
                };

                _photos.Add(photo);
                added++;
            }

            if (added > 0)
            {
                Save();
            }

            return added;
        }

        public bool AddTag(string photoId, string tag)
        {
            if (string.IsNullOrWhiteSpace(photoId) || string.IsNullOrWhiteSpace(tag))
            {
                return false;
            }

            var normalizedTag = tag.Trim();
            var photo = _photos.FirstOrDefault(p => string.Equals(p.Id, photoId, StringComparison.Ordinal));
            if (photo == null)
            {
                return false;
            }

            if (!photo.Tags.Contains(normalizedTag, StringComparer.OrdinalIgnoreCase))
            {
                photo.Tags.Add(normalizedTag);
                Save();
            }
            return true;
        }

        public IReadOnlyList<Photo> SearchByTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return Array.Empty<Photo>();
            }

            return _photos
                .Where(p => p.Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(p => p.DateTaken)
                .ThenBy(p => p.FileName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool IsSupported(string filePath)
        {
            var ext = Path.GetExtension(filePath);
            if (string.IsNullOrEmpty(ext))
            {
                return false;
            }
            return SupportedExtensions.Contains(ext.ToLowerInvariant());
        }

        private static DateTime GetBestGuessDateTaken(FileInfo fileInfo)
        {
            // Without external libraries, fall back to filesystem times
            // Prefer CreationTime if available, otherwise LastWriteTime; normalize to UTC
            var candidate = fileInfo.CreationTimeUtc;
            if (candidate == default || candidate.Year < 1971)
            {
                candidate = fileInfo.LastWriteTimeUtc;
            }
            return candidate;
        }

        public static string GeneratePhotoIdFromPath(string filePath)
        {
            using var sha = SHA256.Create();
            var normalized = filePath.Replace('\', '/');
            var bytes = Encoding.UTF8.GetBytes(normalized);
            var hash = sha.ComputeHash(bytes);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        private sealed class LibraryData
        {
            public List<Photo> Photos { get; set; } = new();
        }
    }
}

