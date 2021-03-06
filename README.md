# Band Server App
Simple proof of concept to get Microsoft Band data into the browser

To begin, run 'npm update' in the same directory as the package.json file 
then, 
navigate to the nodeserver folder in a console and enter the command 'node server.js'
(This will start a websocket server at ws://localhost:54545 - uses https://github.com/websockets/ws)

Open TestPage.html in a browser
(This will start a websocket client which connects to the server and when it receives a messsage displays the heart rate value)

Start the UWP app - load the project into Visual Studio 2015 and press F5
This will connect to the band (currently configured to use Fake Band (https://github.com/BandOnTheRun/fake-band)) and subscribe to it's heart rate sensor and send the data to the websocket server which will transmit it to all other connected clients).
<b>Note. the app does not use background processing so minimising it will stop the transmission of data.</b>

![alt tag](https://raw.github.com/peted70/BandServerApp/master/assets/bandtest.PNG)

The app can be configured to use either a real Microsoft Band device which has been successfully paired to your Windows 10 device or can use the FakeBand project https://github.com/BandOnTheRun/fake-band using the compiler define # define USE_DEVICE. This is implemented like this:

```cs
        IBandClientManager GetBandClientManager()
        {
#if USE_DEVICE
            return BandClientManager.Instance;
#else
            FakeBandClientManager.Configure(new FakeBandClientManagerOptions
            {
                Bands = new List<IBandInfo>
                {
                    new FakeBandInfo(BandConnectionType.Bluetooth, "Fake Band 1"),
                }
            });

            // Use the fake band client manager
            return FakeBandClientManager.Instance;
#endif
        }
```
