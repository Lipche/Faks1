using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PhotoLibrary.Models;
using PhotoLibrary.Services;

namespace PhotoLibrary
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }

            var command = args[0].ToLowerInvariant();
            var libraryPath = GetOption(args, "--library") ?? Path.Combine(Environment.CurrentDirectory, "library.json");
            var service = LibraryService.Load(libraryPath);

            switch (command)
            {
                case "import":
                    HandleImport(args, service);
                    break;
                case "list":
                    HandleList(service);
                    break;
                case "tag":
                    HandleTag(args, service);
                    break;
                case "search":
                    HandleSearch(args, service);
                    break;
                default:
                    PrintUsage();
                    break;
            }
        }

        private static void HandleImport(string[] args, LibraryService service)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Missing directory. Usage: import <directory> [--library <path>]");
                return;
            }
            var directory = args[1];
            var added = service.ImportFromDirectory(directory);
            Console.WriteLine($"Imported {added} photo(s).");
        }

        private static void HandleList(LibraryService service)
        {
            var photos = service.ListPhotos();
            if (photos.Count == 0)
            {
                Console.WriteLine("No photos in library.");
                return;
            }

            foreach (var p in photos)
            {
                var tags = p.Tags.Count > 0 ? string.Join(", ", p.Tags) : "-";
                Console.WriteLine($"{p.Id[..8]}  {p.DateTaken:yyyy-MM-dd HH:mm}  {p.FileName}  [{tags}]");
            }
        }

        private static void HandleTag(string[] args, LibraryService service)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: tag <photoIdPrefix> <tag> [--library <path>]");
                return;
            }

            var idPrefix = args[1].ToLowerInvariant();
            var tag = args[2];

            var match = service
                .ListPhotos()
                .FirstOrDefault(p => p.Id.StartsWith(idPrefix, StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                Console.WriteLine("Photo not found by id prefix.");
                return;
            }

            if (service.AddTag(match.Id, tag))
            {
                Console.WriteLine("Tag added.");
            }
            else
            {
                Console.WriteLine("Failed to add tag.");
            }
        }

        private static void HandleSearch(string[] args, LibraryService service)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: search <tag> [--library <path>]");
                return;
            }

            var tag = args[1];
            var photos = service.SearchByTag(tag);
            if (photos.Count == 0)
            {
                Console.WriteLine("No matching photos.");
                return;
            }

            foreach (var p in photos)
            {
                var tags = p.Tags.Count > 0 ? string.Join(", ", p.Tags) : "-";
                Console.WriteLine($"{p.Id[..8]}  {p.DateTaken:yyyy-MM-dd HH:mm}  {p.FileName}  [{tags}]");
            }
        }

        private static string? GetOption(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Photo Library - simple CLI\n");
            Console.WriteLine("Commands:");
            Console.WriteLine("  import <dir> [--library <path>]   Import photos from directory");
            Console.WriteLine("  list [--library <path>]           List photos");
            Console.WriteLine("  tag <photoIdPrefix> <tag> [--library <path>]   Add tag to photo");
            Console.WriteLine("  search <tag> [--library <path>]   Search photos by tag");
        }
    }
}

