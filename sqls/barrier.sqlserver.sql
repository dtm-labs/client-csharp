IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = 'dtm_barrier')
BEGIN
   CREATE DATABASE dtm_barrier
   USE dtm_barrier
END

GO

IF EXISTS (SELECT * FROM sysobjects WHERE id = object_id(N'[dbo].[barrier]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)  
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