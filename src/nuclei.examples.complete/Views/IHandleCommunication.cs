﻿//-----------------------------------------------------------------------
// <copyright company="Nuclei">
//     Copyright 2013 Nuclei. Licensed under the Apache License, Version 2.0.
// </copyright>
//-----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Nuclei.Communication;

namespace Nuclei.Examples.Complete.Views
{
    /// <summary>
    /// Defines the interface for objects that transmit communication commands 
    /// to the communication layer.
    /// </summary>
    internal interface IHandleCommunication
    {
        /// <summary>
        /// Gets a value indicating whether the communication layer has been
        /// activated.
        /// </summary>
        bool IsConnected 
        { 
            get;
        }

        /// <summary>
        /// Returns a value indicating if connection information is available for
        /// the given endpoint.
        /// </summary>
        /// <param name="endpointId">The endpoint.</param>
        /// <returns>
        /// <see langword="true" /> if connection information is available for the given endpoint;
        /// otherwise, <see langword="false" />.
        /// </returns>
        [SuppressMessage("Microsoft.StyleCop.CSharp.DocumentationRules", "SA1628:DocumentationTextMustBeginWithACapitalLetter",
            Justification = "Documentation can start with a language keyword")]
        bool CanContactEndpoint(EndpointId endpointId);

        /// <summary>
        /// Sends an echo message to the given endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to which the message should be send.</param>
        /// <param name="messageText">The text of the message.</param>
        void SendEchoMessageTo(EndpointId endpoint, string messageText);

        /// <summary>
        /// Sends a message to the given endpoint with the request to add the numbers.
        /// </summary>
        /// <param name="endpoint">The endpoint to which the message should be send.</param>
        /// <param name="first">The first number.</param>
        /// <param name="second">The second number.</param>
        /// <returns>The result of the addition.</returns>
        Task<int> AddNumbers(EndpointId endpoint, int first, int second);

        /// <summary>
        /// Sends a data stream to the given endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to which the data should be send.</param>
        /// <param name="dataText">The text.</param>
        void SendDataTo(EndpointId endpoint, string dataText);

        /// <summary>
        /// Sends a notification to the given endpoint.
        /// </summary>
        void Notify();
    }
}
