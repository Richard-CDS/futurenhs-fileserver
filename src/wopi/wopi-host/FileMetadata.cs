using System;
using System.Globalization;

namespace FutureNHS.WOPIHost
{
    public enum FileStatus: UInt16
    {
        Uploading = 1, 
        Uploaded = 2,
        Failed = 3,
        Verified = 4,
        Quarantined = 5,
        Recycled = 6,
        Deleted = 7
    }

    public sealed class FileMetadata
    {
        internal static FileMetadata EMPTY = new FileMetadata();

        private FileMetadata() { }

        public FileMetadata(string title, string description, string version, string owner, string name, string extension, ulong sizeInBytes, string blobName, DateTimeOffset lastWriteTime, string contentHash, FileStatus fileStatus)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentNullException(nameof(title));
            if (string.IsNullOrWhiteSpace(description)) throw new ArgumentNullException(nameof(description));
            if (string.IsNullOrWhiteSpace(version)) throw new ArgumentNullException(nameof(version));
            if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentNullException(nameof(owner));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(extension)) throw new ArgumentNullException(nameof(extension));
            if (string.IsNullOrWhiteSpace(blobName)) throw new ArgumentNullException(nameof(blobName));
            if (string.IsNullOrWhiteSpace(contentHash)) throw new ArgumentNullException(nameof(contentHash));

            if (3 > extension.Length) throw new ArgumentOutOfRangeException(nameof(extension), "The file extension needs to be at least 3 characters long");
            if (0 >= sizeInBytes) throw new ArgumentOutOfRangeException(nameof(sizeInBytes), "The file size needs to be greater than 0 bytes");

            Title = title;
            Description = description;
            Version = version;
            Owner = owner;
            Name = name;
            Extension = extension;
            BlobName = blobName;
            SizeInBytes = sizeInBytes;
            LastWriteTimeIso8601 = lastWriteTime.ToString("o", CultureInfo.InvariantCulture); // TODO - Check this isn't a crazy future date?
            ContentHash = contentHash;
            FileStatus = fileStatus;
        }

        public bool IsEmpty => ReferenceEquals(this, EMPTY);

        public string? Title { get; }
        public string? Description { get; }

        public string? Name { get; }
        public string? Version { get; }
        public string? Extension { get; }

        public string? BlobName { get; }
        public string? ContentHash { get; }

        public string? Owner { get; } 

        public ulong? SizeInBytes { get; }
        public FileStatus? FileStatus { get; }
        public string? LastWriteTimeIso8601 { get; } 
    }
}
