/* ========================================================================
 * Copyright (c) 2005-2013 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;
using System.IO;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.Controls;

namespace WebSocketsTestClient
{
    /// <summary>
    /// The main form for a simple Quickstart Client application.
    /// </summary>
    public partial class MainForm : Form
    {
        #region Constructors
        /// <summary>
        /// Creates an empty form.
        /// </summary>
        private MainForm()
        {
            InitializeComponent();
            this.Icon = ClientUtils.GetAppIcon();
        }
        
        /// <summary>
        /// Creates a form which uses the specified client configuration.
        /// </summary>
        /// <param name="configuration">The configuration to use.</param>
        public MainForm(ApplicationConfiguration configuration)
        {
            InitializeComponent();
            this.Icon = ClientUtils.GetAppIcon();

            ConnectServerCTRL.Configuration = m_configuration = configuration;
            // ConnectServerCTRL.ServerUrl = "opc.wss://prototyping.opcfoundation.org:65200/";
            ConnectServerCTRL.ServerUrl = "opc.wss://blackice:65200/";
            this.Text = m_configuration.ApplicationName;
        }
        #endregion

        #region Private Fields
        private ApplicationConfiguration m_configuration;
        private Session m_session;
        private bool m_connectedOnce;
        #endregion

        #region Private Methods
        #endregion

        #region Event Handlers
        /// <summary>
        /// Connects to a server.
        /// </summary>
        private async void Server_ConnectMI_Click(object sender, EventArgs e)
        {
            try
            {
                await ConnectServerCTRL.ConnectAsync();
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Disconnects from the current session.
        /// </summary>
        private async void Server_DisconnectMI_Click(object sender, EventArgs e)
        {
            try
            {
                await ConnectServerCTRL.DisconnectAsync();
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Prompts the user to choose a server on another host.
        /// </summary>
        private void Server_DiscoverMI_Click(object sender, EventArgs e)
        {
            try
            {
                ConnectServerCTRL.Discover(null);
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Updates the application after connecting to or disconnecting from the server.
        /// </summary>
        private void Server_ConnectComplete(object sender, EventArgs e)
        {
            try
            {
                m_session = ConnectServerCTRL.Session;

                // set a suitable initial state.
                if (m_session != null && !m_connectedOnce)
                {
                    m_connectedOnce = true;
                }

                // browse the instances in the server.
                BrowseCTRL.Initialize(m_session, ObjectIds.ObjectsFolder, ReferenceTypeIds.Organizes, ReferenceTypeIds.Aggregates);
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Updates the application after a communicate error was detected.
        /// </summary>
        private void Server_ReconnectStarting(object sender, EventArgs e)
        {
            try
            {
                BrowseCTRL.ChangeSession(null);
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Updates the application after reconnecting to the server.
        /// </summary>
        private void Server_ReconnectComplete(object sender, EventArgs e)
        {
            try
            {
                m_session = ConnectServerCTRL.Session;
                BrowseCTRL.ChangeSession(m_session);
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        /// <summary>
        /// Cleans up when the main form closes.
        /// </summary>
        private async void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            await ConnectServerCTRL.DisconnectAsync();
        }

        private async Task DoAnsiCServerTest(Uri endpointUrl)
        {
            LogTextBox.Clear();
            LogTextBox.AppendText(String.Format("[Connecting to EndpointUrl] {0}", endpointUrl));
            LogTextBox.AppendText(Environment.NewLine);

            var messageContext = m_configuration.CreateMessageContext();
            var endpointConfiguration = EndpointConfiguration.Create(m_configuration);

            IList<ApplicationDescription> servers = null;
            IList<EndpointDescription> endpoints = null;

            await Task.Run(() =>
            {
                using (var channel = DiscoveryChannel.Create(m_configuration, endpointUrl, endpointConfiguration, messageContext))
                {
                    var client = new DiscoveryClient(channel);
                    servers = client.FindServers(null);
                    endpoints = client.GetEndpoints(null);
                    client.Close();
                }
            });

            foreach (var server in servers)
            {
                LogTextBox.AppendText(String.Format("[ApplicationDescription] {0}|{1}", server.ApplicationName, server.ApplicationUri));
                LogTextBox.AppendText(Environment.NewLine);
            }

            EndpointDescription description = null;

            foreach (var endpoint in endpoints)
            {
                LogTextBox.AppendText(String.Format("[EndpointDescription] {0}|{1}", endpoint.EndpointUrl, endpoint.TransportProfileUri));
                LogTextBox.AppendText(Environment.NewLine);

                if (new Uri(endpoint.EndpointUrl).Scheme == endpointUrl.Scheme && endpoint.SecurityMode != MessageSecurityMode.None)
                {
                    description = endpoint;
                }
            }

            LogTextBox.AppendText(String.Format("[Connecting to EndpointUrl] {0} {1}", description.EndpointUrl, description.SecurityMode));
            LogTextBox.AppendText(Environment.NewLine);

            DataValueCollection values = null;
            DiagnosticInfoCollection diagnosticInfos = null;

            await Task.Run(() =>
            {
                var channel = SessionChannel.Create(
                     m_configuration,
                     description,
                     endpointConfiguration,
                     m_configuration.SecurityConfiguration.ApplicationCertificate.Find(true),
                     messageContext);

                using (channel)
                {
                    var client = new SessionClient(channel);

                    ReadValueIdCollection nodesToRead = new ReadValueIdCollection();

                    nodesToRead.Add(new ReadValueId()
                    {
                        NodeId = new NodeId("Node1", 1),
                        AttributeId = Attributes.Value
                    });

                    nodesToRead.Add(new ReadValueId()
                    {
                        NodeId = new NodeId("Node2", 1),
                        AttributeId = Attributes.Value
                    });

                    var response = client.Read(null, 0, TimestampsToReturn.Both, nodesToRead, out values, out diagnosticInfos);
                }
            });

            foreach (var value in values)
            {
                LogTextBox.AppendText(String.Format("[DataValue] {0}", value.WrappedValue.ToString()));
                LogTextBox.AppendText(Environment.NewLine);
            }
        }

        private async void Test_AnsiCServerWithWebSockets_Click(object sender, EventArgs e)
        {
            try
            {
                var url = new Uri("opc.wss://localhost:48043");
                await DoAnsiCServerTest(url);
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }

        private async void Test_AnsiCServerWithTcp_Click(object sender, EventArgs e)
        {
            try
            {
                var url = new Uri("opc.tcp://localhost:48040");
                await DoAnsiCServerTest(url);
            }
            catch (Exception exception)
            {
                ClientUtils.HandleException(this.Text, exception);
            }
        }
        #endregion
    }
}
