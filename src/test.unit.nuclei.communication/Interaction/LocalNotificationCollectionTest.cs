﻿//-----------------------------------------------------------------------
// <copyright company="Nuclei">
//     Copyright 2013 Nuclei. Licensed under the Apache License, Version 2.0.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Moq;
using Nuclei.Communication.Interaction.Transport.Messages;
using Nuclei.Communication.Protocol;
using NUnit.Framework;

namespace Nuclei.Communication.Interaction
{
    [TestFixture]
    [SuppressMessage("Microsoft.StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented",
        Justification = "Unit tests do not need documentation.")]
    public sealed class LocalNotificationCollectionTest
    {
        public interface IMockNotificationSet : INotificationSet
        {
            event EventHandler OnMyEvent;

            event EventHandler<UnhandledExceptionEventArgs> OnMyOtherEvent;
        }

        public sealed class MockNotificationSet : IMockNotificationSet
        {
            public event EventHandler OnMyEvent;

            public void RaiseOnMyEvent(EventArgs eventArgs)
            {
                var local = OnMyEvent;
                if (local != null)
                {
                    local(this, eventArgs);
                }
            }

            public event EventHandler<UnhandledExceptionEventArgs> OnMyOtherEvent;

            public void RaiseOnMyOtherEvent(UnhandledExceptionEventArgs eventArgs)
            {
                var local = OnMyOtherEvent;
                if (local != null)
                {
                    local(this, eventArgs);
                }
            }
        }

