using D.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace D.FreeExchange
{
    public class ExchangeMessageCache : ExchangeMessageForPayload
    {
        public TaskCompletionSource<IResult> TCS { get; set; }

        public ExchangeMessageCache()
        {
            Uid = Guid.NewGuid();
            State = ExchangeMessageState.Create;
            TCS = new TaskCompletionSource<IResult>();
            Timestamp = DateTimeOffset.Now;
        }
    }

    public class ExchangeMessageForPayload
    {
        public Guid? Uid { get; set; }

        public ExchangeCode? Code { get; set; }

        public string Msg { get; set; }

        public ExchangeMessageState? State { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public string Url { get; set; }

        public object[] Request { get; set; }

        public object Response { get; set; }

        public TimeSpan? Timeout { get; set; }

        public List<IByteDescription> ByteDescriptions { get; set; }
    }
}
