using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;

namespace DtmCommon
{
    public class DbSpecialDelegate
    {
        private readonly IDbSpecial _special;

        private readonly Dictionary<string, IDbSpecial> _specialDic;

        public DbSpecialDelegate(IEnumerable<IDbSpecial> specials, IOptions<DtmOptions> optionsAccs)
        {
            this._specialDic = specials.ToDictionary((i) => i.Name);
            this._specialDic.TryGetValue(optionsAccs.Value.SqlDbType,out _special);
            if (_specialDic.TryGetValue(optionsAccs.Value.SqlDbType, out _special) == false)
                throw new DtmException($"unknown db type '{optionsAccs.Value.SqlDbType}'");
        }

        public IDbSpecial GetDbSpecial() => _special;

        public IDbSpecial GetDbSpecialByName(string sqlDbType)
        {
            IDbSpecial special;
            if (this._specialDic.TryGetValue(sqlDbType, out special) == false)
                throw new DtmException($"unknown db type '{sqlDbType}'");

            return special;
        }
    }

}