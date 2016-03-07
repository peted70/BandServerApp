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
using Microsoft.Band.Sensors;
using System.Threading;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BandServerApp
{
    public class ArgWrapper<T>
    {
        public string Type { get; set; }

        public T Args { get; set; }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static ArgWrapper<IBandHeartRateReading> _hrArgs = 
            new ArgWrapper<IBandHeartRateReading> { Type = "hr" };
        public static ArgWrapper<IBandGyroscopeReading> _gyroArgs =
            new ArgWrapper<IBandGyroscopeReading> { Type = "gyro" };
        public static ArgWrapper<IBandAccelerometerReading> _accArgs =
            new ArgWrapper<IBandAccelerometerReading> { Type = "acc" };

        private static IBandInfo _bandInfo;
        private static IBandClient _bandClient;

        private AsyncLock _lockObj = new AsyncLock(); 

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

            await SetupSensorReadingAsync(_bandClient.SensorManager.HeartRate, async (obj, ev) =>
            {
                _hrArgs.Args = ev.SensorReading;
                var payload = JsonConvert.SerializeObject(_hrArgs);
                using (var releaser = await _lockObj.LockAsync())
                {
                    await _socketWriter.WriteAsync(payload);
                }
            });

            await SetupSensorReadingAsync(_bandClient.SensorManager.Gyroscope, async (obj, ev) =>
            {
                _gyroArgs.Args = ev.SensorReading;
                var payload = JsonConvert.SerializeObject(_gyroArgs);
                using (var releaser = await _lockObj.LockAsync())
                {
                    await _socketWriter.WriteAsync(payload);
                }
            });

            await SetupSensorReadingAsync(_bandClient.SensorManager.Accelerometer, async (obj, ev) =>
            {
                _accArgs.Args = ev.SensorReading;
                var payload = JsonConvert.SerializeObject(_accArgs);
                using (var releaser = await _lockObj.LockAsync())
                {
                    await _socketWriter.WriteAsync(payload);
                }
            });
        }

        public static async Task SetupSensorReadingAsync<T>(IBandSensor<T> bandSensor, 
            EventHandler<BandSensorReadingEventArgs<T>> cb) 
            where T : IBandSensorReading  
        {
            var uc = bandSensor.GetCurrentUserConsent();
            bool isConsented = false;
            if (uc == UserConsent.NotSpecified)
            {
                isConsented = await bandSensor.RequestUserConsentAsync();
            }

            if (isConsented || uc == UserConsent.Granted)
            {
                bandSensor.ReadingChanged += cb;
                await bandSensor.StartReadingsAsync();
            }
        }
    }

    // http://blogs.msdn.com/b/pfxteam/archive/2012/02/12/10266983.aspx
    public class AsyncSemaphore
    {
        private readonly static Task s_completed = Task.FromResult(true);
        private readonly Queue<TaskCompletionSource<bool>> m_waiters = new Queue<TaskCompletionSource<bool>>();
        private int m_currentCount;

        public AsyncSemaphore(int initialCount)
        {
            if (initialCount < 0) throw new ArgumentOutOfRangeException("initialCount");
            m_currentCount = initialCount;
        }

        public Task WaitAsync()
        {
            lock (m_waiters)
            {
                if (m_currentCount > 0)
                {
                    --m_currentCount;
                    return s_completed;
                }
                else
                {
                    var waiter = new TaskCompletionSource<bool>();
                    m_waiters.Enqueue(waiter);
                    return waiter.Task;
                }
            }
        }

        public void Release()
        {
            TaskCompletionSource<bool> toRelease = null;
            lock (m_waiters)
            {
                if (m_waiters.Count > 0)
                    toRelease = m_waiters.Dequeue();
                else
                    ++m_currentCount;
            }
            if (toRelease != null)
                toRelease.SetResult(true);
        }
    }

    // http://blogs.msdn.com/b/pfxteam/archive/2012/02/12/10266988.aspx
    public class AsyncLock
    {
        private readonly AsyncSemaphore m_semaphore;
        private readonly Task<Releaser> m_releaser;

        public AsyncLock()
        {
            m_semaphore = new AsyncSemaphore(1);
            m_releaser = Task.FromResult(new Releaser(this));
        }

        public Task<Releaser> LockAsync()
        {
            var wait = m_semaphore.WaitAsync();
            return wait.IsCompleted ?
                m_releaser :
                wait.ContinueWith((_, state) => new Releaser((AsyncLock)state),
                    this, CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        public struct Releaser : IDisposable
        {
            private readonly AsyncLock m_toRelease;

            internal Releaser(AsyncLock toRelease) { m_toRelease = toRelease; }

            public void Dispose()
            {
                if (m_toRelease != null)
                    m_toRelease.m_semaphore.Release();
            }
        }
    }
}
