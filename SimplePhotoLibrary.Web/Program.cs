using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SimplePhotoLibrary.Web.Library;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(policy => policy.AddDefaultPolicy(p => p
	.AllowAnyOrigin()
	.AllowAnyHeader()
	.AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

var photosRoot = Path.Combine(app.Environment.ContentRootPath, "photos");
Directory.CreateDirectory(photosRoot);

app.UseStaticFiles(new StaticFileOptions
{
	FileProvider = new PhysicalFileProvider(photosRoot),
	RequestPath = "/photos"
});

var library = new PhotoLibrary();
library.AddPhotosFromDirectory(photosRoot, recursive: false);

app.MapGet("/", () => Results.Json(new
{
	name = "SimplePhotoLibrary.Web",
	upload = "/upload",
	list = "/photos/list",
	staticRoot = "/photos"
}));

app.MapGet("/photos/list", () =>
{
	return library.Photos.Select(p => new
	{
		p.FileName,
		p.FileSizeBytes,
		p.DateTaken,
		Tags = p.Tags.ToArray(),
		url = $"/photos/{Uri.EscapeDataString(p.FileName)}"
	});
});

app.MapPost("/upload", async (HttpRequest request) =>
{
	if (!request.HasFormContentType)
		return Results.BadRequest("Expected multipart/form-data.");

	var form = await request.ReadFormAsync();
	if (form.Files.Count == 0)
		return Results.BadRequest("No files provided.");

	var accepted = new List<object>();
	foreach (var file in form.Files)
	{
		var extension = Path.GetExtension(file.FileName);
		if (!PhotoLibrary.IsImageExtension(extension))
			continue;

		var safeName = Path.GetFileName(file.FileName);
		if (string.IsNullOrWhiteSpace(safeName))
			continue;

		var targetPath = Path.Combine(photosRoot, safeName);
		var duplicateIndex = 1;
		while (System.IO.File.Exists(targetPath))
		{
			var baseName = Path.GetFileNameWithoutExtension(safeName);
			var candidate = $"{baseName}_{duplicateIndex}{extension}";
			targetPath = Path.Combine(photosRoot, candidate);
			duplicateIndex++;
		}

		using (var stream = System.IO.File.Create(targetPath))
		{
			await file.CopyToAsync(stream);
		}

		var photo = library.AddPhoto(targetPath);
		if (photo != null)
		{
			accepted.Add(new
			{
				photo.FileName,
				photo.FileSizeBytes,
				photo.DateTaken,
				url = $"/photos/{Uri.EscapeDataString(photo.FileName)}"
			});
		}
	}

	if (accepted.Count == 0)
		return Results.BadRequest("No valid image files were uploaded.");

	return Results.Json(new { added = accepted });
});

app.Run();

