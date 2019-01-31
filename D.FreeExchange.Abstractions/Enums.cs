using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange
{
    /// <summary>
    /// IExchangeMessage 状态码
    /// </summary>
    public enum ExchangeMessageState
    {
        Create = 0,
        Sending = 1,
        Recevied = 2,
        Processing = 3,
        ProcessTimeout = 4,
        Responsing = 5,
        Complete = 6
    }

    /// <summary>
    /// 交换码
    /// </summary>
    public enum ExchangeCode
    {
        OK = 0,
        Error = 1,

        SentTimeout = 3001,
        ReceivceTimeout = 3002,
        SentBufferFull = 3003,

        ActionExecuteError = 4001
    }
}
