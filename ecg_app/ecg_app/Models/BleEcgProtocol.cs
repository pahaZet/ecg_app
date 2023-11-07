using ecg_app.Models.Protocol;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace ecg_app.Models
{
    /// <summary>
    /// реализация протокола общения ECG с МП
    /// </summary>
    internal class BleEcgProtocol
    {
        private const byte ACK = 0x15;
        private const byte NAK = 0x17;

        private readonly object _syncObj = new object();
        ICharacteristic ecgCharacteristic = null;

        public BleEcgProtocol(ICharacteristic ecgCharacteristic)
        {
            this.ecgCharacteristic = ecgCharacteristic;
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


        /// <summary>
        /// Запрос снятия ЭКГ
        /// </summary>
        /// <returns></returns>
        public async Task<BaseProtocolResponse> RequestEcg()
        {
            return BaseProtocolResponse.Parse(await SendBleWithReadAnswear(Encoding.UTF8.GetBytes("makeecg")));
        }

        public async Task<ReadFrameResponsePack> ReadEcg(int frameNumber)
        {
            return ReadFrameResponsePack.Parse(await SendBleWithReadAnswear(new byte[] { 0x55, (byte)frameNumber }));
        }

        /// <summary>
        /// команда пинга и проверки состояния
        /// </summary>
        /// <returns></returns>
        public async Task<PingResponsePack> Ping()
        {
            try
            {
                Trace.WriteLine("команда пинга и проверки состояния");
                var res = PingResponsePack.Parse(await SendBleWithReadAnswear(Encoding.UTF8.GetBytes("ping")));
                Trace.WriteLine("команда пинга и проверки состояния.. ok");
                return res;
            }
            catch (Exception exc)
            {

            }
            return null;
            
        }

        private async Task<byte[]> SendBleWithReadAnswear(byte[] data)
        {
            byte[] res = null;
            try
            {
                await ecgCharacteristic.WriteAsync(data);
                var readedBeforeSend = await ecgCharacteristic.ReadAsync();
                if (readedBeforeSend.resultCode == 0)
                    res = readedBeforeSend.data;
            }
            catch (Exception exc)
            {

            }
            return res;
        }
    }
}


//await MainThread.InvokeOnMainThreadAsync(async () =>
//{

//});