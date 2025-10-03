using System;
using System.Collections.Generic;
using System.Linq;

namespace SimplePhotoLibrary.Web.Library
{
	public sealed class Album
	{
		public string Name { get; }
		private readonly List<Photo> _photos;

		public Album(string name, IEnumerable<Photo> photos)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Album name must be provided.", nameof(name));

			Name = name.Trim();
			_photos = new List<Photo>(photos ?? Enumerable.Empty<Photo>());
		}

		public IReadOnlyList<Photo> Photos => _photos;

		public int Count => _photos.Count;

		public void Add(Photo photo)
		{
			if (photo == null) return;
			if (_photos.Contains(photo)) return;
			_photos.Add(photo);
		}
	}
}

