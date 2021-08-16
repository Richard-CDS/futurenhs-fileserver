﻿using FutureNHS.WOPIHost;
using FutureNHS.WOPIHost.Configuration;
using FutureNHS.WOPIHost.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FutureNHS_WOPI_Host_UnitTests
{
    [TestClass]
    public sealed class WopiRequestTests
    {
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task HandleAsync_ThrowsIfWopiRequestIsEmpty()
        {
            var cancellationToken = new CancellationToken();

            var httpContext = new DefaultHttpContext();

            var httpRequest = httpContext.Request;

            httpRequest.Method = HttpMethods.Get;
            httpRequest.Path = "/wopi/files/fileidgoeshere";
            httpRequest.QueryString = new QueryString("?access_token=<expired-access-token>");

            var wopiRequest = WopiRequest.EMPTY;

            await wopiRequest.HandleAsync(httpContext, cancellationToken);
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task HandleAsync_ThrowsWhenCancellationTokenCancelledy()
        {
            using var cts = new CancellationTokenSource();

            cts.Cancel();

            var features = new Features();

            var snapshot = new Moq.Mock<IOptionsSnapshot<Features>>();

            snapshot.SetupGet(x => x.Value).Returns(features);

            IWopiRequestFactory wopiRequestFactory = new WopiRequestFactory(features: snapshot.Object);

            var httpContext = new DefaultHttpContext();

            var httpRequest = httpContext.Request;

            httpRequest.Method = HttpMethods.Get;
            httpRequest.Path = "/wopi/files/fileidgoeshere";
            httpRequest.QueryString = new QueryString("?access_token=<valid-access-token>");

            var wopiRequest = wopiRequestFactory.CreateRequest(request: httpContext.Request);

            await wopiRequest.HandleAsync(httpContext, cts.Token);
        }
    }
}
