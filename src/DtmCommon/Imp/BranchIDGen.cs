using System;

namespace DtmCommon
{
    public class BranchIDGen
    {
        private static readonly int MAX_BRANCH_ID = 99;

        private static readonly int LENGTH = 20;

        public string BranchID { get; private set; }
        public int SubBranchID { get; private set; }

        public BranchIDGen(string branchID = "")
        {
            this.BranchID = branchID;
            this.SubBranchID = 0;
        }

        public string NewSubBranchID()
        {
            if (this.SubBranchID >= MAX_BRANCH_ID)
            {
                throw new ArgumentException("branch id is larger than 99");
            }
            if (this.BranchID.Length > LENGTH)
            {
                throw new ArgumentException("total branch id is longer than 20");
            }
            this.SubBranchID = this.SubBranchID + 1;

            return CurrentSubBranchID();
        }

        public string CurrentSubBranchID()
            => string.Concat(this.BranchID, this.SubBranchID.ToString().PadLeft(2, '0'));
    }
}