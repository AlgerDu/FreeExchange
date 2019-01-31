using D.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange.Core
{
    public class ExchangeMessageCache :
        ExchangeMessage
        , IExchangeMessage
    {
        public Guid Uid { get; set; }

        public object Response { get; set; }

        public ExchangeMessageState State { get; set; }

        public TaskCompletionSource<IResult> TCS { get; set; }

        public ExchangeMessageCache(IExchangeMessage msg)
        {
            this.Params = msg.Params;
            this.Timeout = msg.Timeout;
            this.Url = msg.Url;

            Uid = Guid.NewGuid();
            State = ExchangeMessageState.Create;
            TCS = new TaskCompletionSource<IResult>();
        }
    }

    public class ExchangeMessageForPayload
    {
        public Guid Uid { get; set; }

        public ExchangeMessageState? State { get; set; }

        public string Url { get; set; }

        public object Response { get; set; }

        public object[] Params { get; set; }

        public TimeSpan? Timeout { get; set; }
    }
}
