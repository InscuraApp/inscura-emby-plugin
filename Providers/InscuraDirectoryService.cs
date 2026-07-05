using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;

namespace Emby.Plugin.Inscura.Providers
{
    internal sealed class InscuraDirectoryService : IDirectoryService
    {
        private readonly ConcurrentDictionary<string, object> _cache = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<long, BaseItem[]> _children = new ConcurrentDictionary<long, BaseItem[]>();
        private readonly ConcurrentDictionary<long, byte> _taggedItems = new ConcurrentDictionary<long, byte>();

        public FileSystemMetadata[] GetFileSystemEntries(string path)
        {
            return GetFileSystemEntries(path, false);
        }

        public FileSystemMetadata[] GetFileSystemEntries(string path, bool clearCache)
        {
            if (clearCache)
            {
                _cache.TryRemove(path, out _);
            }

            return GetDirectoryInfos(path)
                .Cast<FileSystemInfo>()
                .Concat(GetFileInfos(path))
                .Select(ToMetadata)
                .ToArray();
        }

        public List<FileSystemMetadata> GetFiles(string path)
        {
            return GetFileInfos(path).Select(ToMetadata).ToList();
        }

        public FileSystemMetadata GetFile(string path)
        {
            return ToMetadata(path);
        }

        public FileSystemMetadata GetFile(string path, string fileName, bool resolveShortcuts)
        {
            return ToMetadata(Path.Combine(path, fileName));
        }

        public List<string> GetFilePaths(string path)
        {
            return GetFilePaths(path, false);
        }

        public List<string> GetFilePaths(string path, bool clearCache)
        {
            if (clearCache)
            {
                _cache.TryRemove(path, out _);
            }

            return GetFileInfos(path).Select(file => file.FullName).ToList();
        }

        public void AddOrUpdateCache(string key, object value)
        {
            _cache.AddOrUpdate(key, value, (cacheKey, current) => value);
        }

        public bool TryGetFromCache<T>(string key, out T value)
            where T : class
        {
            if (_cache.TryGetValue(key, out var cached) && cached is T typed)
            {
                value = typed;
                return true;
            }

            value = null;
            return false;
        }

        public BaseItem[] GetCachedValidChildren(long id)
        {
            return _children.TryGetValue(id, out var children) ? children : new BaseItem[0];
        }

        public void SetCachedValidChildren(long id, BaseItem[] children)
        {
            _children[id] = children;
        }

        public bool IsTaggedItemRefreshed(long id)
        {
            return _taggedItems.ContainsKey(id);
        }

        public void MarkTaggedItemRefreshed(long id)
        {
            _taggedItems[id] = 0;
        }

        private static IEnumerable<FileInfo> GetFileInfos(string path)
        {
            return Directory.Exists(path) ? new DirectoryInfo(path).EnumerateFiles() : Enumerable.Empty<FileInfo>();
        }

        private static IEnumerable<DirectoryInfo> GetDirectoryInfos(string path)
        {
            return Directory.Exists(path) ? new DirectoryInfo(path).EnumerateDirectories() : Enumerable.Empty<DirectoryInfo>();
        }

        private static FileSystemMetadata ToMetadata(string path)
        {
            if (File.Exists(path))
            {
                return ToMetadata(new FileInfo(path));
            }

            if (Directory.Exists(path))
            {
                return ToMetadata(new DirectoryInfo(path));
            }

            return new FileSystemMetadata
            {
                Exists = false,
                FullName = path,
                Name = Path.GetFileName(path),
                Extension = Path.GetExtension(path),
                DirectoryName = Path.GetDirectoryName(path),
                LastWriteTimeUtc = DateTimeOffset.MinValue,
                CreationTimeUtc = DateTimeOffset.MinValue
            };
        }

        private static FileSystemMetadata ToMetadata(FileSystemInfo info)
        {
            var fileInfo = info as FileInfo;
            return new FileSystemMetadata
            {
                Exists = info.Exists,
                FullName = info.FullName,
                Name = info.Name,
                Extension = info.Extension,
                Length = fileInfo == null ? 0 : fileInfo.Length,
                DirectoryName = info is DirectoryInfo ? info.FullName : Path.GetDirectoryName(info.FullName),
                LastWriteTimeUtc = info.LastWriteTimeUtc,
                CreationTimeUtc = info.CreationTimeUtc,
                IsDirectory = info is DirectoryInfo
            };
        }
    }
}
