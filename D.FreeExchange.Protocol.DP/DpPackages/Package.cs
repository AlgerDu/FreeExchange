using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange.Protocol.DP
{
    /// <summary>
    /// package 接口定义
    /// </summary>
    public interface IPackage
    {
        /// <summary>
        /// 标记
        /// </summary>
        FlagCode Flag { get; set; }

        /// <summary>
        /// 类型编码
        /// </summary>
        PackageCode Code { get; }

        /// <summary>
        /// package 转换为 buffer 之后的长度
        /// </summary>
        int BufferLength { get; }

        /// <summary>
        /// 将 buffer 数据转换为 package 数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        int PushBuffer(byte[] buffer, ref int index, int length);

        /// <summary>
        /// 将完整的包转换为 buffer
        /// </summary>
        /// <returns></returns>
        byte[] ToBuffer();
    }

    /// <summary>
    /// package header 的定义
    /// </summary>
    public class PackageHeader : IPackage
    {
        protected int AnalysedBufferLength { get; set; }

        public byte HeadBuffer { get; set; }

        public FlagCode Flag { get; set; }

        public PackageCode Code { get; private set; }

        public int BufferLength { get; protected set; }

        public PackageHeader(byte buffer)
        {
            HeadBuffer = buffer;

            Flag = (FlagCode)(buffer >> 6);
            Code = (PackageCode)(buffer & 15);

            AnalysedBufferLength = 1;
        }

        public PackageHeader(
            PackageCode code
            , FlagCode flag = FlagCode.Single
            )
        {
            Flag = flag;
            Code = code;

            HeadBuffer = (byte)((byte)Flag << 6);
            HeadBuffer = (byte)(HeadBuffer + (byte)Code);

            AnalysedBufferLength = 1;
        }

        public PackageHeader(IPackage header)
            : this(
                  header.Code
                  , header.Flag)
        {
        }

        public virtual int PushBuffer(byte[] buffer, ref int index, int length)
        {
            throw new NotImplementedException();
        }

        public virtual byte[] ToBuffer()
        {
            return new byte[]
            {
                HeadBuffer
            };
        }
    }

    public class Package
    {
        /// <summary>
        /// 已经解析的数据长度
        /// </summary>
        int _analysedBufferLength;

        /// <summary>
        /// 包的总长度
        /// </summary>
        int _bufferLength { get; set; }

        PackageCode _pakCode;
        int _payloadLength;

        #region 协议包的内容
        /// <summary>
        /// 是否结束包
        /// </summary>
        public FlagCode Flag { get; set; }

        public PackageCode Code
        {
            get => _pakCode;
            set
            {
                _pakCode = value;

                switch (_pakCode)
                {
                    case PackageCode.Connect:
                    case PackageCode.Disconnect:
                    case PackageCode.Heart:
                        _bufferLength = 1;
                        break;

                    case PackageCode.Clean:
                    case PackageCode.CleanUp:
                    case PackageCode.Lost:
                    case PackageCode.Answer:
                        _bufferLength = 3;
                        break;

                    case PackageCode.Text:
                    case PackageCode.ByteDescription:
                    case PackageCode.Byte:
                        _bufferLength = 5 + _payloadLength;
                        break;
                }
            }
        }

        public int Index { get; set; }

        public int PayloadLength
        {
            get => _payloadLength;
            set
            {
                _payloadLength = value;

                _bufferLength = 5 + _payloadLength;
            }
        }

        public byte[] Data { get; set; }
        #endregion

        public Package()
        {
            _analysedBufferLength = 0;
        }

        public Package(int payloadLength)
        {
            Flag = FlagCode.Middle;

            PayloadLength = payloadLength;
            Data = new byte[PayloadLength];
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
            var endIndex = index + length < buffer.Length ? index + length : buffer.Length;

            if (_analysedBufferLength == 0)
            {
                Flag = (FlagCode)(buffer[index] >> 6);
                Code = (PackageCode)(buffer[index] & 15);

                index++;
                _analysedBufferLength++;
            }

            if (index >= endIndex)
            {
                return _bufferLength - _analysedBufferLength;
            }

            if (_analysedBufferLength == 1)
            {
                Index = buffer[index] << 8;

                index++;
                _analysedBufferLength++;
            }

            if (index >= endIndex)
            {
                return _bufferLength - _analysedBufferLength;
            }

            if (_analysedBufferLength == 2)
            {
                Index += buffer[index];

                index++;
                _analysedBufferLength++;
            }

            if (index >= endIndex)
            {
                return _bufferLength - _analysedBufferLength;
            }

            if (Code >= PackageCode.Text
                && _analysedBufferLength == 3)
            {
                PayloadLength = buffer[index] << 8;

                index++;
                _analysedBufferLength++;
            }

            if (index >= endIndex)
            {
                return _bufferLength - _analysedBufferLength;
            }

            if (Code >= PackageCode.Text
                && _analysedBufferLength == 4)
            {
                PayloadLength += buffer[index];
                Data = new byte[PayloadLength];

                index++;
                _analysedBufferLength++;
            }

            if (index >= endIndex)
            {
                return _bufferLength - _analysedBufferLength;
            }

            var need = _bufferLength - _analysedBufferLength;

            if (need > 0)
            {
                var enableLength =
                    endIndex - index < need
                    ? endIndex - index
                    : need;
                Array.Copy(buffer, index, Data, _analysedBufferLength - 5, enableLength);

                index += enableLength;
                _analysedBufferLength += enableLength;
            }

            return _bufferLength - _analysedBufferLength;
        }

        public byte[] ToBuffer()
        {
            var buffer = new byte[_bufferLength];

            buffer[0] = (byte)((byte)Flag << 6);
            buffer[0] = (byte)(buffer[0] + (byte)Code);

            if (Code >= PackageCode.Clean)
            {
                buffer[1] = (byte)(Index >> 8);
                buffer[2] = (byte)(Index);
            }

            if (Code >= PackageCode.Text)
            {
                buffer[3] = (byte)(PayloadLength >> 8);
                buffer[4] = (byte)(PayloadLength);

                Array.Copy(Data, 0, buffer, 5, PayloadLength);
            }

            return buffer;
        }

        public override string ToString()
        {
            return $"Package[{Flag},{Code},{Index},{PayloadLength}]";
        }
    }
}
