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
        Create,
        Sending,
        Recevied,
        Processing,
        ProcessTimeout,
        Responsing,
        Complete
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
