using NETConnect;
using NETConnect.MyExtensions;
using NETConnect.Shared.Packet;
using NETConnect.Shared.Packet.Headers;
using NETConnectGUI.Packet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NETConnectGUI
{
    public partial class ChatAPP : Form
    {
        public static Peer Self { get; set; }
        public static List<PeerTable> WiredClients { get; set; } = new List<PeerTable>();

        public static (ServerClientHandle, PeerTable) Handle { get; set; }

        public ChatAPP() => InitializeComponent();

        private void ChatAPP_Load(object sender, EventArgs e)
        {
            Task.Run(() => Watcher());
        }

        private void UpdateUI(Action action)
        {
            if (InvokeRequired) Invoke(action);
            else action();
        }

        public void Watcher()
        {
            Self = new Peer(IPAddress.Any, 0);
            //if (Self.FindPeerById(Self.PeerId).HasValue) Handle = Self.FindPeerById(Self.PeerId).Value;

            UpdateUI(() => lblPeerId.Text = $"PeerId: {Self.PeerId} "); //- {Handle.Item1.Id}
                                                                        //{Handle.Item2.AddressPort}
            Self.TCPServer.OnDebugMessage += (string Message) =>
            {
                Task.Run(() => MessageBox.Show(Message));
            };

            while (true)
            {
                Thread.Sleep(100);
                UpdateUI(() => lblAddress.Text = $"Address: {Self.TCPServer.ServerAddress}");
                // Check to see if each peer is listed in @WiredClients

                if (Self.ConnectedPeers.Count() > 0)
                {
                    //&& !Self.ConnectedPeers.Any(x => WiredClients.Any(a => a.PeerId == x.PeerId))


                    var NonWiredClients = Self.ConnectedPeers.Where(x => (!WiredClients.Any(a => a.PeerId == x.PeerId)));//Self.ConnectedPeers.Where(x => !WiredClients.Any(a => x.PeerId == a.PeerId));
                    if (NonWiredClients?.Count() > 0)
                    {
                        //Task.Run(() => MessageBox.Show(string.Join("\n", NonWiredClients.Select(x => $"{x.AddressPort}"))));

                        foreach (var client in NonWiredClients)
                        {
                            client.Client.OnDataReceived += HandleOnClientDataReceived;

                            UpdateUI(() => listPeers.Items.Add($"{client.PeerId} - {client.AddressPort}"));
                            WiredClients.Add(client);
                        }
                    }

                }
                else if (WiredClients.Count() > Self.ConnectedPeers.Count())
                {
                    // Remove disconnected peers
                }
            }

        }

        private void HandleOnClientDataReceived(PacketHelper helper, PacketHeader header, ReadOnlySpan<byte> span)
        {
            //string Data = span.ToArray().ToUTF8String();
            //UpdateUI(() => rtbMessages.AppendText($"{header.PacketAction} - {Data}\n\n"));

            // Im tired of seeing ping pong in my messages
            if (header.PacketAction == PacketActionType.Ping || header.PacketAction == PacketActionType.Pong) return;
            else if (header.PacketAction == PacketActionType.Data)
            {
                string JSON = span.ToArray().ToUTF8String();

                // Check if this is the right type of data

                if (JSON.IsValidJSON(out MessagePacket packet)) 
                {
                    //{header.PacketAction} - {Data}
                    UpdateUI(() => rtbMessages.AppendText($"{packet.SentAt} {packet.Author}: {packet.Message}\n\n"));
                    UpdateUI(() => rtbMessages.ScrollToCaret());
                }
            }
        }

        public void HandlePeerConnected(ServerClientHandle handle, PeerTable table)
        {
            listPeers.Items.Add($"{table.PeerId}"); // - {table.Address}:{table.Port}
        }

        // Later we want to use this application as the frontier for creating custom "communities or applications" that the p2p service is used for 
        private void btnSendMessage_Click(object sender, EventArgs e) {

            if (txtUsername.Text.Length <= 3) { MessageBox.Show("Username must be longer than 3 characters!", "Invalid username length", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

            MessagePacket message = new MessagePacket(txtUsername.Text.Trim(), txtMessage.Text.Trim());

            // Somehow during broadcasts its super delayed at least on first connect not really sure why
            Task.Run(() => Self.Broadcast(message.ToJSON(), PacketActionType.Data));

            UpdateUI(() => rtbMessages.AppendText($"{message.SentAt} {message.Author}: {message.Message}\n\n"));
            UpdateUI(() => rtbMessages.ScrollToCaret());
        }
    }
}
