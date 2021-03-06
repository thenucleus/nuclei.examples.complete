﻿//-----------------------------------------------------------------------
// <copyright company="Nuclei">
//     Copyright 2013 Nuclei. Licensed under the Apache License, Version 2.0.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Nuclei.Communication;
using Nuclei.Communication.Interaction;
using Nuclei.Communication.Protocol;

namespace Nuclei.Examples.Complete
{
    /// <summary>
    /// Defines a set of test commands.
    /// </summary>
    internal sealed class TestCommands
    {
        /// <summary>
        /// The delegate that handles downloads.
        /// </summary>
        private readonly DownloadDataFromRemoteEndpoints m_Download;

        /// <summary>
        /// The function called when an echo command is received.
        /// </summary>
        private readonly Action<EndpointId, string> m_OnEcho;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestCommands"/> class.
        /// </summary>
        /// <param name="download">The delegate used to download data from a remote endpoint.</param>
        /// <param name="onEcho">The function called when an echo command is executed.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if <paramref name="download"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if <paramref name="onEcho"/> is <see langword="null" />.
        /// </exception>
        public TestCommands(DownloadDataFromRemoteEndpoints download, Action<EndpointId, string> onEcho)
        {
            {
                Lokad.Enforce.Argument(() => download);
                Lokad.Enforce.Argument(() => onEcho);
            }

            m_Download = download;
            m_OnEcho = onEcho;
        }

        /// <summary>
        /// Echo's the name.
        /// </summary>
        /// <param name="remoteEndpoint">The endpoint ID of the remote endpoint.</param>
        /// <param name="name">The name.</param>
        public void Echo([InvokingEndpoint]EndpointId remoteEndpoint, string name)
        {
            m_OnEcho(remoteEndpoint, name);
        }

        /// <summary>
        /// Calculates a value from two inputs.
        /// </summary>
        /// <param name="first">The first input.</param>
        /// <param name="second">The second input.</param>
        /// <returns>The calculated value.</returns>
        public int Calculate(int first, int second)
        {
            return first + second;
        }

        /// <summary>
        /// Starts a download.
        /// </summary>
        /// <param name="downloadOwningEndpoint">The endpoint ID of the endpoint that owns the data stream.</param>
        /// <param name="token">The upload token that allows the receiver to indicate which data stream should be downloaded.</param>
        public void StartDownload([InvokingEndpoint]EndpointId downloadOwningEndpoint, UploadToken token)
        {
            var path = Path.Combine(Assembly.GetExecutingAssembly().LocalDirectoryPath(), Path.GetRandomFileName());
            var task = m_Download(downloadOwningEndpoint, token, path, TimeSpan.FromSeconds(15));

            string text;
            try
            {
                task.Wait();
                text = new StreamReader(task.Result.FullName).ReadToEnd();
            }
            catch (AggregateException)
            {
                text = "Failed to download data.";
            }

            m_OnEcho(
                downloadOwningEndpoint,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Downloaded data from: {0}. Data: {1}",
                    downloadOwningEndpoint,
                    text));
        }
    }
}
