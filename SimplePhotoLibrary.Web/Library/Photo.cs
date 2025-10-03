using System;
using System.Collections.Generic;
using System.IO;

namespace SimplePhotoLibrary.Web.Library
{
	public sealed class Photo
	{
		public string FullPath { get; }
		public string FileName { get; }
		public long FileSizeBytes { get; }
		public DateTime DateTaken { get; }
		public ISet<string> Tags { get; }

		public Photo(string fullPath)
		{
			if (string.IsNullOrWhiteSpace(fullPath))
				throw new ArgumentException("Path must be provided.", nameof(fullPath));

			FullPath = Path.GetFullPath(fullPath);
			FileName = Path.GetFileName(FullPath);

			var info = new FileInfo(FullPath);
			if (!info.Exists)
				throw new FileNotFoundException("Photo file not found.", FullPath);

			FileSizeBytes = info.Length;

			var created = info.CreationTime;
			var modified = info.LastWriteTime;
			DateTaken = created <= modified ? created : modified;

			Tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}

		public override string ToString()
		{
			return $"{FileName} ({FileSizeBytes:N0} bytes, {DateTaken:yyyy-MM-dd}) [{string.Join(", ", Tags)}]";
		}
	}
}

