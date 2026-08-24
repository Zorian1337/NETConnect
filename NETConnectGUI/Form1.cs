using NETConnect;
using NETConnect.Peers;
using NETConnect.Shared.Packet;
using NETConnect.Shared.Packet.Headers;
using System.Net;

using NETConnect.MyExtensions;

namespace NETConnectGUI
{
    public partial class Form1 : Form
    {
        public static Peer Self { get; set; }

        public Form1()
        {
            InitializeComponent();

            Self = new Peer(IPAddress.Any, 0);
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            //lblClientIP.Text = $"Client IP: {Self.}"
            //lblServerIP.Text = $"Server IP: {Self.TCPServer.Address}";
            lblPeerId.Text = $"PeerId: {Self.PeerId.ToString()}";

            // GET THIS TO WORK LATER |             
            Self.TCPServer.OnPeerConnected += (ServerClientHandle Handle, PeerTable table) =>
            {
                MessageBox.Show("Peer Connected");
            };

            Self.TCPServer.OnDebugMessage += (message) =>
            {
                MessageBox.Show($"[DEBUG] {message}");
            };

            //Self.TCPServer.OnPacketReceived += (Helper, Packet) =>
            //{
            //    // Handle received packet here if needed
            //    // Example: MessageBox.Show("Packet received");
            //    MessageBox.Show(BitConverter.ToString(Packet.GetFullPacketSpan().ToArray()));
            //};

            //Self.TCPServer.OnDataReceived += (client, header, data) =>
            //{
            //    // WE ONLY WANT TO SEE BROADCASTED MESSAGES
            //    if (header.Route != PacketRoute.Broadcast) return;

            //    //MessageBox.Show($"TCPServer -> {client.PacketHelper.Self.PeerId} says -> {BitConverter.ToString(data.ToArray())}");
            //    rtbConsole.AppendText($"[TCPServer] received from {header.OriginPeerId} - [{header.Type.ToString()} {header.Action.ToString()}]\n{data.ToArray().ToUTF8String()}\n\n"); //{BitConverter.ToString(data.ToArray())}
            //};

            Self.TCPServer.OnDataReceived += HandleServerDataReceived;

            //MessageBox.Show(Self.PeerId.ToString());

            Task.Run(async () => await WatcherAsync());
        }



        public List<PeerTable> WiredPeers = new List<PeerTable>();
        public async Task WatcherAsync()
        {
            PeriodicTimer timer = new PeriodicTimer(new TimeSpan(0, 0, 1));
            while (!Self.TCPServer.ServerToken.IsCancellationRequested && await timer.WaitForNextTickAsync())
            {
                if(lblServerIP.Text != Self.TCPServer.ServerAddress)
                {
                    lblServerIP.Text = Self.TCPServer.ServerAddress;

                    StripServerStatus.Text = "ServerStatus: Online";
                }



                var NonWiredClients = Self.ConnectedPeers.Where(x => !WiredPeers.Any(z => z.PeerId == x.PeerId));
                if (NonWiredClients is not null && NonWiredClients?.Count() > 0)
                {
                    //MessageBox.Show(string.Join("\n", NonWiredClients.Select(x => $"{x.AddressPort}")));

                    foreach (var peer in NonWiredClients)
                    {
                        peer.Client.OnDataReceived += HandleClientDataReceived;
                        WiredPeers.Add(peer);
                    }
                }

                // REMOVE PEERS THAT ARE NO LONGER CONNECTED
                var InvalidPeers = WiredPeers.Where(x => !Self.ConnectedPeers.Any(z => z.PeerId == x.PeerId));
                if (InvalidPeers is not null && InvalidPeers?.Count() > 0)
                {
                    //MessageBox.Show($"Invalid Peer Detected: {InvalidPeers.Count()}");
                    foreach(var peer in InvalidPeers)
                    {
                        peer.Client.OnDataReceived -= HandleClientDataReceived;
                        WiredPeers.Remove(peer);
                    }

                }

                listPeerView.Invoke(new Action(() =>
                {
                    // Create a copy of the list to prevent "Collection was modified" errors
                    var snapshot = WiredPeers.ToList();

                    listPeerView.DataSource = snapshot;
                    listPeerView.DisplayMember = "PeerId";
                    listPeerView.Refresh();
                }));

                if (Self.TCPServer.MyPeerTable?.DiscoveredPeers is not null || Self.TCPServer.MyPeerTable?.DiscoveredPeers.Count() > 0)
                {
                    listDiscoveredPeers.Invoke(new Action(() =>
                    {
                        // Create a copy of the list to prevent "Collection was modified" errors
                        //var snapshot = WiredPeers.ToList();

                        listDiscoveredPeers.DataSource = Self.TCPServer.MyPeerTable.DiscoveredPeers;
                        listDiscoveredPeers.DisplayMember = "PeerId";
                        listDiscoveredPeers.Refresh();
                    }));
                }


                StripPeerCount.Text = $"Peers: {listPeerView.Items.Count}";
            }
        }

        private void HandleClientDataReceived(PacketHelper client, PacketHeader header, ReadOnlySpan<byte> data)
        {
            // WE ONLY WANT TO SEE BROADCASTED MESSAGES
            //if (header.Route != PacketRoute.Broadcast) return;

            rtbConsole.AppendText($"[TCPClient] received from {header.OriginPeerId} - [{header.Type.ToString()} {header.Action.ToString()}]\n{data.ToArray().ToUTF8String()}\n\n"); //{BitConverter.ToString(data.ToArray())}
            rtbConsole.ScrollToCaret();
        }

        private void HandleServerDataReceived(ServerClientHandle client, PacketHeader header, ReadOnlySpan<byte> data)
        {
            // WE ONLY WANT TO SEE BROADCASTED MESSAGES
            //if (header.Route != PacketRoute.Broadcast) return;

            //MessageBox.Show($"TCPServer -> {client.PacketHelper.Self.PeerId} says -> {BitConverter.ToString(data.ToArray())}");
            rtbConsole.AppendText($"[TCPServer] received from {header.OriginPeerId} - [{header.Type.ToString()} {header.Action.ToString()}]\n{data.ToArray().ToUTF8String()}\n\n"); //{BitConverter.ToString(data.ToArray())}
            rtbConsole.ScrollToCaret();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            Self.Gossip(txtMessage.Text, PacketType.Data, PacketAction.NONE);
        }
    }
}
