using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace D.FreeExchange.Protocol.DP
{
    internal class PayloadAnalyser : IPayloadAnalyser
    {
        ILogger _logger;
        Encoding _encoding;
        DProtocolOptions _options;

        public PayloadAnalyser(
            ILogger logger
            )
        {
            _logger = logger;
        }

        public void UpdateParams(Encoding encoding, DProtocolOptions options)
        {
            _encoding = encoding;
            _options = options;
        }

        public IEnumerable<IPackage> Analyse(IProtocolPayload payload)
        {
            List<IPackage> packages = new List<IPackage>();

            var textBuffer = _encoding.GetBytes(payload.Text);

            BufferToPackages(packages, textBuffer, PackageCode.Text);

            if (packages.Count == 1)
            {
                packages[0].Flag = FlagCode.Single;
            }
            else
            {
                packages[0].Flag = FlagCode.Start;
                packages[packages.Count - 1].Flag = FlagCode.End;
            }

            return packages;
        }

        private void BufferToPackages(List<IPackage> packages, byte[] buffer, PackageCode code)
        {
            var offset = 0;

            do
            {
                var length = buffer.Length - offset < _options.MaxPayloadDataLength
                    ? buffer.Length - offset
                    : _options.MaxPayloadDataLength;

                var package = new PackageWithPayload(code, length);

                Array.Copy(buffer, offset, package.Payload, 0, length);

                offset += length;

                packages.Add(package);

            } while (offset < buffer.Length);
        }
    }
}
