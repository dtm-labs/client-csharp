using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtmworkflow
{
    internal partial class Workflow
    {
        private (dtmgpb.DtmProgressesReply, Exception) GetProgress()
        {
            var reply = new dtmgpb.DtmProgressesReply();

            return (reply, null);
        }

        private Exception Submit(byte[] result, Exception err)
        {

            return null;
        }

        private Exception RegisterBranch(byte[] res, string branchId, string op, string status)
        {
            return null;
        }
    }
}
