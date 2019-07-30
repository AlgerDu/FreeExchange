using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange.Protocol.DP
{
    /// <summary>
    /// 首次连接的连接包
    /// </summary>
    internal class ConnectPackage : PackageWithPayload
    {
        private Encoding _encoding;

        public DProtocolOptions DpOptions
        {
            get
            {
                var json = _encoding.GetString(Payload);

                return JsonConvert.DeserializeObject<DProtocolOptions>(json);
            }
            set
            {
                var json = JsonConvert.SerializeObject(value);

                Payload = _encoding.GetBytes(json);

                PayloadLength = Payload.Length;
            }
        }

        public ConnectPackage(
            IPackage header
            , Encoding encoding)
            : base(header)
        {
            _encoding = encoding;
        }

        public ConnectPackage(
            Encoding encoding
            )
            : base(PackageCode.Connect, 0)
        {
            _encoding = encoding;
        }
    }
}
