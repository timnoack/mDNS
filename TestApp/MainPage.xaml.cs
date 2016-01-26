using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


namespace TestApp
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }
        mDNS.mDNS dns;

        private void btnList_Click(object sender, RoutedEventArgs e)
        {
            btnList.IsEnabled = false;   
            dns.List(textBox.Text).ContinueWith(new Action<System.Threading.Tasks.Task<mDNS.ServiceInfo[]>>((task) => {
                mDNS.ServiceInfo[] services = task.Result;
                    var disp = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, new Windows.UI.Core.DispatchedHandler(delegate {
                        listBox.Items.Clear();
                        foreach (var service in services)
                            listBox.Items.Add(new ServiceListBoxItem() { Service = service });
                        btnList.IsEnabled = true;
                    }));


                
            }));


        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            mDNS.Logging.LogManager.MessageReceived += LogManager_MessageReceived;

            dns = new mDNS.mDNS();

            var init = dns.Init().ContinueWith(new Action<System.Threading.Tasks.Task>(delegate {
                var disp = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, new Windows.UI.Core.DispatchedHandler(delegate {
                    txtInit.Visibility = Visibility.Collapsed;
                    contentGrid.Visibility = Visibility.Visible;
                    btnList_Click(this, null);

                }));
            }));


        }

        private void LogManager_MessageReceived(object sender, mDNS.Logging.LogMessageEventArgs e)
        {
            if(e.Type != mDNS.Logging.LogType.Debug)
            System.Diagnostics.Debug.WriteLine("mDNS " + e.ToString());
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (dns != null)
                dns.Close();
        }

        private void btnSimulate_Click(object sender, RoutedEventArgs e)
        {
            // thank you http://nto.github.io/AirPlay.html
            btnSimulate.IsEnabled = false;
            lblSimulateStatus.Text = "Initializing simulation..";

            var token = System.Threading.Tasks.Task.Run(new Action(delegate
            {
                string name = "Simulated mDNS Apple TV";

                // AirTunes service
                string fullname = "5855CA1AE288@" + name;
                System.Collections.Hashtable props = new System.Collections.Hashtable();
                props.Add("txtvers", "1");
                props.Add("ch", "2");
                props.Add("cn", "0,1,2,3");
                props.Add("da", "true");
                props.Add("et", "0,3,5");
                props.Add("md", "0,1,2");
                props.Add("pw", "false");
                props.Add("sv", "false");
                props.Add("sr", "44100");
                props.Add("ss", "16");
                props.Add("tp", "UDP");
                props.Add("vn", "65537");
                props.Add("vs", "130.14");
                props.Add("am", "AppleTV2,1");
                props.Add("sf", "0x4");

                dns.RegisterService(new mDNS.ServiceInfo("_raop._tcp.local.", fullname, 49152, 0, 0, props));

                // AirPlay service
                props = new System.Collections.Hashtable();
                props.Add("deviceid", "58:55:CA:1A:E2:88");
                props.Add("features", "0x39f7");
                props.Add("model", "AppleTV2,1");
                props.Add("srcvers", "130.14");

                dns.RegisterService(new mDNS.ServiceInfo("_airplay._tcp.local.", name, 7000, 0, 0, props));

                // just for fun - HomeKit service
                string homeKitName = "mDNS HomeKit Device";
                props = new System.Collections.Hashtable();
                props.Add("id", "58:55:CA:1A:E2:88");
                props.Add("pv", "1.0");
                props.Add("c#", "1");
                props.Add("s#", "1");
                props.Add("sf", "1");
                props.Add("ff", "0");
                props.Add("md", homeKitName);
                props.Add("ci", "1");

                dns.RegisterService(new mDNS.ServiceInfo("_hap._tcp.local.", homeKitName, 51350, 0, 0, props));

                var disp = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, new Windows.UI.Core.DispatchedHandler(delegate
                {
                    lblSimulateStatus.Text = "Simulation running!";
                }));
            }));
            

        }

        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
      
            if (listBox.SelectedItem == null || !(listBox.SelectedItem is ServiceListBoxItem))
                detailBox.Items.Clear();
            else
            {
                ServiceListBoxItem item = (ServiceListBoxItem)listBox.SelectedItem;
                mDNS.ServiceInfo info = item.Service;
                info.PropertyNames.Reset();
                detailBox.Items.Clear();
                detailBox.Items.Add(info.HostAddress);
                System.Collections.IEnumerator enumerator = info.PropertyNames;
                while(enumerator.MoveNext())
                {
                    detailBox.Items.Add(enumerator.Current + ": " + info.GetPropertyString(enumerator.Current.ToString()));
                }
            }
        }

        public class ServiceListBoxItem
        {
            public mDNS.ServiceInfo Service { get; set; }

            public override string ToString()
            {
                return Service.QualifiedName + " " + Service.HostAddress;
            }
        }
    }
}
