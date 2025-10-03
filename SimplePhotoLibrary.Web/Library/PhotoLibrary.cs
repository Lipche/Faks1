using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SimplePhotoLibrary.Web.Library
{
	public sealed class PhotoLibrary
	{
		private static readonly string[] ImageExtensions =
		{
			".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".webp"
		};

		private readonly List<Photo> _photos = new List<Photo>();
		private readonly Dictionary<string, Album> _albums = new Dictionary<string, Album>(StringComparer.OrdinalIgnoreCase);

		public IReadOnlyList<Photo> Photos => _photos;
		public IReadOnlyDictionary<string, Album> Albums => _albums;

		public Photo? AddPhoto(string filePath)
		{
			if (!IsImageFile(filePath)) return null;

			try
			{
				var photo = new Photo(filePath);

				if (_photos.Any(p => string.Equals(p.FullPath, photo.FullPath, StringComparison.OrdinalIgnoreCase)))
					return null;

				_photos.Add(photo);
				return photo;
			}
			catch
			{
				return null;
			}
		}

		public int AddPhotosFromDirectory(string rootDirectory, bool recursive = true)
		{
			if (string.IsNullOrWhiteSpace(rootDirectory) || !Directory.Exists(rootDirectory))
				return 0;

			var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

			int added = 0;
			foreach (var filePath in Directory.EnumerateFiles(rootDirectory, "*.*", option))
			{
				if (!IsImageFile(filePath)) continue;
				if (AddPhoto(filePath) != null) added++;
			}
			return added;
		}

		public void AddTag(Photo photo, string tag)
		{
			if (photo == null) return;
			if (string.IsNullOrWhiteSpace(tag)) return;
			if (!_photos.Contains(photo)) return;

			photo.Tags.Add(NormalizeTag(tag));
		}

		public IEnumerable<Photo> SearchByFileName(string contains, bool caseInsensitive = true)
		{
			if (string.IsNullOrEmpty(contains)) return Enumerable.Empty<Photo>();

			return caseInsensitive
				? _photos.Where(p => p.FileName.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0)
				: _photos.Where(p => p.FileName.Contains(contains));
		}

		public IEnumerable<Photo> SearchByTag(string tag)
		{
			if (string.IsNullOrWhiteSpace(tag)) return Enumerable.Empty<Photo>();
			var normalized = NormalizeTag(tag);
			return _photos.Where(p => p.Tags.Contains(normalized));
		}

		public IEnumerable<Photo> SearchByDateRange(DateTime? startInclusive, DateTime? endInclusive)
		{
			IEnumerable<Photo> query = _photos;

			if (startInclusive.HasValue)
				query = query.Where(p => p.DateTaken >= startInclusive.Value);

			if (endInclusive.HasValue)
				query = query.Where(p => p.DateTaken <= endInclusive.Value);

			return query;
		}

		public Album CreateAlbum(string name, IEnumerable<Photo> source)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Album name must be provided.", nameof(name));

			var album = new Album(name, source ?? Enumerable.Empty<Photo>());
			_albums[name.Trim()] = album;
			return album;
		}

		public bool TryGetAlbum(string name, out Album album)
		{
			return _albums.TryGetValue(name, out album!);
		}

		public static bool IsImageExtension(string? extension)
		{
			if (string.IsNullOrWhiteSpace(extension)) return false;
			var normalized = extension.StartsWith('.') ? extension : "." + extension;
			return ImageExtensions.Contains(normalized, StringComparer.OrdinalIgnoreCase);
		}

		private static string NormalizeTag(string tag) => tag.Trim();

		private static bool IsImageFile(string? path)
		{
			if (string.IsNullOrWhiteSpace(path)) return false;
			var ext = Path.GetExtension(path);
			if (string.IsNullOrEmpty(ext)) return false;
			return ImageExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
		}
	}
}

