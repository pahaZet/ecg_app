using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ecg_app.Models.Protocol
{
    /// <summary>
    /// Ответ на запрос фрейма
    /// </summary>
    public class ReadFrameResponsePack : BaseProtocolResponse
    {
        public short[] Samples { get; set; }

        public static ReadFrameResponsePack Parse(byte[] data)
        {
            if (data.Length < 2)
                return null;

            var res = new ReadFrameResponsePack()
            {
                Result = (ProtocolResponseResult)data[0],
                Samples = data.Skip(1).Select(b => (short)b).ToArray()
            };
            return res;
        }
    }
}
