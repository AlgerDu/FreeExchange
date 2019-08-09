using System;
using System.Collections.Generic;
using System.Text;

namespace D.Example.LiveChat
{
    /// <summary>
    /// 聊天消息
    /// </summary>
    public class ChatMessage
    {
        public Guid Uid { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 发送时间
        /// </summary>
        public DateTimeOffset SendTime { get; set; }

        /// <summary>
        /// 发送人
        /// </summary>
        public User Sender { get; set; }

        /// <summary>
        /// 聊天地点的唯一 ID
        /// </summary>
        public Guid ChatPlaceUid { get; set; }
    }
}
