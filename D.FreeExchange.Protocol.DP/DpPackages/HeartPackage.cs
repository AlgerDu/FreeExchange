using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange.Protocol.DP
{
    /// <summary>
    /// 心跳包
    /// </summary>
    public class HeartPackage : PackageHeader
    {
        /// <summary>
        /// 13 位时间戳
        /// </summary>
        public int Timestamp { get; set; }

        public HeartPackage()
            : base(PackageCode.Heart)
        {
            BufferLength = 5;
        }

        public override int PushBuffer(byte[] buffer, ref int index, int length)
        {
            var endIndex = index + length < buffer.Length ? index + length : buffer.Length;

            for (; index < endIndex || AnalysedBufferLength < BufferLength; index++)
            {
                Timestamp = (Timestamp << 8) + buffer[index];
                AnalysedBufferLength++;
            }

            return BufferLength - AnalysedBufferLength;
        }

        public override byte[] ToBuffer()
        {
            var buffer = new byte[BufferLength];

            buffer[0] = HeadBuffer;

            buffer[1] = (byte)(Timestamp >> 24);
            buffer[2] = (byte)(Timestamp >> 16);
            buffer[3] = (byte)(Timestamp >> 8);
            buffer[4] = (byte)(Timestamp);

            return buffer;
        }
    }
}
