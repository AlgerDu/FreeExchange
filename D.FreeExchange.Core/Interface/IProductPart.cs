using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange.Core.Interface
{
    /// <summary>
    /// IFitter 用零件
    /// </summary>
    public interface IProductPart
    {
        /// <summary>
        /// 字符串数据
        /// </summary>
        string TxtData { get; set; }

        /// <summary>
        /// 字节数据
        /// </summary>
        IDictionary<string, byte[]> BufferData { get; set; }
    }
}
