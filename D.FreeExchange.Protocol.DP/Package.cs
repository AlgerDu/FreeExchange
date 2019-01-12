using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange
{
    public enum PackageCode
    {
        Heart = 0,
        Disconnect = 1,
        Answer = 2,
        Lost = 3,
        Bad = 4,

        Text = 8,
        ByteDescription = 9,
        Byte = 10
    }

    public class Package
    {
        /// <summary>
        /// 已经解析的数据长度
        /// </summary>
        private int _analysedBufferLength;

        #region 协议包的内容
        /// <summary>
        /// 是否结束包
        /// </summary>
        public bool Fin { get; set; }

        public PackageCode Code { get; set; }

        public int Index { get; set; }

        public int PayloadLength { get; set; }

        public byte[] Data { get; set; }
        #endregion

        public Package()
        {
            _analysedBufferLength = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="length"></param>
        /// <returns>还需要多少长度组成一个完整的包</returns>
        public int PushBuffer(byte[] buffer, ref int index, int length)
        {
            var offset = index;

            if (offset - index < length
                && _analysedBufferLength == 0)
            {
                Fin = (buffer[offset] & 128) > 0;
                Code = (PackageCode)(buffer[offset] & 15);

                offset++;
                _analysedBufferLength++;
            }

            if (offset - index < length
                && _analysedBufferLength == 1)
            {
                Index = buffer[offset] << 8;

                offset++;
                _analysedBufferLength++;
            }

            if (offset - index < length
                && _analysedBufferLength == 2)
            {
                Index += buffer[offset];

                offset++;
                _analysedBufferLength++;
            }

            if (offset - index < length
                && (int)Code >= 8
                && _analysedBufferLength == 3)
            {
                PayloadLength = buffer[offset] << 8;

                offset++;
                _analysedBufferLength++;
            }

            if (offset - index < length
                && (int)Code >= 8
                && _analysedBufferLength == 4)
            {
                PayloadLength += buffer[offset];
                Data = new byte[PayloadLength];

                offset++;
            }

            if (offset - index < length
                && (int)Code >= 8
                && _analysedBufferLength - 5 < PayloadLength)
            {
                var enableLength =
                    offset - index < PayloadLength - _analysedBufferLength + 5
                    ? offset - index
                    : PayloadLength - _analysedBufferLength + 5;
                Array.Copy(buffer, offset, Data, _analysedBufferLength - 5, enableLength);

                offset += enableLength;
                _analysedBufferLength += enableLength;
            }

            index = offset;

            if ((int)Code < 8)
            {
                return 3 - _analysedBufferLength;
            }
            else if (_analysedBufferLength < 5)
            {
                return 5 - _analysedBufferLength;
            }
            else
            {
                return PayloadLength - _analysedBufferLength + 5;
            }
        }

        public byte[] ToBuffer()
        {
            byte[] buffer = null;
            if ((int)Code < 8)
            {
                buffer = new byte[3];
            }
            else
            {
                buffer = new byte[5 * PayloadLength];
            }

            buffer[0] = (byte)((Fin ? 1 : 0) << 7);
            buffer[0] = (byte)(buffer[0] + (byte)Code);

            buffer[1] = (byte)(Index >> 8);
            buffer[2] = (byte)(Index);

            if ((int)Code >= 8)
            {
                buffer[3] = (byte)(PayloadLength >> 8);
                buffer[4] = (byte)(PayloadLength);

                Array.Copy(buffer, 5, Data, 0, PayloadLength);
            }

            return buffer;
        }
    }
}
