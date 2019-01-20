using D.FreeExchange.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange.Core.Models
{
    /// <summary>
    /// fitter 上报
    /// </summary>
    public class FitterReportEventArgs : EventArgs
    {
        public FitterReportEvent Type { get; set; }

        public object[] Datas { get; set; }
    }
}
