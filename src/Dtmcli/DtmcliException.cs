using System;

namespace Dtmcli
{
    public class DtmcliException : Exception
    {
        public DtmcliException(string message)
            : base(message)
        {
        }
    }
}
