using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange.Core.Interface
{
    /// <summary>
    /// fitter 间传输数据用的 box
    /// </summary>
    public interface IFitterBox
    {
        /// <summary>
        /// buffer 数据
        /// </summary>
        Dictionary<string, byte[]> BufferDatas { get; set; }

        /// <summary>
        /// stirng 数据
        /// </summary>
        string StringData { get; set; }

        /// <summary>
        /// 
        /// </summary>
        object ObjData { get; set; }
    }
}
