using FutureNHS.WOPIHost;
using FutureNHS.WOPIHost.Configuration;
using FutureNHS.WOPIHost.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace FutureNHS_WOPI_Host_UnitTests
{
    [TestClass]
    public sealed class WopiRequestFactoryTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_ThrowsIfFileRepositoryIsNull()
        {
            var features = new Moq.Mock<IOptionsSnapshot<Features>>().Object;

            _ = new WopiRequestFactory(fileRepository: default, features: features);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_ThrowsIfFeaturesOptionsConfigurationIsNull()
        {
            var fileRepository = new Moq.Mock<IFileRepository>().Object;

            _ = new WopiRequestFactory(fileRepository, features: default);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_ThrowsIfFeaturesConfigurationIsNull()
        {
            var fileRepository = new Moq.Mock<IFileRepository>().Object;

            var snapshot = new Moq.Mock<IOptionsSnapshot<Features>>();

            snapshot.SetupGet(x => x.Value).Returns(default(Features));

            _ = new WopiRequestFactory(fileRepository, features: snapshot.Object);
        }



        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CreateRequest_ThrowsIfHttpRequestIsNull()
        {
            var fileRepository = new Moq.Mock<IFileRepository>().Object;

            var features = new Features();

            var snapshot = new Moq.Mock<IOptionsSnapshot<Features>>();

            snapshot.SetupGet(x => x.Value).Returns(features);

            IWopiRequestFactory wopiRequestFactory = new WopiRequestFactory(fileRepository, features: snapshot.Object);

            _ = wopiRequestFactory.CreateRequest(request: default);
        }

        [TestMethod]
        public void CreateRequest_NoneWopiRequestIsIdentifiedAndIgnored()
        {
            var fileRepository = new Moq.Mock<IFileRepository>().Object;

            var features = new Features();

            var snapshot = new Moq.Mock<IOptionsSnapshot<Features>>();

            snapshot.SetupGet(x => x.Value).Returns(features);

            IWopiRequestFactory wopiRequestFactory = new WopiRequestFactory(fileRepository, features: snapshot.Object);

            var httpContext = new DefaultHttpContext();

            var wopiRequest = wopiRequestFactory.CreateRequest(request: httpContext.Request);

            Assert.IsNotNull(wopiRequest);
            
            Assert.IsTrue(wopiRequest.IsEmpty, "Expected a none WOPI request to be ignored and return an empty marker");
        }

        [TestMethod]
        public void CreateRequest_WopiRequestWithMissingAccessTokenIsIdentifiedAndIgnored()
        {
            var fileRepository = new Moq.Mock<IFileRepository>().Object;

            var features = new Features();

            var snapshot = new Moq.Mock<IOptionsSnapshot<Features>>();

            snapshot.SetupGet(x => x.Value).Returns(features);

            IWopiRequestFactory wopiRequestFactory = new WopiRequestFactory(fileRepository, features: snapshot.Object);

            var httpContext = new DefaultHttpContext();

            var httpRequest = httpContext.Request;

            httpRequest.Method = HttpMethods.Get;
            httpRequest.Path = "/wopi/files/fileidgoeshere";

            var wopiRequest = wopiRequestFactory.CreateRequest(request: httpContext.Request);

            Assert.IsNotNull(wopiRequest);

            Assert.IsTrue(wopiRequest.IsEmpty, "Expected a WOPI request with a missing access token to be ignored and return an empty marker");
        }

        [TestMethod]
        public void CreateRequest_WopiRequestWithInvalidAccessTokenIsIdentifiedAndIgnored()
        {
            var fileRepository = new Moq.Mock<IFileRepository>().Object;

            var features = new Features();

            var snapshot = new Moq.Mock<IOptionsSnapshot<Features>>();

            snapshot.SetupGet(x => x.Value).Returns(features);

            IWopiRequestFactory wopiRequestFactory = new WopiRequestFactory(fileRepository, features: snapshot.Object);

            var httpContext = new DefaultHttpContext();

            var httpRequest = httpContext.Request;

            httpRequest.Method = HttpMethods.Get;
            httpRequest.Path = "/wopi/files/fileidgoeshere";
            httpRequest.QueryString = new QueryString("?access_token=<invalid-access-token>");

            httpRequest.Headers["X-WOPI-ItemVersion"] = "file-version";

            var wopiRequest = wopiRequestFactory.CreateRequest(request: httpContext.Request);

            Assert.IsNotNull(wopiRequest);

            Assert.IsTrue(wopiRequest.IsEmpty, "Expected a WOPI request with an invalid access token to be ignored and return an empty marker");
        }

        [TestMethod]
        public void CreateRequest_IdentifiesCheckFileInfoRequest()
        {
            var fileRepository = new Moq.Mock<IFileRepository>().Object;

            var features = new Features();

            var snapshot = new Moq.Mock<IOptionsSnapshot<Features>>();

            snapshot.SetupGet(x => x.Value).Returns(features);

            IWopiRequestFactory wopiRequestFactory = new WopiRequestFactory(fileRepository, features: snapshot.Object);

            var httpContext = new DefaultHttpContext();

            var httpRequest = httpContext.Request;

            httpRequest.Method = HttpMethods.Get;
            httpRequest.Path = "/wopi/files/fileidgoeshere";
            httpRequest.QueryString = new QueryString("?access_token=<valid-access-token>");

            httpRequest.Headers["X-WOPI-ItemVersion"] = "file-version";

            var wopiRequest = wopiRequestFactory.CreateRequest(request: httpContext.Request);

            Assert.IsInstanceOfType(wopiRequest, typeof(CheckFileInfoWopiRequest), "Expected Check File Info requests to be identified");

            var checkFileInfoRequest = (CheckFileInfoWopiRequest)wopiRequest;

            Assert.AreEqual("<valid-access-token>", checkFileInfoRequest.AccessToken, "Expected the access token to be extracted and retained");
        }

        [TestMethod]
        public void CreateRequest_IdentifiesGetFileRequest()
        {
            var fileRepository = new Moq.Mock<IFileRepository>().Object;

            var features = new Features();

            var snapshot = new Moq.Mock<IOptionsSnapshot<Features>>();

            snapshot.SetupGet(x => x.Value).Returns(features);

            IWopiRequestFactory wopiRequestFactory = new WopiRequestFactory(fileRepository, features: snapshot.Object);

            var httpContext = new DefaultHttpContext();

            var httpRequest = httpContext.Request;

            httpRequest.Method = HttpMethods.Get;
            httpRequest.Path = "/wopi/files/fileidgoeshere/contents";
            httpRequest.QueryString = new QueryString("?access_token=<valid-access-token>");

            httpRequest.Headers["X-WOPI-ItemVersion"] = "file-version";

            var wopiRequest = wopiRequestFactory.CreateRequest(request: httpContext.Request);

            Assert.IsInstanceOfType(wopiRequest, typeof(GetFileWopiRequest), "Expected Get File requests to be identified");

            var getFileInfoRequest = (GetFileWopiRequest)wopiRequest;

            Assert.AreEqual("<valid-access-token>", getFileInfoRequest.AccessToken, "Expected the access token to be extracted and retained");
        }

        [TestMethod]
        public void CreateRequest_IdentifiesSaveFileRequest()
        {
            var fileRepository = new Moq.Mock<IFileRepository>().Object;

            var features = new Features();

            var snapshot = new Moq.Mock<IOptionsSnapshot<Features>>();

            snapshot.SetupGet(x => x.Value).Returns(features);

            IWopiRequestFactory wopiRequestFactory = new WopiRequestFactory(fileRepository, features: snapshot.Object);

            var httpContext = new DefaultHttpContext();

            var httpRequest = httpContext.Request;

            httpRequest.Method = HttpMethods.Post;
            httpRequest.Path = "/wopi/files/fileidgoeshere/contents";
            httpRequest.QueryString = new QueryString("?access_token=<valid-access-token>");

            var wopiRequest = wopiRequestFactory.CreateRequest(request: httpContext.Request);

            Assert.IsInstanceOfType(wopiRequest, typeof(PostFileWopiRequest), "Expected Save File requests to be identified");

            var postFileInfoRequest = (PostFileWopiRequest)wopiRequest;

            Assert.AreEqual("<valid-access-token>", postFileInfoRequest.AccessToken, "Expected the access token to be extracted and retained");
        }

        [TestMethod]
        public void CreateRequest_IdentifiesAndIgnoresFolderRequest()
        {
            var fileRepository = new Moq.Mock<IFileRepository>().Object;

            var features = new Features();

            var snapshot = new Moq.Mock<IOptionsSnapshot<Features>>();

            snapshot.SetupGet(x => x.Value).Returns(features);

            IWopiRequestFactory wopiRequestFactory = new WopiRequestFactory(fileRepository, features: snapshot.Object);

            var httpContext = new DefaultHttpContext();

            var httpRequest = httpContext.Request;

            httpRequest.Method = HttpMethods.Post;
            httpRequest.Path = "/wopi/folders/";
            httpRequest.QueryString = new QueryString("?access_token=<valid-access-token>");

            var wopiRequest = wopiRequestFactory.CreateRequest(request: httpContext.Request);

            Assert.IsTrue(wopiRequest.IsEmpty, "Expected folder based requests to be ignored");
        }
    }
}
