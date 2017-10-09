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
        /// 是否正在运行
        /// </summary>
        bool IsWorking { get; }

        /// <summary>
        /// 类型标签
        /// </summary>
        string Tag { get; }

        /// <summary>
        /// 将两个 fitter 联系起来
        /// </summary>
        /// <param name="i">用于装配的下一个 fitter</param>
        /// <param name="d">用于拆卸的下一个 fitter</param>
        void Connect(IFitter i, IFitter d);

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

        /// <summary>
        /// 上报一些事件，目前只有 close 事件
        /// </summary>
        event FitterReportHandler OnReport;
    }
}
