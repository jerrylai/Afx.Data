using System;
using System.Collections.Generic;
using System.Text;

namespace Afx.Data.Schema
{
    /// <summary>
    /// mysql
    /// </summary>
    public class MySqlDatabaseSchema : DatabaseSchema
    {
        private Database db;
        private string database;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mysql"></param>
        /// <param name="database"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public MySqlDatabaseSchema(Database mysql, string database)
        {
            if (db == null) throw new ArgumentNullException("db");
            if (string.IsNullOrEmpty(database)) throw new ArgumentNullException("database");
            //Server=127.0.0.1;Port=3306;Database=FileSystem;User Id=root;Password=mycsv.cn;CharacterSet=UTF8;Pooling=True;MinPoolSize=1;MaxPoolSize=100;ConnectionLifeTime=30;Keepalive=30
            //var connectionStringBuilder = new MySqlConnectionStringBuilder(connectionString);
            //this.database = connectionStringBuilder.Database;
            //connectionStringBuilder.Database = "mysql";
            //connectionStringBuilder.Remove("MinPoolSize");
            //connectionStringBuilder.Remove("MaxPoolSize");
            //connectionStringBuilder.Remove("ConnectionLifeTime");
            //connectionStringBuilder.Remove("Keepalive");
            //connectionStringBuilder.Pooling = false;

            this.db = mysql;
            this.database = database;
        }
        /// <summary>
        /// 是否存在数据库
        /// </summary>
        /// <returns>true：存在，false：不存在</returns>
        public override bool Exist()
        {
            var sql = "SELECT COUNT(1) FROM `information_schema`.`SCHEMATA` WHERE schema_name = @name";
            int count = this.db.ExecuteScalar<int>(sql, new { name = this.database });

            return count > 0;
        }
        /// <summary>
        /// 创建数据库
        /// </summary>
        /// <returns>true：创建成功，false：创建失败</returns>
        public override bool CreateDatabase()
        {
            var sql = string.Format("CREATE DATABASE `{0}` CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;",
                this.database);
            int count = this.db.ExecuteNonQuery(sql);

            return count > 0;
        }
        /// <summary>
        /// 删除数据库
        /// </summary>
        /// <returns>true：删除成功，false：删除失败</returns>
        public override bool DeleteDatabase()
        {
            var sql = string.Format("DROP DATABASE `{0}`;",
                this.database);
            int count = this.db.ExecuteNonQuery(sql);

            return count > 0;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public override void Dispose()
        {
            if (db != null) db.Dispose();
            this.db = null;
            base.Dispose();
        }
    }
}
