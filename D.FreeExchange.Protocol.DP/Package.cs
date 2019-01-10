﻿using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange
{
    internal enum PackageCode
    {
        Heart = 0,
        Disconnect = 1,
        DataReceiveAnswer = 2,
        LostData = 3,
        BadData = 4,

        Text = 8,
        ByteDescription = 9,
        Byte = 10
    }

    internal class Package
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
            var offest = index;

            if (offest - index < length
                && _analysedBufferLength == 0)
            {
                Fin = (buffer[offest] & 128) > 0;
                Code = (PackageCode)(buffer[offest] & 15);

                offest++;
            }

            if (offest - index < length
                && _analysedBufferLength == 1)
            {
                Index = buffer[offest] >> 8;

                offest++;
            }

            if (offest - index < length
                && _analysedBufferLength == 2)
            {
                Index += buffer[offest];

                offest++;
            }

            if (offest - index < length
                && (int)Code >= 8
                && _analysedBufferLength == 3)
            {
                PayloadLength = buffer[offest] >> 8;

                offest++;
            }

            if (offest - index < length
                && (int)Code >= 8
                && _analysedBufferLength == 4)
            {
                PayloadLength += buffer[offest];
                Data = new byte[PayloadLength];

                offest++;
            }

            if (offest - index < length
                && (int)Code >= 8
                && _analysedBufferLength - 5 < PayloadLength)
            {
                var enableLength =
                    offest - index < PayloadLength - _analysedBufferLength + 5
                    ? offest - index
                    : PayloadLength - _analysedBufferLength + 5;
                Array.Copy(buffer, offest, Data, _analysedBufferLength - 5, enableLength);

                offest += enableLength;
            }

            index = offest;

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
                return
PayloadLength - _analysedBufferLength + 5;
            }
        }

        public byte[] ToBuffer()
        {
            if ()
        }
    }
}