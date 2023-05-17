using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data.Schema
{
    /// <summary>
    /// 
    /// </summary>
    public class MsSqlServerDatabaseSchema : DatabaseSchema
    {
        private Database db;
        private string database;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="master"></param>
        /// <param name="database"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public MsSqlServerDatabaseSchema(Database master, string database)
        {
            if (db == null) throw new ArgumentNullException("db");
            if (string.IsNullOrEmpty(database)) throw new ArgumentNullException("database");
            this.database = database;
            this.db = master;
        }
        /// <summary>
        /// 是否存在数据库
        /// </summary>
        /// <returns></returns>
        public override bool Exist()
        {
            var count = this.db.ExecuteScalar<int>("SELECT COUNT(1) FROM sys.databases WHERE name=@name", new { name = database });

            return count > 0;
        }
        /// <summary>
        /// 创建数据库
        /// </summary>
        /// <returns>true：创建成功，false：创建失败</returns>
        public override async Task<bool> CreateDatabase()
        {
            //var  sql = @"CREATE DATABASE [ssss] ON  PRIMARY 
            //( NAME = N'ssss', FILENAME = N'D:\Program Files\Microsoft SQL Server\DATA\ssss.mdf' , SIZE = 3072KB , FILEGROWTH = 1024KB )
            // LOG ON 
            //( NAME = N'ssss_log', FILENAME = N'D:\Program Files\Microsoft SQL Server\DATA\ssss_log.ldf' , SIZE = 1024KB , FILEGROWTH = 10%)";
            var sql = string.Format("CREATE DATABASE [{0}]", database);
            int count = await this.db.ExecuteNonQuery(sql);
            return count > 0;
        }
        /// <summary>
        /// 删除数据库
        /// </summary>
        /// <returns>true：删除成功，false：删除失败</returns>
        public override async Task<bool> DeleteDatabase()
        {
            var sql = string.Format("DROP DATABASE [{0}]", database);
            int count = await this.db.ExecuteNonQuery(sql);

            return count > 0;
        }
        /// <summary>
        /// 释放所有资源
        /// </summary>
        public override void Dispose()
        {
            if(db != null) db.Dispose();
            this.db = null;
            base.Dispose();
        }

    }
}