        [Test]
        public void Register()
        {
            var knownEndpoint = new EndpointId("other");
            var layer = new Mock<IProtocolLayer>();
            {
                layer.Setup(l => l.Id)
                    .Returns(new EndpointId("mine"));
                layer.Setup(l => l.IsSignedIn)
                    .Returns(true);
                layer.Setup(l => l.KnownEndpoints())
                    .Returns(
                        new List<EndpointId> 
                            { 
                                knownEndpoint, 
                            });
                layer.Setup(l => l.SendMessageTo(It.IsAny<EndpointId>(), It.IsAny<ICommunicationMessage>(), It.IsAny<int>()))
                    .Verifiable();
            }

            var collection = new LocalNotificationCollection(layer.Object);

            var id = NotificationId.Create(typeof(IMockNotificationSet).GetEvent("OnMyEvent"));
            var def = new NotificationDefinition(id);
            collection.Register(new[] { def });

            Assert.IsTrue(collection.Any(pair => pair.Equals(id)));
            layer.Verify(l => l.SendMessageTo(It.IsAny<EndpointId>(), It.IsAny<ICommunicationMessage>(), It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void RegisterWithoutBeingSignedIn()
        {
            var layer = new Mock<IProtocolLayer>();
            {
                layer.Setup(l => l.Id)
                    .Returns(new EndpointId("mine"));
                layer.Setup(l => l.IsSignedIn)
                    .Returns(false);
                layer.Setup(l => l.SendMessageTo(It.IsAny<EndpointId>(), It.IsAny<ICommunicationMessage>(), It.IsAny<int>()))
                    .Verifiable();
            }

            var collection = new LocalNotificationCollection(layer.Object);

            var id = NotificationId.Create(typeof(IMockNotificationSet).GetEvent("OnMyEvent"));
            var def = new NotificationDefinition(id);
            collection.Register(new[] { def });

            Assert.AreEqual(1, collection.Count(pair => pair.Equals(id)));
            layer.Verify(l => l.SendMessageTo(It.IsAny<EndpointId>(), It.IsAny<ICommunicationMessage>(), It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void RaiseNormalEvent()
        {
            var knownEndpoint = new EndpointId("other");
            EndpointId other = null;
            ICommunicationMessage msg = null;
            var layer = new Mock<IProtocolLayer>();
            {
                layer.Setup(l => l.Id)
                    .Returns(new EndpointId("mine"));
                layer.Setup(l => l.IsSignedIn)
                    .Returns(true);
                layer.Setup(l => l.KnownEndpoints())
                    .Returns(
                        new List<EndpointId> 
                            { 
                                knownEndpoint, 
                            });
                layer.Setup(l => l.SendMessageTo(It.IsAny<EndpointId>(), It.IsAny<ICommunicationMessage>(), It.IsAny<int>()))
                    .Callback<EndpointId, ICommunicationMessage, int>(
                        (e, m, r) =>
                        {
                            other = e;
                            msg = m;
                        })
                    .Verifiable();
            }

            var collection = new LocalNotificationCollection(layer.Object);

            var id = NotificationId.Create(typeof(IMockNotificationSet).GetEvent("OnMyEvent"));
            var def = new NotificationDefinition(id);
            var obj = new MockNotificationSet();
            obj.OnMyEvent += def.ForwardToListeners;
            collection.Register(new[] { def });

            Assert.AreEqual(1, collection.Count(pair => pair.Equals(id)));
            layer.Verify(l => l.SendMessageTo(It.IsAny<EndpointId>(), It.IsAny<ICommunicationMessage>(), It.IsAny<int>()), Times.Never());

            collection.RegisterForNotification(knownEndpoint, id);

            var args = new EventArgs();
            obj.RaiseOnMyEvent(args);

            layer.Verify(l => l.SendMessageTo(It.IsAny<EndpointId>(), It.IsAny<ICommunicationMessage>(), It.IsAny<int>()), Times.Once());
            Assert.AreEqual(knownEndpoint, other);
            Assert.IsInstanceOf<NotificationRaisedMessage>(msg);

            var notificationMsg = msg as NotificationRaisedMessage;
            Assert.AreEqual(id, notificationMsg.Notification.Notification);
            Assert.AreSame(args, notificationMsg.Notification.EventArgs);
        }

        [Test]
        public void RaiseTypedEvent()
        {
            var knownEndpoint = new EndpointId("other");
            EndpointId other = null;
            ICommunicationMessage msg = null;
            var layer = new Mock<IProtocolLayer>();
            {
                layer.Setup(l => l.Id)
                    .Returns(new EndpointId("mine"));
                layer.Setup(l => l.IsSignedIn)
                    .Returns(true);
                layer.Setup(l => l.KnownEndpoints())
                    .Returns(
                        new List<EndpointId> 
                            { 
                                knownEndpoint, 
                            });
                layer.Setup(l => l.SendMessageTo(It.IsAny<EndpointId>(), It.IsAny<ICommunicationMessage>(), It.IsAny<int>()))
                    .Callback<EndpointId, ICommunicationMessage, int>(
                        (e, m, r) =>
                        {
                            other = e;
                            msg = m;
                        })
                    .Verifiable();
            }

            var collection = new LocalNotificationCollection(layer.Object);

            var id = NotificationId.Create(typeof(IMockNotificationSet).GetEvent("OnMyOtherEvent"));
            var def = new NotificationDefinition(id);
            var obj = new MockNotificationSet();
            obj.OnMyOtherEvent += def.ForwardToListeners;
            collection.Register(new[] { def });

            Assert.AreEqual(1, collection.Count(pair => pair.Equals(id)));
            layer.Verify(l => l.SendMessageTo(It.IsAny<EndpointId>(), It.IsAny<ICommunicationMessage>(), It.IsAny<int>()), Times.Never());

            collection.RegisterForNotification(knownEndpoint, id);

            var args = new UnhandledExceptionEventArgs(new Exception(), false);
            obj.RaiseOnMyOtherEvent(args);

            layer.Verify(l => l.SendMessageTo(It.IsAny<EndpointId>(), It.IsAny<ICommunicationMessage>(), It.IsAny<int>()), Times.Once());
            Assert.AreEqual(knownEndpoint, other);
            Assert.IsInstanceOf<NotificationRaisedMessage>(msg);

            var notificationMsg = msg as NotificationRaisedMessage;
            Assert.AreEqual(id, notificationMsg.Notification.Notification);
            Assert.AreSame(args, notificationMsg.Notification.EventArgs);
        }

        [Test]
        public void UnregisterFromNotification()
        {
            var knownEndpoint = new EndpointId("other");
            EndpointId other = null;
            ICommunicationMessage msg = null;
            var layer = new Mock<IProtocolLayer>();
            {
                layer.Setup(l => l.Id)
                    .Returns(new EndpointId("mine"));
                layer.Setup(l => l.IsSignedIn)
                    .Returns(true);
                layer.Setup(l => l.KnownEndpoints())
                    .Returns(
                        new List<EndpointId> 
                            { 
                                knownEndpoint, 
                            });
                layer.Setup(l => l.SendMessageTo(It.IsAny<EndpointId>(), It.IsAny<ICommunicationMessage>(), It.IsAny<int>()))
                    .Callback<EndpointId, ICommunicationMessage, int>(
                        (e, m, r) =>
                        {
                            other = e;
                            msg = m;
                        })
                    .Verifiable();
            }

            var collection = new LocalNotificationCollection(layer.Object);

            var id = NotificationId.Create(typeof(IMockNotificationSet).GetEvent("OnMyEvent"));
            var def = new NotificationDefinition(id);
            var obj = new MockNotificationSet();
            obj.OnMyEvent += def.ForwardToListeners;
            collection.Register(new[] { def });

            Assert.AreEqual(1, collection.Count(pair => pair.Equals(id)));
            layer.Verify(l => l.SendMessageTo(It.IsAny<EndpointId>(), It.IsAny<ICommunicationMessage>(), It.IsAny<int>()), Times.Never());

            collection.RegisterForNotification(knownEndpoint, id);
            collection.UnregisterFromNotification(knownEndpoint, id);

            other = null;
            msg = null;

            var args = new EventArgs();
            obj.RaiseOnMyEvent(args);

            layer.Verify(l => l.SendMessageTo(It.IsAny<EndpointId>(), It.IsAny<ICommunicationMessage>(), It.IsAny<int>()), Times.Never());
            Assert.IsNull(other);
            Assert.IsNull(msg);
        }
    }
}
