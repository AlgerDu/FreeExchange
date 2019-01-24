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
}
