using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ecg_app.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BtPage : ContentPage
    {
        private IDevice cardIQ = null;
        ICharacteristic ecgCharacteristic = null;
        private Timer timer = null;
        List<IDevice> deviceList = new List<IDevice>();
        private readonly object _syncObj = new object();

        private const byte ACK = 0x15;
        private const byte NAK = 0x17;

        private const int timerPeriodMs = 3000;

        public BtPage()
        {
            InitializeComponent();
            BindingContext = this;
            timer = new Timer(PingTimerCallback);
        }

        public string Label2 { get; set; } = "two";


        public int BatteryLevel { get; set; }

        public int LoPositive { get; set; }

        public int LoNegative { get; private set; }

        public int EcgFramesCount { get; set; }

        public bool Connected { get; set; }


        protected override void OnAppearing()
        {
            base.OnAppearing();
            connect2boundedDevice();
            timer = new Timer(PingTimerCallback);
            timer.Change(timerPeriodMs, -1);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            timer.Change(-1, -1);
        }

        private void PingTimerCallback(object state)
        {
            try
            {
                if (cardIQ != null && Connected && cardIQ.State == Plugin.BLE.Abstractions.DeviceState.Connected)
                {
                    if (ecgCharacteristic != null)
                    {
                        WriteDataToBLE(Encoding.UTF8.GetBytes("ping"));
                    }
                }
            }
            catch (Exception exc)
            {

            }
            timer.Change(timerPeriodMs, -1);
        }


        void tapped(object sender, EventArgs args)
        {
            Label2 = "three";
            OnPropertyChanged(nameof(Label2));

            Button_Clicked(sender, args);
        }

        private async Task scan4devices()
        {
            var ble = CrossBluetoothLE.Current;
            var adapter = CrossBluetoothLE.Current.Adapter;
            adapter.DeviceDiscovered += (s, a) => deviceList.Add(a.Device);

            _ = adapter.StartScanningForDevicesAsync().ContinueWith(async (t) =>
            {
                await adapter.StopScanningForDevicesAsync();
                deviceList.ForEach((d) => Trace.WriteLine($"{d.Name} - {d.Rssi}"));
                try
                {
                    var cardIQ = deviceList.Where(q => q.Name == "CardIQ").OrderByDescending(q => q.Rssi).First();
                    if (cardIQ != null)
                    {
                        await adapter.ConnectToDeviceAsync(cardIQ);
                        var services = await cardIQ.GetServicesAsync();
                        foreach (var s in services)
                        {
                            Trace.WriteLine($"{s.Name} {s.Id}. Characteristics:");
                            var characts = await s.GetCharacteristicsAsync();
                            foreach (var c in characts)
                            {
                                Trace.WriteLine($"{c.Name} {c.Uuid}");
                                if (!c.CanRead)
                                {
                                    Trace.WriteLine($"read not support");
                                    continue;
                                }
                                var readed = await c.ReadAsync();
                                if (readed.data != null && readed.data.Length > 0)
                                {
                                    Trace.WriteLine($"readed data code {readed.resultCode}. Data - {BitConverter.ToString(readed.data)}");
                                }
                                else
                                {
                                    Trace.WriteLine($"data is empty. code {readed.resultCode}.");
                                }
                            }

                        }
                    }
                }
                catch (Exception exc)
                {

                }

            });



            // Battery Service 0000180f-0000-1000-8000-00805f9b34fb. Characteristics:
            // Battery Level 00002a19-0000-1000-8000-00805f9b34fb

            // ECG
            //Unknown Service 4fafc201-1fb5-459e-8fcc-c5c9c3319333. Characteristics:
            //Unknown characteristic beb5483e-36e1-4688-b7f5-ea07361b26a8
            //readed data code 0. Data - 00-0C-01-0C-02-0C-03-0C-04-0C-05-0C-06-0C-07-0C-08-0C-09-0C-0A-0C-0B




        }


        private async Task connect2boundedDevice()
        {
            var ble = CrossBluetoothLE.Current;
            var adapter = CrossBluetoothLE.Current.Adapter;
            cardIQ = adapter.BondedDevices.SingleOrDefault(q => q.Name == "CardIQ");
            if (cardIQ == null)
                return;

            await adapter.ConnectToDeviceAsync(cardIQ);

            // Ecg
            IService ecgService = await cardIQ.GetServiceAsync(Guid.Parse("4fafc201-1fb5-459e-8fcc-c5c9c3319333"));
            ecgCharacteristic = await ecgService.GetCharacteristicAsync(Guid.Parse("beb5483e-36e1-4688-b7f5-ea07361b26a8"));
            ecgCharacteristic.ValueUpdated += EcgCharacteristic_ValueUpdated;
            await ecgCharacteristic.StartUpdatesAsync();
            Connected = true;
        }


        private void EcgCharacteristic_ValueUpdated(object sender, Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs e)
        {
            _ = e.Characteristic.ReadAsync().ContinueWith((t) =>
            {
                try
                {
                    if (t.Status != TaskStatus.Faulted && t.Exception == null && t.Result.resultCode == 0)
                    {
                        var res = t.Result.data;
                        if (res[0] == 1)
                            _ = MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                Trace.WriteLine($"from BT ecg {BitConverter.ToString(res)}");
                                BatteryLevel = (int)res[1];
                                LoPositive = (int)res[3];
                                LoNegative = (int)res[4];
                                EcgFramesCount = (int)res[2];
                                OnPropertyChanged(nameof(BatteryLevel));
                                OnPropertyChanged(nameof(LoPositive));
                                OnPropertyChanged(nameof(LoNegative));
                                OnPropertyChanged(nameof(EcgFramesCount));
                            });
                        else if (res[0] == 2)
                            Trace.WriteLine($"ERROR BLE ECG {Encoding.UTF8.GetString(res)}");
                    }
                    else
                    {

                    }
                }
                catch (Exception exc)
                {

                }

            });
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            timer.Change(-1, -1);
            Thread.Sleep(1000);
            WriteDataToBLE(new byte[] { 0x02 });
            Thread.Sleep(1000);
            timer.Change(timerPeriodMs, -1);
        }

        private void WriteDataToBLE(byte[] data)
        {
            if (ecgCharacteristic != null)
                lock (_syncObj)
                    ecgCharacteristic.WriteAsync(data);
        }
    }
}