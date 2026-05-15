using System;
using DocumentManagementApp.Core.Interfaces;
using DocumentManagementApp.Clients.Client1;
using DocumentManagementApp.Clients.Client2;
using DocumentManagementApp.Clients.Client3;

namespace DocumentManagementApp.Core.Factory
{
    /// <summary>
    /// Responsible for creating the appropiate ICLienteExporter instance based on the client name provided.
    /// </summary>
    public static class ClientExporterFactory
    {
        ///<summary>
        /// Returns the correct exporter for the given client identifier.
        /// </summary>
        ///<param name = "clientName" > The client identifier string.</param>
        ///<returns> An implementation of IClientExporter.</returns>
        ///<exception cref="ArgumentException">Thown when the client name is not recognized.</exception>
        public static IClientExporter GetClientExporter(string clientName)
        {
            switch (clientName.ToLower())
            {
                case "client1":
                    return new Client1Exporter();
                case "client2":
                    return new Client2Exporter();
                case "client3-billing":
                    return new Client3BillingExporter();
                case "client3-settlements":
                    return new Client3SettlementsExporter();
                default:
                    throw new ArgumentException($"Unrecognized client name: {clientName}");
            }
    }
}
