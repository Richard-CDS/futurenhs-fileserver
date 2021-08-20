using FutureNHS.WOPIHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace FutureNHS_WOPI_Host_UnitTests
{
    [TestClass]
    public sealed class FileTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void With_ThrowsIfFileNameIsNull()
        {
            _ = File.With(fileName: default, fileVersion: "file-version");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void With_ThrowsIfFileNameIsEmpty()
        {
            _ = File.With(fileName: string.Empty, fileVersion: "file-version");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void With_ThrowsIfFileNameIsWhiteSpace()
        {
            _ = File.With(fileName: " ", fileVersion: "file-version");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void With_ThrowsIfFileVersionIsNull()
        {
            _ = File.With(fileName: "file-name", fileVersion: default);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void With_ThrowsIfFileVersionIsEmpty()
        {
            _ = File.With(fileName: "file-name", fileVersion: string.Empty);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void With_ThrowsIfFileVersionIsWhiteSpace()
        {
            _ = File.With(fileName: "file-name", fileVersion: " ");
        }

        [TestMethod]
        public void With_CorrectlyConstructsBasedOnParameters()
        {
            var fileName = "file-name";
            var fileVersion = "file-version";

            var file = File.With(fileName, fileVersion);

            Assert.IsFalse(file.IsEmpty);

            Assert.AreEqual(fileName, file.Name);
            Assert.AreEqual(fileVersion, file.Version);

            var id = string.Concat(fileName, "|", fileVersion);

            Assert.AreEqual(id, file.Id);
        }



        [TestMethod]
        public void IsEmpty_ReturnsTrueForBlindlyConstructedInstant()
        {
            var file = new File();

            Assert.IsTrue(file.IsEmpty);
        }



        [TestMethod]
        public void Equals_IdentifiesWhenInstancesAreEqual()
        {
            var file1 = new File();
            var file2 = new File();

            Assert.IsTrue(file1.Equals(file1));
            Assert.IsTrue(file1.Equals(file2));
            Assert.IsTrue(file2.Equals(file1));
            Assert.IsTrue(file2.Equals(file2));

            file1 = File.With("file-name", "file-version");
            file2 = File.With("file-name", "file-version");

            Assert.IsTrue(file1.Equals(file1));
            Assert.IsTrue(file1.Equals(file2));
            Assert.IsTrue(file2.Equals(file1));
            Assert.IsTrue(file2.Equals(file2));

            var file1AsObj = (object)file1;
            var file2AsObj = (object)file2;

            Assert.IsTrue(file1AsObj.Equals(file1AsObj));
            Assert.IsTrue(file1AsObj.Equals(file1));
            Assert.IsTrue(file1AsObj.Equals(file2));
            Assert.IsTrue(file2AsObj.Equals(file1AsObj));
            Assert.IsTrue(file2AsObj.Equals(file1));
            Assert.IsTrue(file2AsObj.Equals(file2));
            Assert.IsTrue(file1.Equals(file1AsObj));
            Assert.IsTrue(file2.Equals(file1AsObj));
            Assert.IsTrue(file1AsObj.Equals(file2AsObj));
            Assert.IsTrue(file2.Equals(file1AsObj));
            Assert.IsTrue(file2.Equals(file2AsObj));

            var file1AsNullable = new File?(file1);
            var file2AsNullable = new File?(file2);

            Assert.IsTrue(file1AsNullable.Equals(file1));
            Assert.IsTrue(file1AsNullable.Equals(file1AsObj));
            Assert.IsTrue(file1.Equals(file1AsNullable));
            Assert.IsTrue(file1AsObj.Equals(file1AsNullable));
            Assert.IsTrue(file2AsNullable.Equals(file2));
            Assert.IsTrue(file2AsNullable.Equals(file2AsObj));
            Assert.IsTrue(file2.Equals(file2AsNullable));
            Assert.IsTrue(file2AsObj.Equals(file2AsNullable));
            Assert.IsTrue(file1AsNullable.Equals(file2AsNullable));
        }

        [TestMethod]
        public void Equals_IdentifiesWhenInstancesAreNotEqual()
        {
            var file1 = File.With("file-name1", "file-version");
            var file2 = File.With("file-name2", "file-version");

            Assert.IsFalse(file1.Equals(new File()));
            Assert.IsFalse(file2.Equals(new File()));

            Assert.IsFalse(file1.Equals(file2));
            Assert.IsFalse(file2.Equals(file1));
           
            var file1AsObj = (object)file1;
            var file2AsObj = (object)file2;

            Assert.IsFalse(file1AsObj.Equals(file2));
            Assert.IsFalse(file2AsObj.Equals(file1AsObj));
            Assert.IsFalse(file2AsObj.Equals(file1));
            Assert.IsFalse(file2.Equals(file1AsObj));
            Assert.IsFalse(file1AsObj.Equals(file2AsObj));
            Assert.IsFalse(file2.Equals(file1AsObj));
           
            var file1AsNullable = new File?(file1);
            var file2AsNullable = new File?(file2);

            Assert.IsFalse(file1AsNullable.Equals(file2AsNullable));
            Assert.IsFalse(file1AsNullable.Equals(file2AsObj));
            Assert.IsFalse(file1AsNullable.Equals(file2));
            Assert.IsFalse(file2AsNullable.Equals(file1AsNullable));
            Assert.IsFalse(file2AsNullable.Equals(file1AsObj));
            Assert.IsFalse(file2AsNullable.Equals(file1));
        }


        [TestMethod]
        public void GetHashCode_InstancesConstructedTheSameHaveIdenticalHashCodes()
        {
            var file1 = File.With("file-name", "file-version");
            var file2 = File.With("file-name", "file-version");

            var file1HashCode = file1.GetHashCode();
            var file2HashCode = file2.GetHashCode();

            Assert.AreEqual(file1HashCode, file2HashCode);

            file1 = new File();
            file2 = new File();

            file1HashCode = file1.GetHashCode();
            file2HashCode = file2.GetHashCode();

            Assert.AreEqual(file1HashCode, file2HashCode);
        }

        [TestMethod]
        public void GetHashCode_InstancesNotConstructedTheSameHaveDifferentHashCodes()
        {
            var file1 = File.With("file-name1", "file-version");
            var file2 = File.With("file-name2", "file-version");

            var file1HashCode = file1.GetHashCode();
            var file2HashCode = file2.GetHashCode();

            Assert.AreNotEqual(file1HashCode, file2HashCode);
        }

    }
}
