using D.FreeExchange.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using D.Util.Interface;
using System.Collections.Concurrent;
using System.Threading;

namespace D.FreeExchange.Core
{
    /// <summary>
    /// SocketAsyncEventArgsPool 的配置
    /// </summary>
    public class SocketAsyncEventArgsPoolConfig : IConfig
    {
        public string Path
        {
            get
            {
                return "FreeExchange.SocketAsyncEventArgsPool";
            }
        }

        /// <summary>
        /// 初始化时，创建 SocketAsyncEventArgs 的个数
        /// </summary>
        public int InitCount { get; set; }

        /// <summary>
        /// 当 pool 中 arg 不足时，自动增加数量
        /// </summary>
        public int Increment { get; set; }

        /// <summary>
        /// 每个 arg 缓冲区大小
        /// </summary>
        public int ArgBufferSize { get; set; }

        public SocketAsyncEventArgsPoolConfig()
        {
            InitCount = 4;
            Increment = 4;
            ArgBufferSize = 1024 * 4;
        }
    }

    /// <summary>
    /// SocketAsyncEventArgsPool 实现
    /// </summary>
    public class SocketAsyncEventArgsPool : ISocketAsyncEventArgsPool
    {
        ILogger _logger;
        SocketAsyncEventArgsPoolConfig _config;

        ConcurrentQueue<SocketAsyncEventArgs> _argQueue;
        int _argCount;

        public SocketAsyncEventArgsPool(
            ILoggerFactory loggerFactory
            , IConfigProvider configProvider
            )
        {
            _logger = loggerFactory.CreateLogger<SocketAsyncEventArgsPool>();
            _config = configProvider.GetConfigNullWithDefault<SocketAsyncEventArgsPoolConfig>();

            _argQueue = new ConcurrentQueue<SocketAsyncEventArgs>();
            _argCount = 0;

            CreateArgAndPutInQueue(_config.InitCount);
            _logger.LogInformation($"SocketAsyncEventArgs pool 初始化 {_config.InitCount} 个 arg");
        }

        #region ISocketAsyncEventArgsPool
        public SocketAsyncEventArgs Pop()
        {
            SocketAsyncEventArgs arg = null;

            if (!_argQueue.TryDequeue(out arg))
            {
                arg = new SocketAsyncEventArgs();
                InitArg(arg);
                Interlocked.Increment(ref _argCount);

                CreateArgAndPutInQueue(_config.Increment - 1);

                _logger.LogInformation($"SocketAsyncEventArgs pool 中 arg 不足，补充 {_config.Increment} 个，现在共有 {_argCount} 个 arg");
            }

            return arg;
        }

        public void Push(SocketAsyncEventArgs arg)
        {
            CleanArg(arg);

            _argQueue.Enqueue(arg);
        }
        #endregion

        /// <summary>
        /// 初始化 count 个 arg，然后添加到 queue 中
        /// </summary>
        /// <param name="count"></param>
        private void CreateArgAndPutInQueue(int count)
        {
            var i = 0;

            while (i < count)
            {
                var arg = new SocketAsyncEventArgs();

                InitArg(arg);

                _argQueue.Enqueue(arg);
            }

            Interlocked.Add(ref _argCount, count);
        }

        /// <summary>
        /// 初始化 arg 的一些数据
        /// </summary>
        /// <param name="arg"></param>
        private void InitArg(SocketAsyncEventArgs arg)
        {
            arg.SetBuffer(new byte[_config.ArgBufferSize], 0, _config.ArgBufferSize);
        }

        /// <summary>
        /// 清理 arg ，使其可以被继续使用
        /// </summary>
        /// <param name="arg"></param>
        private void CleanArg(SocketAsyncEventArgs arg)
        {
            //貌似现在不需要做什么，暂时放着
        }
    }
}
