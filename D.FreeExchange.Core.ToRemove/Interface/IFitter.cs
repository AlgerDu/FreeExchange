using D.FreeExchange.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange.Core.Interface
{
    /// <summary>
    /// 装配工
    /// 有关fitter 装配和拆卸的参数，想了很久，是定义一个 context 还是说使用 object
    /// 如果是一个 context ，就好像 asp.net core 的中间件一样，一串 fitter 形成了一个 职责链；
    /// 我还想到了学习使用摄像头的时候学到的一些 com 编程的内容，里面为了使两个 com 组件能够连接到一起，所定义的那些复杂的对象，有什么获取接口啊，获取接口参数类型啊，缓冲区啊，等等等等
    /// context 从头到尾都是一个对象，还想和《装配和安装》没有任何关系，只能算是处理，com 的方式才是我想要的，当然不需要那么复杂，简单就好
    /// 所有，在我写这段话的时候，还是决定使用 object，当然这也要求了使用者在使用不同的 fitter 组成一个生成线时，必需保证两个连接在一起的 fitter 能够一起工作
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
        event EventHandler<FitterReportEventArgs> Report;
    }
}
