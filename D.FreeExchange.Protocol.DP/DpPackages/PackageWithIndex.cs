using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange.Protocol.DP
{
    /// <summary>
    /// 带有 index 数据的包
    /// </summary>
    public class PackageWithIndex : PackageHeader
        , IPackageWithIndex
    {
        /// <summary>
        /// 包编号
        /// </summary>
        public int Index { get; set; }

        public override int BufferLength => 3;

        public PackageWithIndex(
            PackageCode code
            , FlagCode flag = FlagCode.Single
            ) : base(code, flag)
        {
        }

        public PackageWithIndex(IPackage header)
            : this(header.Code, header.Flag)
        {
        }

        public override int PushBuffer(byte[] buffer, ref int index, int length)
        {
            var endIndex = index + length < buffer.Length ? index + length : buffer.Length;

            for (; index < endIndex || AnalysedBufferLength < BufferLength; index++)
            {
                Index = (Index << 8) + buffer[index];
                AnalysedBufferLength++;
            }

            return BufferLength - AnalysedBufferLength;
        }

        public override byte[] ToBuffer()
        {
            var buffer = new byte[BufferLength];

            buffer[0] = HeadBuffer;

            buffer[1] = (byte)(Index >> 8);
            buffer[2] = (byte)(Index);

            return buffer;
        }
    }
}
