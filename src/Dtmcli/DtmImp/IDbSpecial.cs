using System;
using System.Collections.Generic;

namespace Dtmcli.DtmImp
{
    public interface IDbSpecial
    {
        string GetPlaceHoldSQL(string sql);

        string GetInsertIgnoreTemplate(string tableAndValues, string pgConstraint);

        string GetXaSQL(string command, string xid);
    }

    public class MysqlDBSpecial : IDbSpecial
    {
        private MysqlDBSpecial()
        { }

        private static readonly Lazy<MysqlDBSpecial> Instancelock =
                    new Lazy<MysqlDBSpecial>(() => new MysqlDBSpecial());

        public static MysqlDBSpecial Instance => Instancelock.Value;

        public string GetInsertIgnoreTemplate(string tableAndValues, string pgConstraint)
            => string.Format("insert ignore into {0}", tableAndValues);

        public string GetPlaceHoldSQL(string sql)
            => sql;

        public string GetXaSQL(string command, string xid)
            => string.Format("xa {0} '{1}'", command, xid);
    }

    public class PostgresDBSpecial : IDbSpecial
    {
        private PostgresDBSpecial()
        { }

        private static readonly Lazy<PostgresDBSpecial> Instancelock =
                    new Lazy<PostgresDBSpecial>(() => new PostgresDBSpecial());

        public static PostgresDBSpecial Instance => Instancelock.Value;

        public string GetInsertIgnoreTemplate(string tableAndValues, string pgConstraint)
            => string.Format("insert into {0} on conflict ON CONSTRAINT {1} do nothing", tableAndValues, pgConstraint);

        public string GetPlaceHoldSQL(string sql)
            => sql;

        public string GetXaSQL(string command, string xid)
        {
            var dict = new System.Collections.Generic.Dictionary<string, string> 
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

    public class DbSpecialDelegate
    {
        private DbSpecialDelegate()
        { }

        private static readonly Lazy<DbSpecialDelegate> Instancelock =
                    new Lazy<DbSpecialDelegate>(() => new DbSpecialDelegate());

        public static DbSpecialDelegate Instance => Instancelock.Value;

        private readonly Dictionary<string, IDbSpecial> _dbSpecials = new Dictionary<string, IDbSpecial>() 
        {
            { Constant.Barrier.DBTYPE_MYSQL, MysqlDBSpecial.Instance },
            { Constant.Barrier.DBTYPE_POSTGRES, PostgresDBSpecial.Instance },
        };
        private string _currentDBType = Constant.Barrier.DBTYPE_MYSQL;

        public IDbSpecial GetDBSpecial()
            => _dbSpecials[_currentDBType];

        public void SetCurrentDBType(string dbType)
        {
            if (_dbSpecials.TryGetValue(dbType, out _))
            {
                _currentDBType = dbType;
            }
            else
            {
                throw new System.Exception($"unknown db type '{dbType}'");
            }
        }

        public string GetCurrentDBType()
            => _currentDBType;
    }
}
