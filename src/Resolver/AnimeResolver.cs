using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.Resolver.Api;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Resolver.Resolver
{
	enum FileType
	{
		FolderAnime, // Can not be nested
		FolderExtra, // Can not be nested, always has anime as parent

		FileEpisode, // Always has anime as parent

		// Todo: FileRecap, FileOpening, FileEnding etc.
		FileExtra, // Always has extra as parent

		Unknown // No clue what this file is
	}

	public class AnimeEpisodeResolver : IItemResolver, IMultiItemResolver
	{
		private static readonly string[] VideoExtensions =
		{
			".mkv",
			".mp4"
		};

		public ResolverPriority Priority => ResolverPriority.Plugin;
		private readonly ILogger<AnimeEpisodeResolver> _logger;
		private readonly IServerApplicationPaths _appPaths;

		public AnimeEpisodeResolver(ILogger<AnimeEpisodeResolver> logger, IServerApplicationPaths appPaths)
		{
			_logger = logger;
			_appPaths = appPaths;
		}

		private static FileType GetFolderType(string path)
		{
			var basename = Path.GetFileName(path);

			if (string.IsNullOrEmpty(basename)) return FileType.Unknown;

			FileType type;

			return FileType.FolderAnime;
		}

		private static FileType? GetFileType(string path, string parentPath, bool isDirectory)
		{
			var type = FileType.Unknown;

			if (isDirectory)
			{
				type = GetFolderType(path);
			}
			else
			{
				var parentType = GetFolderType(parentPath);
				var extension = Path.GetExtension(path);
				var isVideo = VideoExtensions.Contains(extension);

				if (isVideo && parentType == FileType.FolderExtra) type = FileType.FileExtra;
				else if (isVideo && parentType == FileType.FolderAnime) type = FileType.FileEpisode;
			}

			return type;
		}

		public BaseItem ResolvePath(ItemResolveArgs args)
		{
			// Only for tv shows folders
			// Empty collection type is "programdata" folder
			if (args.CollectionType == null ||
			    !args.CollectionType.Equals(CollectionType.TvShows, StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}

			// Only enable for anime libraries (todo: better detection)
			if (args.Path.IndexOf("anime", 0, StringComparison.OrdinalIgnoreCase) == -1) return null;

			var type = GetFileType(args.Path, args.Parent.Path, args.IsDirectory);
			_logger.LogDebug($"{args.Path} is {type}");

			if (type == FileType.FolderAnime)
			{
				var name = Regex.Replace(args.FileInfo.Name, @"^\d+\.\s", "");

				return new Series
				{
					Path = args.Path,
					Name = name,
					SortName = name,
					ForcedSortName = name
				};
			}
			else if (type == FileType.FolderExtra)
			{
				var path = new Season
				{
					Path = args.Path,
					Name = args.LibraryOptions.SeasonZeroDisplayName,
					IndexNumber = 0,
				};

				return path;
			}
			else if (type == FileType.FileEpisode)
			{
				var episode = new Episode
				{
					Path = args.Path,
					SortName = args.FileInfo.Name,
					ForcedSortName = args.FileInfo.Name,
					ParentIndexNumber = 1 // Set as "first season" item
				};

				var anitomy = new Anitomy(args.FileInfo.Name);
				var episodeNumber = anitomy.GetEpisodeNumberAsInt();

				// Set name
				if (anitomy.EpisodeTitle != null)
					episode.Name = anitomy.EpisodeTitle;
				// else if (episodeNumber != null)
				// 	episode.Name = $"Episode {episodeNumber}";
				else
					episode.Name = args.FileInfo.Name;

				// Set index
				if (episodeNumber != null)
					episode.IndexNumber = episodeNumber;

				return episode;
			}

			return null;
		}

		private MultiItemResolverResult ResolveVideos(
			Folder parent,
			IEnumerable<FileSystemMetadata> fileSystemEntries,
			string collectionType,
			IDirectoryService directoryService
		)
		{
			var files = new List<FileSystemMetadata>();
			var leftOver = new List<FileSystemMetadata>();

			// Loop through each child file/folder and see if we find a video
			foreach (var child in fileSystemEntries)
			{
				if (child.IsDirectory)
				{
					leftOver.Add(child);
				}
				else
				{
					files.Add(child);
				}
			}

			var items = new List<BaseItem>();

			foreach (var file in files)
			{
				var item = ResolvePath(new ItemResolveArgs(_appPaths, directoryService)
				{
					Parent = parent,
					CollectionType = collectionType,
					FileInfo = file,
					FileSystemChildren = Array.Empty<FileSystemMetadata>(),
					LibraryOptions = new LibraryOptions()
				});
				if (item == null) leftOver.Add(file);
				else items.Add(item);
			}

			return new MultiItemResolverResult
			{
				ExtraFiles = leftOver,
				Items = items,
			};
		}

		public MultiItemResolverResult ResolveMultiple(Folder parent, List<FileSystemMetadata> files,
			string collectionType, IDirectoryService directoryService)
		{
			if (string.Equals(collectionType, CollectionType.TvShows, StringComparison.OrdinalIgnoreCase))
			{
				// Only enable for anime libraries (todo: better detection)
				if (parent.Path.IndexOf("anime", 0, StringComparison.OrdinalIgnoreCase) == -1) return null;

				return ResolveVideos(parent, files, collectionType, directoryService);
			}

			return null;
		}
	}
}