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

    public interface IPackageWithIndex : IPackage
    {
        int Index { get; }
    }

    /// <summary>
    /// package header 的定义
    /// </summary>
    public class PackageHeader : IPackage
    {
        FlagCode _flag;
        PackageCode _code;

        protected int AnalysedBufferLength { get; set; }

        public byte HeadBuffer { get; set; }

        public FlagCode Flag
        {
            get => _flag;
            set
            {
                _flag = value;

                HeadBuffer = (byte)((byte)Flag << 6);
                HeadBuffer = (byte)(HeadBuffer + (byte)Code);
            }
        }

        public PackageCode Code
        {
            get => _code;
            private set
            {
                _code = value;
            }
        }

        public virtual int BufferLength
        {
            get
            {
                return 1;
            }
        }

        public PackageHeader(byte buffer)
        {
            HeadBuffer = buffer;

            _code = (PackageCode)(buffer & 15);
            _flag = (FlagCode)(buffer >> 6);

            AnalysedBufferLength = 1;
        }

        public PackageHeader(
            PackageCode code
            , FlagCode flag = FlagCode.Single
            )
        {
            Code = code;
            Flag = flag;

            AnalysedBufferLength = 1;
        }

        public PackageHeader(IPackage header)
            : this(header.Code, header.Flag)
        {
        }

        public virtual int PushBuffer(byte[] buffer, ref int index, int length)
        {
            return 0;
        }

        public virtual byte[] ToBuffer()
        {
            return new byte[]
            {
                HeadBuffer
            };
        }

        public override string ToString()
        {
            return $"package[{Code}]";
        }
    }
}
