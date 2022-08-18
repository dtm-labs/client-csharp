using System.Collections.Generic;

namespace DtmCommon
{
    public class PostgresDBSpecial : IDbSpecial
    {
        public string Name => "postgres";

        public string GetInsertIgnoreTemplate(string tableAndValues, string pgConstraint)
            => string.Format("insert into {0} on conflict ON CONSTRAINT {1} do nothing", tableAndValues, pgConstraint);

        public string GetPlaceHoldSQL(string sql)
            => sql;

        public string GetXaSQL(string command, string xid)
        {
            var dict = new Dictionary<string, string>
            {
                { "end", "" },
                { "start", "begin" },
                { "prepare", $"prepare transaction '{xid}'" },
                { "commit", $"commit prepared '{xid}'" },
                { "rollback", $"rollback prepared '{xid}'" },
            };

            return dict.TryGetValue(command, out var sql) ? sql : string.Empty;
        }
    }

}