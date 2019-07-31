﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange.Protocol.DP
{
    /// <summary>
    /// 连接包携带的数据
    /// </summary>
    internal class ConnectPackageData
    {
        /// <summary>
        /// 自定义唯一标识
        /// 可以用来区分重启和重新连接等等
        /// </summary>
        public Guid Uid { get; set; }

        /// <summary>
        /// 可选配置参数
        /// </summary>
        public DProtocolOptions Options { get; set; }
    }

    /// <summary>
    /// 连接包
    /// </summary>
    internal class ConnectPackage : PackageWithPayload
    {
        private Encoding _encoding;

        /// <summary>
        /// 首次连接，客户端将配置传送给服务端
        /// </summary>
        public ConnectPackageData Data
        {
            get
            {
                var json = _encoding.GetString(Payload);

                return JsonConvert.DeserializeObject<ConnectPackageData>(json);
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
