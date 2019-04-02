using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange
{
    public enum FlagCode
    {
        Start,
        Middle,
        End,
        Single
    }

    public enum PackageCode
    {
        Connect = 0,
        Disconnect,
        Heart,

        Clean = 4,
        CleanUp,
        Answer,
        Lost,

        Text = 12,
        ByteDescription,
        Byte
    }

    public interface IPackage
    {
        FlagCode Flag { get; set; }

        PackageCode Code { get; set; }

        int PushBuffer(byte[] buffer, ref int index, int length);

        byte[] ToBuffer();
    }

    public class PackageHeader : IPackage
    {
        public byte HeadBuffer { get; set; }

        public FlagCode Flag { get; set; }

        public PackageCode Code { get; set; }

        public PackageHeader(byte buffer)
        {
            HeadBuffer = buffer;

            Flag = (FlagCode)(buffer >> 6);
            Code = (PackageCode)(buffer & 15);
        }

        public PackageHeader(IPackage package)
        {
            Flag = package.Flag;
            Code = package.Code;

            HeadBuffer = (byte)((byte)Flag << 6);
            HeadBuffer = (byte)(HeadBuffer + (byte)Code);
        }

        public virtual int PushBuffer(byte[] buffer, ref int index, int length)
        {
            throw new NotImplementedException();
        }

        public virtual byte[] ToBuffer()
        {
            throw new NotImplementedException();
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
