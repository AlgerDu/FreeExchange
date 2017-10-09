using D.FreeExchange.Core.Interface;
using D.Util.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange.Core.Fitters
{
    /// <summary>
    /// socket fitter
    /// 最基层的 fitter，用于对 socket 的一些封装
    /// </summary>
    public class SocketFitter : IFitter
    {
        public static string Tag = "Socket";

        ILogger _logger;

        bool _isWorking;

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
            ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SocketFitter>();
        }

        #region IFitter 行为
        public event FitterReportHandler OnReport;

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
