using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange.Core.Interface
{
    /// <summary>
    /// 装配工
    /// </summary>
    public interface IFitter
    {
        /// <summary>
        /// 类型标签
        /// </summary>
        string Tag { get; }

        /// <summary>
        /// 装配（数据组包）
        /// </summary>
        /// <param name="product"></param>
        void Installation(object product);

        /// <summary>
        /// 拆卸（数据分包）
        /// </summary>
        /// <param name="product"></param>
        void Dismantling(object product);

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="command"></param>
        void ExecuteCommand(FitterCommand command);

        event FitterReportHandler OnReport;
    }
}
