using ecg_app.Models;
using ecg_app.Models.Protocol;
using ecg_app.Views;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace ecg_app.ViewModels
{
    public class EcgViewModel : BaseViewModel
    {
        private IDevice cardIQ = null;
        ICharacteristic ecgCharacteristic = null;

        public Command OnRequestEcgCommand { get; }

        public Command OnConnectCommand { get; }

        List<IDevice> deviceList = new List<IDevice>();

        BleEcgProtocol protocol = null;

        public int BatteryLevel { get; set; }
        public int LoPositive { get; set; }
        public int LoNegative { get; set; }
        public int EcgFramesCount { get; set; }

        public string MeasureResultText { get; set; }

        public bool Connected => cardIQ != null && cardIQ.State == Plugin.BLE.Abstractions.DeviceState.Connected;

        public EcgViewModel()
        {
            OnRequestEcgCommand = new Command((s) =>
            {
                ReadEcg();
            });
            
            OnConnectCommand = new Command((s) =>
            {
                Init();
            });

            Init();
        }

        public void OnAppearing()
        {
        }

        public void OnDisappearing()
        {
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task Init()
        {
            try
            {
                var ble = CrossBluetoothLE.Current;
                var adapter = CrossBluetoothLE.Current.Adapter;
                cardIQ = adapter.BondedDevices.SingleOrDefault(q => q.Name == "CardIQ");
                if (cardIQ == null)
                    throw new Exception("CardIQ not bounded");


                adapter.DeviceConnectionLost += Adapter_DeviceConnectionLost;
                await adapter.ConnectToDeviceAsync(cardIQ);

                IService ecgService = await cardIQ.GetServiceAsync(Guid.Parse("4fafc201-1fb5-459e-8fcc-c5c9c3319333"));
                ecgCharacteristic = await ecgService.GetCharacteristicAsync(Guid.Parse("beb5483e-36e1-4688-b7f5-ea07361b26a8"));
    

                protocol = new BleEcgProtocol(ecgCharacteristic);

                await Task.Delay(500);
                var pingres = await protocol.Ping();

                BatteryLevel = pingres.BatteryPercent;
                LoPositive = pingres.LoPos;
                LoNegative = pingres.LoNeg;
                EcgFramesCount = pingres.Frames;

                OnPropertyChanged(nameof(BatteryLevel));
                OnPropertyChanged(nameof(LoPositive));
                OnPropertyChanged(nameof(LoNegative));
                OnPropertyChanged(nameof(EcgFramesCount));

                OnPropertyChanged(nameof(Connected));

            }
            catch (Exception exc)
            {

            }

        }

        private void Adapter_DeviceConnectionLost(object sender, Plugin.BLE.Abstractions.EventArgs.DeviceErrorEventArgs e)
        {
            cardIQ = null; 

            BatteryLevel = 0;
            LoPositive = 0;
            LoNegative = 0;
            EcgFramesCount = 0;

            OnPropertyChanged(nameof(BatteryLevel));
            OnPropertyChanged(nameof(LoPositive));
            OnPropertyChanged(nameof(LoNegative));
            OnPropertyChanged(nameof(EcgFramesCount));
            OnPropertyChanged(nameof(Connected));
        }

        /// <summary>
        /// запрос и вычитывание ЭКГ
        /// </summary>
        /// <returns></returns>
        private async Task ReadEcg()
        {
            try
            {
                MeasureResultText = $"Запрос ЭКГ";
                OnPropertyChanged("MeasureResultText");

                var resp = await protocol.RequestEcg();
                Trace.WriteLine($"RequestEcg resp res - {resp.Result}");

                if (resp.Result == ProtocolResponseResult.NAK)
                {
                    Trace.WriteLine($"ERROR");
                    MeasureResultText = $"ошибка - {resp.ErrorText}";
                    OnPropertyChanged("MeasureResultText");
                    return;
                }

                Trace.WriteLine($"{DateTime.Now} begin wait for ecg mteasure complite");
                var maxAwaitSecs = 10;
                PingResponsePack pingres = null;
                for (int i = 0; i < maxAwaitSecs; i++)
                {
                    pingres = await protocol.Ping();
                    if (pingres == null)
                        throw new ArgumentException($"На шаге {i} не получен ответ на PING");
                    if (pingres.Frames > 0)
                    {
                        break;
                    }
                    await Task.Delay(1000);
                }

                var allSamples = new List<short>();

                if (pingres.Frames > 0) // чтото намерилось
                {
                    /// дальше читать все намеренное
                    for (global::System.Int32 i = 0; i < pingres.Frames; i++)
                    {
                        Trace.WriteLine($"get ecg frame #{i}/{pingres.Frames}");
                        MeasureResultText = $"get frame #{i}/{pingres.Frames}";
                        OnPropertyChanged("MeasureResultText");
                        var readres = await protocol.ReadEcg(i);
                        allSamples.AddRange(readres.Samples);
                    }
                }
                else
                    MeasureResultText = $"не удалось провести измерение";

                MeasureResultText = $"Получено {allSamples.Count} измерений";
                OnPropertyChanged("MeasureResultText");

                GoSleepBaby(60);
            }
            catch(Exception exc)
            {

            }

        }

        /// <summary>
        /// Отправить спать платку
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        private async Task GoSleepBaby(int seconds)
        {
            var res = await protocol.Sleep((ushort)seconds);
            _=Task.Run(async () =>
            {
                await Task.Delay(seconds * 1000);
                _=Init();
            });
        }
    }

}

