namespace DtmCommon
{
    public class MysqlDBSpecial : IDbSpecial
    {
        public string Name => "mysql";

        public string GetInsertIgnoreTemplate(string tableAndValues, string pgConstraint)
            => string.Format("insert ignore into {0}", tableAndValues);

        public string GetPlaceHoldSQL(string sql)
            => sql;

        public string GetXaSQL(string command, string xid)
            => string.Format("xa {0} '{1}'", command, xid);
    }

}