using NETConnect.MyExtensions;
using NETConnect.Shared.Packet;
using NETConnect.Shared.Packet.Headers;
using System.ComponentModel;
using System.Net;

namespace NETConnectGUI
{
    public partial class Form1 : Form
    {
        public static Peer Self { get; set; }

        //public event Action<>

        public static List<PeerTable> WiredClients { get; set; } = new List<PeerTable>();


        public Form1() => InitializeComponent();

        private void Form1_Load(object sender, EventArgs e)
        {
            Self = new Peer(IPAddress.Any, 0);

            lblPeerId.Text = $"PeerId: {Self.PeerId}";

            Self.TCPServer.OnPeerConnected += (ServerClientHandle Handle, PeerTable table) =>
            {
                MessageBox.Show("Peer Connected");
            };



            //Self..OnDataReceived += HandleOnDataReceived;
            Task.Run(() => Watcher());
        }


        public void Watcher()
        {
            //MessageBox.Show("test");

            

            while (true)
            {
                Thread.Sleep(100);

                // Check to see if each peer is listed in @WiredClients

                if (Self.ConnectedPeers.Count() > 0 && !Self.ConnectedPeers.Any(x => WiredClients.Any(a => a.PeerId == x.PeerId)))
                {
                    var NonWiredClients = Self.ConnectedPeers.Where(x => !WiredClients.Any(a => x.PeerId == a.PeerId));
                    if (NonWiredClients?.Count() > 0)
                    {
                        MessageBox.Show(string.Join("\n", NonWiredClients.Select(x => $"{x.AddressPort}")));

                        foreach (var client in NonWiredClients)
                        {
                            client.Client.OnDataReceived += HandleOnClientDataReceived;
                            WiredClients.Add(client);
                        }
                    }

                }


                //cmbPeerList.Items.Clear();
                //cmbPeerList.Items.AddRange(Self.ConnectedPeers.Select(x => $"{x.PeerId} - {x.Address}:{x.Port}"));
                //cmbPeerList.DisplayMember = Self.
                //cmbPeerList.data
            }

        }


        public void HandleOnServerDataReceived(ServerClientHandle Clienthandle, PacketHeader Header, ReadOnlySpan<byte> Data)
        {
            MessageBox.Show("message received");
        }

        public void HandleOnClientDataReceived(PacketHelper Packer, PacketHeader Header, ReadOnlySpan<byte> Data)
        {
            MessageBox.Show($"{Header.ToJSON()}\n\n{Data.ToArray().ToUTF8String()}");
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            
        }

        private void btnSendMessage_Click(object sender, EventArgs e)
        {
            string Message = txtMessage.Text;

            //MessageBox.Show(string.Join("\n", Self.ConnectedPeers.Select(x => $"{x.PeerId} - {x.Address}:{x.Port}")));


            PeerTable PT = (PeerTable)cmbPeerList.SelectedItem;
            //MessageBox.Show(PT.ToJSON());
            var Peer = Self.ConnectedPeers.Find(x => x.PeerId == PT.PeerId);

            //Peer.

            //Peer.Client.

            // Only allows me to send if I start the connection first
            Peer.Client.Packer.SendUTF8Packet(Message);


            //if (Peer. is not null) { MessageBox.Show("Test");}

            //Self.TCPServer.
            //var Current = Self.FindPeerById(Guid.Parse(lblPeerId.Text.Replace("PeerId: ", "")));
            //Current.Value.Item2.PacketHelper.SendUTF8Packet(Message);
        }

        private void cmbPeerList_Click(object sender, EventArgs e)
        {
            cmbPeerList.DataSource = Self.ConnectedPeers;
            cmbPeerList.DisplayMember = "AddressPort";
            cmbPeerList.ValueMember = "PeerId";
        }
    }
}
