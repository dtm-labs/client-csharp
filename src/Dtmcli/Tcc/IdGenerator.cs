using System;

namespace Dtmcli
{
    public class IdGenerator
    {
        private string parentId;
        private int branchId;

        public IdGenerator(string parentId ="")
        {
            this.parentId = parentId;
            this.branchId = 0;
        }

        public string NewBranchId()
        {
            if (this.branchId >= 99)
            {
                throw new ArgumentException("branch id is larger than 99");
            }
            if (this.parentId.Length > 20)
            {
                throw new ArgumentException("total branch id is longer than 20");
            }
            this.branchId = this.branchId + 1;

            return this.parentId + this.branchId.ToString().PadRight(2, '0');
        }
    }
}
