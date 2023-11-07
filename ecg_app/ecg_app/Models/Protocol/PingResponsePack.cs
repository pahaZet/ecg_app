using System;
using System.Collections.Generic;
using System.Text;

namespace ecg_app.Models.Protocol
{
    /// <summary>
    /// Ответ на запрос пинга
    /// </summary>
    public class PingResponsePack : BaseProtocolResponse
    {
        public int BatteryPercent { get; set; }
        public int Frames { get; set; }
        public int LoPos { get; set; }
        public int LoNeg { get; set; }

        public static PingResponsePack Parse(byte[] data)
        {
            if (data == null || data.Length < 6)
                return null;

            var res = new PingResponsePack()
            {
                Result = (ProtocolResponseResult)data[0],
                BatteryPercent = data[2],
                Frames = data[3],
                LoPos = data[4],
                LoNeg = data[5]
            };
            return res;
        }
    }
}
