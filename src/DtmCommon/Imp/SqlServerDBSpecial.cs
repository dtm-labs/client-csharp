namespace DtmCommon
{
    public class SqlServerDBSpecial : IDbSpecial
    {
        public string Name => "sqlserver";

        public string GetInsertIgnoreTemplate(string tableAndValues, string pgConstraint)
            => string.Format("insert into {0}", tableAndValues);

        public string GetPlaceHoldSQL(string sql)
            => sql;

        public string GetXaSQL(string command, string xid)
            => throw new DtmException("not support xa now!");
    }

}