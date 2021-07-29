using System;
using System.Collections.Generic;
using System.Text;

namespace Dtmcli
{
    public class RegisterTccBranch
    {
        public string Gid { get; set; }

        public string Branch_id { get; set; }

        public string Trans_type { get; set; } = "tcc";

        public string Status { get; set; } = "prepared";

        public string Data { get; set; }

        public string Try { get; set; }

        public string Confirm { get; set; }

        public string Cancel { get; set; }
    }
}
