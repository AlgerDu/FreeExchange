using System;
using System.Collections.Generic;
using System.Text;

namespace D.Example.LiveChat
{
    /// <summary>
    /// 用户
    /// </summary>
    public class User
    {
        /// <summary>
        /// 唯一 ID
        /// </summary>
        public Guid Uid { get; set; }

        /// <summary>
        /// 昵称
        /// </summary>
        public string Nickname { get; set; }

        /// <summary>
        /// 其他人想和这个用户，聊天，需要知道他的验证码
        /// </summary>
        public string IndentifyCode { get; set; }

        /// <summary>
        /// 是否在线
        /// </summary>
        public bool Online { get; set; }
    }
}
