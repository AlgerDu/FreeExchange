using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange.Protocol.DP
{
    /// <summary>
    /// 连接包携带的数据
    /// </summary>
    public class ConnectPackageData
    {
        /// <summary>
        /// 自定义唯一标识
        /// 可以用来区分重启和重新连接等等
        /// </summary>
        public string Uid { get; set; }

        /// <summary>
        /// 可选配置参数
        /// </summary>
        public DProtocolOptions Options { get; set; }
    }

    /// <summary>
    /// 连接包
    /// </summary>
    public class ConnectPackage : PackageWithPayload
    {
        public ConnectPackage(
            IPackage header)
            : base(header)
        {
        }

        public ConnectPackage(
            )
            : base(PackageCode.Connect, 0)
        {
        }

        public void SetData(ConnectPackageData data, Encoding encoding)
        {
            var json = JsonConvert.SerializeObject(data);

            Payload = encoding.GetBytes(json);

            PayloadLength = Payload.Length;
        }

        public ConnectPackageData GetData(Encoding encoding)
        {
            var json = encoding.GetString(Payload);

            return JsonConvert.DeserializeObject<ConnectPackageData>(json);
        }
    }
}
