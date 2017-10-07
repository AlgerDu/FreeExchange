using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange.Core.Interface
{
    /// <summary>
    /// fitter 工厂
    /// </summary>
    public interface IFitterFactory
    {
        /// <summary>
        /// 根据 tag 的顺序生成一个 生产线，每个 tag 对应一种 fitter
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        IProductionLine Employ(params string[] tags);
    }
}
