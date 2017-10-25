using D.FreeExchange.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D.FreeExchange.Core.Models;
using D.Util.Interface;

namespace D.FreeExchange.Fitter.Young
{
    /// <summary>
    /// 自定义封装的一个 fitter；实现了一个自定义的传输协议，扩展改编自 websocket 协议，
    /// 
    /// </summary>
    public class YoungFitter : IFitter
    {
        public static string Tag = "Young";

        ILogger _logger;

        bool _isWorking;

        #region IFitter 属性
        public bool IsWorking => _isWorking;

        string IFitter.Tag => Tag;

        public event EventHandler<FitterReportEventArgs> Report;
        #endregion

        public YoungFitter(
            ILoggerFactory loggerFactory
            )
        {
            _logger = loggerFactory.CreateLogger<YoungFitter>();
        }

        #region IFitter 行为
        public void Connect(IFitter i, IFitter d)
        {
            throw new NotImplementedException();
        }

        public void Dismantling(object product)
        {
            throw new NotImplementedException();
        }

        public void ExecuteCommand(FitterCommand command)
        {
            throw new NotImplementedException();
        }

        public void Installation(object product)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
