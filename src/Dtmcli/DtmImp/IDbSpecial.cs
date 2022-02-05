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

    public class SqlServerDBSpecial : IDbSpecial
    {
        private SqlServerDBSpecial()
        { }

        private static readonly Lazy<SqlServerDBSpecial> Instancelock =
                    new Lazy<SqlServerDBSpecial>(() => new SqlServerDBSpecial());

        public static SqlServerDBSpecial Instance => Instancelock.Value;

        /*

IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = 'dtm_barrier')
BEGIN
    CREATE DATABASE dtm_barrier
    USE dtm_barrier
END

GO

IF EXISTS (SELECT * FROM sysobjects WHERE id = object_id(N’[dbo].[barrier]’) and OBJECTPROPERTY(id, N’IsUserTable’) = 1)  
BEGIN
  DROP TABLE [dbo].[barrier]
END

GO

CREATE TABLE [dbo].[barrier]
(
    [id] bigint NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [trans_type] varchar(45) NOT NULL DEFAULT(''),
    [gid] varchar(128) NOT NULL DEFAULT(''),
    [branch_id] varchar(128) NOT NULL DEFAULT(''),
    [op] varchar(45) NOT NULL DEFAULT(''),
    [barrier_id] varchar(45) NOT NULL DEFAULT(''),
    [reason] varchar(45) NOT NULL DEFAULT(''),
    [create_time] datetime NOT NULL DEFAULT(getdate()) ,
    [update_time] datetime NOT NULL DEFAULT(getdate())
)

GO

CREATE UNIQUE INDEX[ix_uniq_barrier] ON[dbo].[barrier]
        ([gid] ASC, [branch_id] ASC, [op] ASC, [barrier_id] ASC)
WITH(IGNORE_DUP_KEY = ON)

GO
         */
        public string GetInsertIgnoreTemplate(string tableAndValues, string pgConstraint)
            => string.Format("insert into {0}", tableAndValues);

        public string GetPlaceHoldSQL(string sql)
            => sql;

        public string GetXaSQL(string command, string xid)
            => throw new DtmcliException("not support xa now!!!");
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
            { Constant.Barrier.DBTYPE_SQLSERVER, SqlServerDBSpecial.Instance },
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
                throw new DtmcliException($"unknown db type '{dbType}'");
            }
        }

        public string GetCurrentDBType()
            => _currentDBType;
    }
}
