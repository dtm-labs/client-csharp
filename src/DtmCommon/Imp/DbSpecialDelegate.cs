﻿using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;

namespace DtmCommon
{
    public class DbSpecialDelegate
    {
        private readonly IDbSpecial _special;

        public DbSpecialDelegate(IEnumerable<IDbSpecial> specials, IOptions<DtmOptions> optionsAccs)
        {
            var dbSpecial = specials.FirstOrDefault(x => x.Name.Equals(optionsAccs.Value.SqlDbType));

            if (dbSpecial == null) throw new DtmException($"unknown db type '{optionsAccs.Value.SqlDbType}'");

            _special = dbSpecial;
        }

        public IDbSpecial GetDbSpecial() => _special;
    }

}