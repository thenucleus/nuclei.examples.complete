﻿//-----------------------------------------------------------------------
// <copyright company="Nuclei">
//     Copyright 2013 Nuclei. Licensed under the Apache License, Version 2.0.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Reactive.Testing;
using Moq;
using Nuclei.Communication.Protocol.Messages;
using Nuclei.Communication.Protocol.Messages.Processors;
using Nuclei.Diagnostics;
using NUnit.Framework;

namespace Nuclei.Communication.Protocol
{
    [TestFixture]
    [SuppressMessage("Microsoft.StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented",
                Justification = "Unit tests do not need documentation.")]
    public sealed class MessageHandlerTest
    {
        [Test]
        public void ForwardResponse()
        {
            var store = new Mock<IStoreInformationAboutEndpoints>();
            {
                store.Setup(s => s.CanCommunicateWithEndpoint(It.IsAny<EndpointId>()))
                    .Returns(false);
            }

            var systemDiagnostics = new SystemDiagnostics((p, s) => { }, null);
            var handler = new MessageHandler(store.Object, systemDiagnostics);

            var endpoint = new EndpointId("sendingEndpoint");
            var messageId = new MessageId();
            var timeout = TimeSpan.FromSeconds(30);
            var task = handler.ForwardResponse(endpoint, messageId, timeout);
            Assert.IsFalse(task.IsCompleted);

            var msg = new SuccessMessage(endpoint, messageId);
            handler.ProcessMessage(msg);

            task.Wait();
            Assert.IsTrue(task.IsCompleted);
            Assert.AreSame(msg, task.Result);
        }

        [Test]
        public void ForwardResponseWithMessageReceiveTimeout()
        {
            var store = new Mock<IStoreInformationAboutEndpoints>();
            {
                store.Setup(s => s.CanCommunicateWithEndpoint(It.IsAny<EndpointId>()))
                    .Returns(false);
            }

            var systemDiagnostics = new SystemDiagnostics((p, s) => { }, null);
            var scheduler = new TestScheduler();
            var handler = new MessageHandler(store.Object, systemDiagnostics, scheduler);

            var endpoint = new EndpointId("sendingEndpoint");
            var messageId = new MessageId();
            var timeout = TimeSpan.FromSeconds(30);
            var task = handler.ForwardResponse(endpoint, messageId, timeout);
            Assert.IsFalse(task.IsCompleted);
            Assert.IsFalse(task.IsCanceled);

            scheduler.Start();

            Assert.Throws<AggregateException>(task.Wait);
            Assert.IsTrue(task.IsCompleted);
            Assert.IsFalse(task.IsCanceled);
            Assert.IsTrue(task.IsFaulted);
        }

        [Test]
        public void ForwardResponseWithDisconnectingEndpoint()
        {
            var store = new Mock<IStoreInformationAboutEndpoints>();
            {
                store.Setup(s => s.CanCommunicateWithEndpoint(It.IsAny<EndpointId>()))
                    .Returns(false);
            }

            var systemDiagnostics = new SystemDiagnostics((p, s) => { }, null);
            var handler = new MessageHandler(store.Object, systemDiagnostics);

            var endpoint = new EndpointId("sendingEndpoint");
            var messageId = new MessageId();
            var timeout = TimeSpan.FromSeconds(30);
            var task = handler.ForwardResponse(endpoint, messageId, timeout);
            Assert.IsFalse(task.IsCompleted);
            Assert.IsFalse(task.IsCanceled);

            handler.OnEndpointSignedOff(endpoint);

            Assert.Throws<AggregateException>(task.Wait);
            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(task.IsCanceled);
        }

        [Test]
        public void ActOnArrivalWithMessageFromNonBlockedSender()
        {
            ICommunicationMessage storedMessage = null;
            var processAction = new Mock<IMessageProcessAction>();
            {
                processAction.Setup(p => p.Invoke(It.IsAny<ICommunicationMessage>()))
                    .Callback<ICommunicationMessage>(m => { storedMessage = m; });
            }

            var store = new Mock<IStoreInformationAboutEndpoints>();
            {
                store.Setup(s => s.CanCommunicateWithEndpoint(It.IsAny<EndpointId>()))
                    .Returns(true);
            }

            var systemDiagnostics = new SystemDiagnostics((p, s) => { }, null);
            var handler = new MessageHandler(store.Object, systemDiagnostics);

            handler.ActOnArrival(new MessageKindFilter(typeof(SuccessMessage)), processAction.Object);

            var endpoint = new EndpointId("sendingEndpoint");
            var msg = new SuccessMessage(endpoint, MessageId.None);
            handler.ProcessMessage(msg);

            Assert.AreSame(msg, storedMessage);
        }

