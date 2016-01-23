using FakeBand.Fakes;
using Microsoft.Band;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BandServerApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static IBandInfo _bandInfo;
        private static IBandClient _bandClient;

        MessageWebSocket _socket = new MessageWebSocket();
        StreamWriter _socketWriter;

        public MainPage()
        {
            this.InitializeComponent();
            Loaded += OnLoad;
        }

        private async void OnLoad(object sender, RoutedEventArgs e)
        {
            _socket.Control.MessageType = SocketMessageType.Utf8;
            await _socket.ConnectAsync(new Uri("ws://localhost:54545"));

            var writeStream = _socket.OutputStream.AsStreamForWrite();
            _socketWriter = new StreamWriter(writeStream);
            _socketWriter.AutoFlush = true;

            await SetupBandAsync();
        }

        private async Task SetupBandAsync()
        {
            FakeBandClientManager.Configure(new FakeBandClientManagerOptions
            {
                Bands = new List<IBandInfo>
                {
                    new FakeBandInfo(BandConnectionType.Bluetooth, "Fake Band 1"),
                }
            });

            // Use the fake band client manager
            IBandClientManager clientManager = FakeBandClientManager.Instance;

            // --------------------------------------------------
            // set up Band SDK code to start reading HR values..
            var bands = await clientManager.GetBandsAsync();
            _bandInfo = bands.First();

            _bandClient = await clientManager.ConnectAsync(_bandInfo);

            var uc = _bandClient.SensorManager.HeartRate.GetCurrentUserConsent();
            bool isConsented = false;
            if (uc == UserConsent.NotSpecified)
            {
                isConsented = await _bandClient.SensorManager.HeartRate.RequestUserConsentAsync();
            }

            if (isConsented || uc == UserConsent.Granted)
            {
                _bandClient.SensorManager.HeartRate.ReadingChanged += async (obj, ev) =>
                {
                    var payload = JsonConvert.SerializeObject(ev);
                    await _socketWriter.WriteAsync(payload);
                };
                await _bandClient.SensorManager.HeartRate.StartReadingsAsync();
            }
        }
    }
}
