using FutureNHS.WOPIHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FutureNHS_WOPI_Host_UnitTests
{
    [TestClass]
    public sealed class FileMetadataTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CTor_ThrowsIfTitleIsNull()
        {
           _ = new FileMetadata(
               title: default,
               description: "description",
               version: "version",
               owner: "owner",
               name: "name",
               extension: "extension",
               sizeInBytes: 999,
               lastWriteTime: DateTimeOffset.UtcNow, 
               contentHash: "content-hash"
               );
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CTor_ThrowsIfTitleIsEmpty()
        {
            _ = new FileMetadata(
                title: string.Empty,
                description: "description",
                version: "version",
                owner: "owner",
                name: "name",
                extension: "extension",
                sizeInBytes: 999,
                lastWriteTime: DateTimeOffset.UtcNow,
                contentHash: "content-hash"
                );
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CTor_ThrowsIfTitleIsWhitespace()
        {
            _ = new FileMetadata(
                title: " ",
                description: "description",
                version: "version",
                owner: "owner",
                name: "name",
                extension: "extension",
                sizeInBytes: 999,
                lastWriteTime: DateTimeOffset.UtcNow,
                contentHash: "content-hash"
                );
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CTor_ThrowsIfDescriptionIsNull()
        {
            _ = new FileMetadata(
                title: "title",
                description: default,
                version: "version",
                owner: "owner",
                name: "name",
                extension: "extension",
                sizeInBytes: 999,
                lastWriteTime: DateTimeOffset.UtcNow,
                contentHash: "content-hash"
                );
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CTor_ThrowsIfDescriptionIsEmpty()
        {
            _ = new FileMetadata(
                title: "title",
                description: string.Empty,
                version: "version",
                owner: "owner",
                name: "name",
                extension: "extension",
                sizeInBytes: 999,
                lastWriteTime: DateTimeOffset.UtcNow,
                contentHash: "content-hash"
                );
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CTor_ThrowsIfDescriptionIsWhitespace()
        {
            _ = new FileMetadata(
                title: "title",
                description: " ",
                version: "version",
                owner: "owner",
                name: "name",
                extension: "extension",
                sizeInBytes: 999,
                lastWriteTime: DateTimeOffset.UtcNow,
                contentHash: "content-hash"
                );
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CTor_ThrowsIfVersionIsNull()
        {
            _ = new FileMetadata(
                title: "title",
                description: "description",
                version: default,
                owner: "owner",
                name: "name",
                extension: "extension",
                sizeInBytes: 999,
                lastWriteTime: DateTimeOffset.UtcNow,
                contentHash: "content-hash"
                );
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CTor_ThrowsIfVersionIsEmpty()
        {
            _ = new FileMetadata(
                title: "title",
                description: "description",
                version: string.Empty,
                owner: "owner",
                name: "name",
                extension: "extension",
                sizeInBytes: 999,
                lastWriteTime: DateTimeOffset.UtcNow,
                contentHash: "content-hash"
                 );
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CTor_ThrowsIfVersionIsWhitespace()
        {
            _ = new FileMetadata(
                title: "title",
                description: "description",
                version: " ",
                owner: "owner",
                name: "name",
                extension: "extension",
                sizeInBytes: 999,
                lastWriteTime: DateTimeOffset.UtcNow,
                contentHash: "content-hash"
                 );
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CTor_ThrowsIfOwnerIsNull()
        {
            _ = new FileMetadata(
                title: "title",
                description: "description",
                version: "version",
                owner: default,
                name: "name",
                extension: "extension",
                sizeInBytes: 999,
                lastWriteTime: DateTimeOffset.UtcNow,
                contentHash: "content-hash"
                 );
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CTor_ThrowsIfOwnerIsEmpty()
        {
            _ = new FileMetadata(
                title: "title",
                description: "description",
                version: "version",
                owner: string.Empty,
                name: "name",
                extension: "extension",
                sizeInBytes: 999,
                lastWriteTime: DateTimeOffset.UtcNow,
                contentHash: "content-hash"
                 );
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CTor_ThrowsIfOwnerIsWhitespace()
        {
            _ = new FileMetadata(
                title: "title",
                description: "description",
                version: "version",
                owner: " ",
                name: "name",
                extension: "extension",
                sizeInBytes: 999,
                lastWriteTime: DateTimeOffset.UtcNow,
                contentHash: "content-hash"
                 );
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CTor_ThrowsIfNameIsNull()
        {
            _ = new FileMetadata(
                title: "title",
                description: "description",
                version: "version",
                owner: "owner",
                name: default,
                extension: "extension",
                sizeInBytes: 999,
                lastWriteTime: DateTimeOffset.UtcNow,
                contentHash: "content-hash"
                 );
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CTor_ThrowsIfNameIsEmpty()
        {
            _ = new FileMetadata(
                title: "title",
                description: "description",
                version: "version",
                owner: "owner",
                name: string.Empty,
                extension: "extension",
                sizeInBytes: 999,
                lastWriteTime: DateTimeOffset.UtcNow,
                contentHash: "content-hash"
                 );
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CTor_ThrowsIfNameIsWhitespace()
        {
            _ = new FileMetadata(
                title: "title",
                description: "description",
                version: "version",
                owner: "owner",
                name: " ",
                extension: "extension",
                sizeInBytes: 999,
                lastWriteTime: DateTimeOffset.UtcNow,
                contentHash: "content-hash"
                 );
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CTor_ThrowsIfExtensionIsNull()
        {
            _ = new FileMetadata(
                title: "title",
                description: "description",
                version: "version",
                owner: "owner",
                name: "name",
                extension: default,
                sizeInBytes: 999,
                lastWriteTime: DateTimeOffset.UtcNow,
                contentHash: "content-hash"
                 );
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CTor_ThrowsIfExtensionIsEmpty()
        {
            _ = new FileMetadata(
                title: "title",
                description: "description",
                version: "version",
                owner: "owner",
                name: "name",
                extension: string.Empty,
                sizeInBytes: 999,
                lastWriteTime: DateTimeOffset.UtcNow,
                contentHash: "content-hash"
                 );
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CTor_ThrowsIfExtensionIsWhitespace()
        {
            _ = new FileMetadata(
                title: "title",
                description: "description",
                version: "version",
                owner: "owner",
                name: "name",
                extension: " ",
                sizeInBytes: 999,
                lastWriteTime: DateTimeOffset.UtcNow,
                contentHash: "content-hash"
                 );
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void CTor_ThrowsIfExtensionIsOneCharacterLong()
        {
            _ = new FileMetadata(
                title: "title",
                description: "description",
                version: "version",
                owner: "owner",
                name: "name",
                extension: "e",
                sizeInBytes: 0,
                lastWriteTime: DateTimeOffset.UtcNow,
                contentHash: "content-hash"
                 );
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void CTor_ThrowsIfExtensionIsTwoCharactersLong()
        {
            _ = new FileMetadata(
                title: "title",
                description: "description",
                version: "version",
                owner: "owner",
                name: "name",
                extension: "ex",
                sizeInBytes: 0,
                lastWriteTime: DateTimeOffset.UtcNow,
                contentHash: "content-hash"
                 );
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void CTor_ThrowsIfExtensionIsThreeCharactersLong()
        {
            _ = new FileMetadata(
                title: "title",
                description: "description",
                version: "version",
                owner: "owner",
                name: "name",
                extension: "ext",
                sizeInBytes: 0,
                lastWriteTime: DateTimeOffset.UtcNow,
                contentHash: "content-hash"
                 );
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void CTor_ThrowsIfSizeInBytesIsZero()
        {
            _ = new FileMetadata(
                title: "title",
                description: "description",
                version: "version",
                owner: "owner",
                name: "name",
                extension: "extension",
                sizeInBytes: 0,
                lastWriteTime: DateTimeOffset.UtcNow,
                contentHash: "content-hash"
                 );
        }



        [TestMethod]
        public void CTor_DoesNotThrowIfExtensionIsFourOrMoreCharactersLong()
        {
            var rnd = new Random();

            for (var n = 1; n < 50; n++)
            {
                _ = new FileMetadata(
                    title: "title",
                    description: "description",
                    version: "version",
                    owner: "owner",
                    name: "name",
                    extension: new string('x', rnd.Next(4, 500)),
                    sizeInBytes: 1,
                    lastWriteTime: DateTimeOffset.UtcNow,
                    contentHash: "content-hash"
                     );
            }
        }

        [TestMethod]
        public void CTor_DoesNotThrowIfSizeInBytesIsGreaterThanZero()
        {
            _ = new FileMetadata(
                title: "title",
                description: "description",
                version: "version",
                owner: "owner",
                name: "name",
                extension: "extension",
                sizeInBytes: 1,
                lastWriteTime: DateTimeOffset.UtcNow,
                contentHash: "content-hash"
                 );
        }

        [TestMethod]
        public void CTor_DoesNotThrowIfSizeInBytesIsMaxValue()
        {
            _ = new FileMetadata(
                title: "title",
                description: "description",
                version: "version",
                owner: "owner",
                name: "name",
                extension: "extension",
                sizeInBytes: ulong.MaxValue,
                lastWriteTime: DateTimeOffset.UtcNow,
                contentHash: "content-hash"
                 );
        }




        [TestMethod]
        public void CTor_CorrectlyInitialisesProperties()
        {
            var title = "title";
            var description = "description";
            var version = "version";
            var owner = "owner";
            var name = "name";
            var extension = "extension";
            var sizeInBytes = ulong.MaxValue;
            var lastWriteTime = DateTimeOffset.UtcNow;
            var contentHash = "content-hash";

            var fileMetadata = new FileMetadata(
                title: title, 
                description: description,
                version: version, 
                owner: owner,
                name: name,
                extension: extension,
                sizeInBytes: sizeInBytes,
                lastWriteTime: lastWriteTime,
                contentHash: contentHash
                );

            Assert.AreEqual(title, fileMetadata.Title);
            Assert.AreEqual(description, fileMetadata.Description);
            Assert.AreEqual(version, fileMetadata.Version);
            Assert.AreEqual(owner, fileMetadata.Owner);
            Assert.AreEqual(name, fileMetadata.Name);
            Assert.AreEqual(extension, fileMetadata.Extension);
            Assert.AreEqual(sizeInBytes, fileMetadata.SizeInBytes);
            Assert.AreEqual(lastWriteTime.ToString("o", CultureInfo.InvariantCulture), fileMetadata.LastWriteTimeIso8601);
            Assert.AreEqual(contentHash, fileMetadata.ContentHash);
        }
    }
}