        [Test]
        public void ActOnArrivalWithMessageFromBlockedSender()
        {
            ICommunicationMessage storedMessage = null;
            var processAction = new Mock<IMessageProcessAction>();
            {
                processAction.Setup(p => p.Invoke(It.IsAny<ICommunicationMessage>()))
                    .Callback<ICommunicationMessage>(m => { storedMessage = m; });
            }

            var store = new Mock<IStoreInformationAboutEndpoints>();
            {
                store.Setup(s => s.CanCommunicateWithEndpoint(It.IsAny<EndpointId>()))
                    .Returns(false);
            }

            var systemDiagnostics = new SystemDiagnostics((p, s) => { }, null);
            var handler = new MessageHandler(store.Object, systemDiagnostics);

            handler.ActOnArrival(new MessageKindFilter(typeof(SuccessMessage)), processAction.Object);

            var endpoint = new EndpointId("sendingEndpoint");
            var msg = new SuccessMessage(endpoint, new MessageId());
            handler.ProcessMessage(msg);

            Assert.IsNull(storedMessage);
        }

        [Test]
        public void ActOnArrivalWithHandshakeMessage()
        {
            ICommunicationMessage storedMessage = null;
            var processAction = new Mock<IMessageProcessAction>();
            {
                processAction.Setup(p => p.Invoke(It.IsAny<ICommunicationMessage>()))
                    .Callback<ICommunicationMessage>(m => { storedMessage = m; });
            }

            var store = new Mock<IStoreInformationAboutEndpoints>();
            {
                store.Setup(s => s.CanCommunicateWithEndpoint(It.IsAny<EndpointId>()))
                    .Returns(false);
            }

            var systemDiagnostics = new SystemDiagnostics((p, s) => { }, null);
            var handler = new MessageHandler(store.Object, systemDiagnostics);

            handler.ActOnArrival(new MessageKindFilter(typeof(EndpointConnectMessage)), processAction.Object);

            var endpoint = new EndpointId("sendingEndpoint");
            var msg = new EndpointConnectMessage(
                endpoint, 
                new DiscoveryInformation(new Uri("http://localhost/discovery/invalid")), 
                new ProtocolInformation( 
                    new Version(), 
                    new Uri(@"net.pipe://localhost/test"), 
                    new Uri(@"net.pipe://localhost/test/data")),
                new ProtocolDescription(new List<CommunicationSubject>()));
            handler.ProcessMessage(msg);

            Assert.AreSame(msg, storedMessage);
        }

        [Test]
        public void ActOnArrivalWithLastChanceHandler()
        {
            var localEndpoint = new EndpointId("id");

            ICommunicationMessage storedMsg = null;
            SendMessage sendAction = 
                (e, m, r) =>
                {
                    storedMsg = m;
                };

            var store = new Mock<IStoreInformationAboutEndpoints>();
            {
                store.Setup(s => s.CanCommunicateWithEndpoint(It.IsAny<EndpointId>()))
                    .Returns(false);
            }

            var systemDiagnostics = new SystemDiagnostics((p, s) => { }, null);

            var processAction = new UnknownMessageTypeProcessAction(localEndpoint, sendAction, systemDiagnostics);
            var handler = new MessageHandler(store.Object, systemDiagnostics);
            handler.ActOnArrival(new MessageKindFilter(processAction.MessageTypeToProcess), processAction);

            var endpoint = new EndpointId("sendingEndpoint");
            var msg = new EndpointConnectMessage(
                endpoint,
                new DiscoveryInformation(new Uri("http://localhost/discovery/invalid")),
                new ProtocolInformation(
                    new Version(),
                    new Uri(@"net.pipe://localhost/test"),
                    new Uri(@"net.pipe://localhost/test/data")),
                new ProtocolDescription(new List<CommunicationSubject>()));
            handler.ProcessMessage(msg);

            Assert.IsInstanceOf<FailureMessage>(storedMsg);
        }

        [Test]
        public void OnLocalChannelClosed()
        {
            var store = new Mock<IStoreInformationAboutEndpoints>();
            {
                store.Setup(s => s.CanCommunicateWithEndpoint(It.IsAny<EndpointId>()))
                    .Returns(false);
            }

            var systemDiagnostics = new SystemDiagnostics((p, s) => { }, null);
            var handler = new MessageHandler(store.Object, systemDiagnostics);

            var endpoint = new EndpointId("sendingEndpoint");
            var messageId = new MessageId();
            var timeout = TimeSpan.FromSeconds(30);
            var task = handler.ForwardResponse(endpoint, messageId, timeout);
            Assert.IsFalse(task.IsCompleted);
            Assert.IsFalse(task.IsCanceled);

            handler.OnLocalChannelClosed();

            Assert.Throws<AggregateException>(task.Wait);
            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(task.IsCanceled);
        }
    }
}
