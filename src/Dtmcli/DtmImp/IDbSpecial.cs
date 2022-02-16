using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dtmcli.DtmImp
{
    public interface IDbSpecial
    {
        string Name { get; }

        string GetPlaceHoldSQL(string sql);

        string GetInsertIgnoreTemplate(string tableAndValues, string pgConstraint);

        string GetXaSQL(string command, string xid);
    }

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

    public class PostgresDBSpecial : IDbSpecial
    {
        public string Name => "postgres";

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
        public string Name => "sqlserver";

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
        private readonly IDbSpecial _special;

        public DbSpecialDelegate(IEnumerable<IDbSpecial> specials, IOptions<DtmOptions> optionsAccs)
        {
            var dbSpecial = specials.FirstOrDefault(x => x.Name.Equals(optionsAccs.Value.DBType));

            if (dbSpecial == null) throw new DtmcliException($"unknown db type '{optionsAccs.Value.DBType}'");

            _special = dbSpecial;
        }

        public IDbSpecial GetDbSpecial() => _special;
    }
}
