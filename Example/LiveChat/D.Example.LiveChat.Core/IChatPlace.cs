using D.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace D.Example.LiveChat
{
    /// <summary>
    /// 聊天地点
    /// </summary>
    public interface IChatPlace
    {
        Guid Uid { get; }

        IReadOnlyDictionary<Guid, User> Users { get; }

        IEnumerable<ChatMessage> Messages { get; }

        /// <summary>
        /// 加入一个用户
        /// </summary>
        /// <param name="uer"></param>
        /// <returns></returns>
        IResult Join(User uer);

        /// <summary>
        /// 发布一条消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        IResult PublishMessage(ChatMessage message);
    }
}
