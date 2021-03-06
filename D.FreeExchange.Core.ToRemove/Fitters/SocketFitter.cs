﻿using D.FreeExchange.Core.Interface;
using D.Util.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using D.FreeExchange.Core.Models;

namespace D.FreeExchange.Core.Fitters
{
    /// <summary>
    /// socket fitter
    /// 最基层的 fitter，用于对 socket 的一些封装
    /// </summary>
    public class SocketFitter : IFitter, IBasket
    {
        public static string Tag = "Socket";

        ILogger _logger;
        ISocketAsyncEventArgsPool _pool;

        bool _isWorking;
        IFitter _nextDismantlingFitter;
        IFitter _nextInstallationFitter;

        Socket _socket;

        SocketAsyncEventArgs _receiveEventArg;
        SocketAsyncEventArgs _sendEventArg;

        #region IFitter 属性
        public bool IsWorking
        {
            get
            {
                return _isWorking;
            }
        }

        string IFitter.Tag
        {
            get
            {
                return SocketFitter.Tag;
            }
        }
        #endregion

        public SocketFitter(
            ILoggerFactory loggerFactory
            , ISocketAsyncEventArgsPool pool)
        {
            _logger = loggerFactory.CreateLogger<SocketFitter>();
            _pool = pool;

            _isWorking = false;
        }

        #region IFitter 行为
        public event EventHandler<FitterReportEventArgs> Report;

        public void Connect(IFitter i, IFitter d)
        {
            _nextInstallationFitter = i;
            _nextDismantlingFitter = d;
        }

        public void Dismantling(object product)
        {
            if (!_isWorking)
            {
                _logger.LogWarning($"{Tag} 尚未开始工作，不发送数据");
                return;
            }

            if (product.GetType() != typeof(byte[]))
            {
                _logger.LogWarning($"{Tag} 只能发送 byte[]，不能发送 {product.GetType().Name} 数据");
                return;
            }

            if (_socket == null)
            {
                _logger.LogWarning($"发送数据用 socket 不存在");
                return;
            }

            if (_socket.Connected)
            {
                try
                {
                    _socket.Send(product as byte[]);
                }
                catch (SocketException ex)
                {
                    _logger.LogError($"{Tag} send 数据失败：{ex.ToString()}");
                }
            }
            else
            {
                _logger.LogWarning($"{Tag} 使用的 socket 已经关闭，无法发送数据");
                return;
            }
        }

        public void ExecuteCommand(FitterCommand command)
        {
            switch (command)
            {
                case FitterCommand.Run: ExecuteRunCommand(); break;
                case FitterCommand.Stop: ExecuteStopCommand(); break;
                default:
                    _logger.LogWarning($"没有处理的 fitter 命令：{command}");
                    break;
            }
        }

        public void Installation(object product)
        {
            throw new NotImplementedException($"{Tag} 的 installation 不应该被调用");
        }
        #endregion

        #region IBasket
        public void AddMaterial(Socket socket)
        {
            _socket = socket;
        }
        #endregion

        #region 执行命令
        /// <summary>
        /// 执行 run 命令
        /// </summary>
        private void ExecuteRunCommand()
        {
            if (_isWorking)
            {
                _logger.LogWarning($"fitter 正处于工作中，不再响应 {FitterCommand.Run} 命令");
                return;
            }

            if (_socket == null)
            {
                _logger.LogWarning($"没有可以使用的 socket 材料，fitter 无法开始工作");
                return;
            }

            lock (this)
            {
                _isWorking = true;

                //从 pool 中申请用于 receive 和 send 的 SocketAsyncEventArgs
                _receiveEventArg = _pool.Pop();
                _sendEventArg = _pool.Pop();

                _receiveEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnCompleted);
                _sendEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnCompleted);

                if (!_socket.ReceiveAsync(_receiveEventArg)) ProcessReceive(_receiveEventArg);
            }
        }

        /// <summary>
        /// 执行 stop 命令
        /// </summary>
        private void ExecuteStopCommand()
        {
            StopWorking();
        }
        #endregion

        #region SocketAsyncEventArgs 事件处理

        private void OnCompleted(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive: ProcessReceive(e); break;
                default:
                    _logger.LogWarning($"没有进行处理的 SocketAsyncEventArgs 事件：{e.LastOperation}");
                    break;
            }
        }

        /// <summary>
        /// socket 数据接受完成之后，处理数据
        /// </summary>
        /// <param name="e"></param>
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            // 当 socket 中传输的 byte 字节为 0 时，默认 socket 已经关闭连接
            if (e.BytesTransferred > 0)
            {
                if (e.SocketError == SocketError.Success)
                {
                    var buffer = new byte[e.BytesTransferred];
                    Array.Copy(e.Buffer, e.Offset, buffer, 0, buffer.Length);

                    _logger.LogTrace($"socket 接受到 {e.BytesTransferred} 个 byte 数据");

                    //获取到的 buffer 直接作为下一个 fitter 的零件进行组装
                    if (_nextInstallationFitter != null)
                        _nextInstallationFitter?.Installation(buffer);
                    else
                        ReportData(buffer);

                    if (_isWorking)
                    {
                        if (!_socket.ReceiveAsync(e)) ProcessReceive(e);
                    }
                }
                else
                {
                    ProcessError(e);
                }
            }
            else
            {
                StopWorking();
                ReportClose();
            }
        }

        /// <summary>
        /// 处理 SocketAsyncEventArgs 的错误
        /// </summary>
        /// <param name="e"></param>
        private void ProcessError(SocketAsyncEventArgs e)
        {
            _logger.LogError($"在 socket 发送或者接收的过程中出现了错误：{e.SocketError}");

            StopWorking();
            ReportClose();
        }

        #endregion

        /// <summary>
        /// fitter 停止工作
        /// </summary>
        private void StopWorking()
        {
            if (!_isWorking)
            {
                _logger.LogWarning("尚未工作，不处理停止工作的命令");
                return;
            }

            lock (this)
            {
                _isWorking = false;

                if (_socket != null)
                {
                    try
                    {
                        _socket.Shutdown(SocketShutdown.Both);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"关闭 socket 的过程中出现错误：{ex.ToString()}");
                    }
                }

                _sendEventArg.Completed -= new EventHandler<SocketAsyncEventArgs>(OnCompleted);
                _receiveEventArg.Completed -= new EventHandler<SocketAsyncEventArgs>(OnCompleted);

                _pool.Push(_sendEventArg);
                _pool.Push(_receiveEventArg);
            }
        }

        /// <summary>
        /// 上报 close 事件
        /// </summary>
        private void ReportClose()
        {
            var arg = new FitterReportEventArgs
            {
                Type = FitterReportEvent.Close,
                Datas = null
            };

            Report?.Invoke(this, arg);
        }

        private void ReportData(byte[] buffer)
        {
            var arg = new FitterReportEventArgs
            {
                Type = FitterReportEvent.Data,
                Datas = new object[] { buffer }
            };

            Report?.Invoke(this, arg);
        }
    }
}
