using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange.Protocol.DP
{
    class PackageWithPayload : PackageHeader
        , IPackageWithIndex
    {
        /// <summary>
        /// 包编号
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 负载数据长度；赋值时会重置 Payload 和 
        /// </summary>
        public int PayloadLength { get; private set; }

        /// <summary>
        /// 负载数据
        /// </summary>
        public byte[] Payload { get; set; }

        public PackageWithPayload(
            PackageCode code
            , int payloadLength
            , FlagCode flag = FlagCode.Single
            ) : base(code, flag)
        {
            PayloadLength = payloadLength;

            Payload = new byte[PayloadLength];
        }

        public PackageWithPayload(IPackage header)
            : base(header.Code, header.Flag)
        {
        }

        public override int PushBuffer(byte[] buffer, ref int index, int length)
        {
            var endIndex = index + length < buffer.Length ? index + length : buffer.Length;

            for (; index < endIndex || AnalysedBufferLength < 5; index++)
            {
                Index = (Index << 8) + buffer[index];
                AnalysedBufferLength++;

                if (AnalysedBufferLength == 5)
                {
                    PayloadLength = Index >> 16;
                    Index = (Int16)Index;
                }
            }

            if (index == endIndex) return BufferLength - AnalysedBufferLength;

            var copyLength = endIndex - index < BufferLength - AnalysedBufferLength
                ? endIndex - index
                : BufferLength - AnalysedBufferLength;

            Array.Copy(buffer, index, Payload, AnalysedBufferLength - 5, copyLength);

            index += copyLength;
            AnalysedBufferLength += copyLength;

            return BufferLength - AnalysedBufferLength;
        }

        public override byte[] ToBuffer()
        {
            var buffer = new byte[BufferLength];

            buffer[0] = HeadBuffer;

            buffer[1] = (byte)(Index >> 8);
            buffer[2] = (byte)(Index);

            buffer[3] = (byte)(PayloadLength >> 8);
            buffer[4] = (byte)(PayloadLength);

            Array.Copy(Payload, 0, buffer, 5, PayloadLength);

            return buffer;
        }
    }
}
