using System;
using System.Collections.Generic;
using System.Text;

namespace ecg_app.Models.Protocol
{
    public class BaseProtocolResponse
    {
        public ProtocolResponseResult Result { get; protected set; }

        public string ErrorText { get; protected set; }

        public static BaseProtocolResponse Parse(byte[] data)
        {
            if (data.Length < 1)
                return null;
            BaseProtocolResponse res = new BaseProtocolResponse() { Result = (ProtocolResponseResult)data[0] };

            if (data.Length > 5)
            {
                res.ErrorText = Encoding.UTF8.GetString(data, 4, data.Length - 5);
            }

            return res;
        }
    }


    public enum ProtocolResponseResult
    {
        ACK = 0x15,
        NAK = 0x17
    }
}
