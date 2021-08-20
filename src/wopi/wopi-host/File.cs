using System;

namespace FutureNHS.WOPIHost
{
    public struct File
        : IEquatable<File>
    {
        private File(string fileName, string fileVersion)
        {
            Name = fileName;
            Version = fileVersion;

            Id = string.Concat(fileName, '|', fileVersion);
        }

        public string Id { get; }
        public string Name { get; }
        public string Version { get; }

        public bool IsEmpty => string.IsNullOrWhiteSpace(Name) && string.IsNullOrWhiteSpace(Version);

        public static File With(string fileName, string fileVersion)
        {
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException(nameof(fileName));
            if (string.IsNullOrWhiteSpace(fileVersion)) throw new ArgumentNullException(nameof(fileVersion));

            return new File(fileName, fileVersion);
        }

        private static File With(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return new File();

            var segments = id.Split('|', StringSplitOptions.RemoveEmptyEntries);

            if (2 != segments.Length) return new File();

            var fileName = segments[0];
            var fileVersion = segments[1];

            if (string.IsNullOrWhiteSpace(fileName)) return new File();
            if (string.IsNullOrWhiteSpace(fileVersion)) return new File();

            return new File(fileName.Trim(), fileVersion.Trim());
        }


        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;

                hash = hash * 23 + (Name ?? string.Empty).GetHashCode();
                hash = hash * 23 + (Version ?? string.Empty).GetHashCode();

                return hash;
            }
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (!typeof(File).IsAssignableFrom(obj.GetType())) return false;

            return Equals((File)obj);
        }

        public bool Equals(File other)
        {
            if (other.IsEmpty) return IsEmpty;

            if (0 != string.Compare(other.Name, Name, StringComparison.OrdinalIgnoreCase)) return false;
            if (0 != string.CompareOrdinal(other.Version, Version)) return false;

            return true;
        }

        public bool Equals(File? other)
        {
            if (other is null) return IsEmpty;

            return Equals(other.Value);
        }

        public static bool operator ==(File left, File right) => left.Equals(right);
        public static bool operator !=(File left, File right) => !(left.Equals(right));

        public static implicit operator string?(File file) => file.IsEmpty ? default : file.Id;
        public static implicit operator File(string id) => With(id);

        public override string ToString()
        {
            return $"File Name = '{Name ?? "null"}', File Version = '{Version ?? "null"}'";
        }
    }
}
