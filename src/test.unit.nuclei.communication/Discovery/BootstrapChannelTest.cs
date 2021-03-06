﻿//-----------------------------------------------------------------------
// <copyright company="Nuclei">
//     Copyright 2013 Nuclei. Licensed under the Apache License, Version 2.0.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.ServiceModel;
using System.ServiceModel.Description;
using Moq;
using NUnit.Framework;

namespace Nuclei.Communication.Discovery
{
    [TestFixture]
    [SuppressMessage("Microsoft.StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented",
                Justification = "Unit tests do not need documentation.")]
    public sealed class BootstrapChannelTest
    {
        [Test]
        public void OpenChannel()
        {
            var id = EndpointIdExtensions.CreateEndpointIdForCurrentProcess();
            var baseUri = new Uri("http://localhost/invalid");
            var versionedEndpointUri = new Uri("http://localhost/invalid/v1");
            var template = new Mock<IDiscoveryChannelTemplate>();
            {
                template.Setup(
                    t => t.AttachDiscoveryEntryEndpoint(It.IsAny<ServiceHost>(), It.IsAny<Type>(), It.IsAny<EndpointId>(), It.IsAny<bool>()))
                    .Returns(new ServiceEndpoint(new ContractDescription("a"), new NetNamedPipeBinding(), new EndpointAddress(baseUri)));
            }

            var versionedEndpointCounter = 0;
            var versionedEndpoint = new Mock<IVersionedDiscoveryEndpoint>();
            Func<Version, ChannelTemplate, Tuple<Type, IVersionedDiscoveryEndpoint>> endpointBuilder =
                (v, t) =>
                {
                    versionedEndpointCounter++;
                    return new Tuple<Type, IVersionedDiscoveryEndpoint>(versionedEndpoint.Object.GetType(), versionedEndpoint.Object);
                };

           BootstrapEndpoint baseEndpoint = null;
            var host = new Mock<IHoldServiceConnections>();
            {
                host.Setup(h => h.OpenChannel(It.IsAny<IReceiveInformationFromRemoteEndpoints>(), It.IsAny<Func<ServiceHost, ServiceEndpoint>>()))
                    .Callback<IReceiveInformationFromRemoteEndpoints, Func<ServiceHost, ServiceEndpoint>>(
                        (h, e) =>
                        {
                            e(null);
                            baseEndpoint = h as BootstrapEndpoint;
                        })
                    .Returns<IReceiveInformationFromRemoteEndpoints, Func<ServiceHost, ServiceEndpoint>>(
                        (h, e) => (h is IVersionedDiscoveryEndpoint) ? versionedEndpointUri : baseUri)
                    .Verifiable();
            }

            Func<IHoldServiceConnections> hostBuilder = () => host.Object;

            Uri entryAddress = null;
            Action<Uri> storage = u => entryAddress = u;

            var channel = new BootstrapChannel(
                id,
                template.Object,
                endpointBuilder,
                hostBuilder,
                storage);
            
            channel.OpenChannel(true);

            host.Verify(
                h => h.OpenChannel(It.IsAny<IReceiveInformationFromRemoteEndpoints>(), It.IsAny<Func<ServiceHost, ServiceEndpoint>>()),
                Times.Exactly(2));
            Assert.AreEqual(1, versionedEndpointCounter);
            Assert.AreEqual(baseUri, entryAddress);
            Assert.IsNotNull(baseEndpoint);
            Assert.AreEqual(1, baseEndpoint.DiscoveryVersions().Length);
            Assert.AreEqual(versionedEndpointUri, baseEndpoint.UriForVersion(baseEndpoint.DiscoveryVersions()[0]));
        }

        [Test]
        public void CloseChannel()
        {
            var id = EndpointIdExtensions.CreateEndpointIdForCurrentProcess();
            var template = new Mock<IDiscoveryChannelTemplate>();

            var versionedEndpoint = new Mock<IVersionedDiscoveryEndpoint>();
            Func<Version, ChannelTemplate, Tuple<Type, IVersionedDiscoveryEndpoint>> endpointBuilder =
                (v, t) => new Tuple<Type, IVersionedDiscoveryEndpoint>(versionedEndpoint.Object.GetType(), versionedEndpoint.Object);

            var host = new Mock<IHoldServiceConnections>();
            {
                host.Setup(h => h.OpenChannel(It.IsAny<IReceiveInformationFromRemoteEndpoints>(), It.IsAny<Func<ServiceHost, ServiceEndpoint>>()))
                    .Returns<IReceiveInformationFromRemoteEndpoints, Func<ServiceHost, ServiceEndpoint>>(
                        (h, e) => new Uri("http://localhost/invalid"));
                host.Setup(h => h.CloseConnection())
                    .Verifiable();
            }

            Func<IHoldServiceConnections> hostBuilder = () => host.Object;
            Action<Uri> storage = u => { };

            var channel = new BootstrapChannel(
                id,
                template.Object,
                endpointBuilder,
                hostBuilder,
                storage);

            channel.OpenChannel(true);
            channel.CloseChannel();

            host.Verify(h => h.CloseConnection(), Times.Exactly(2));
        }
    }
}
