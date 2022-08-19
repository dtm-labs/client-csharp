namespace DtmCommon
{
    public interface IDbSpecial
    {
        string Name { get; }

        string GetPlaceHoldSQL(string sql);

        string GetInsertIgnoreTemplate(string tableAndValues, string pgConstraint);

        string GetXaSQL(string command, string xid);
    }

}