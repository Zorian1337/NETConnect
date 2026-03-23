using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using NETConnect;

namespace WPFConnect
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

       public ObservableCollection<string> Peers { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            Peers = new ObservableCollection<string>();
            PeerListView.ItemsSource = Peers;
            //PeerListView.SelectionChanged += PeerListView_SelectionChanged;

            Peers.Add("Alice");
            Peers.Add("Bob");

            PeerCountText.Text = $"{Peers.Count} peers online";
        }


    }
}