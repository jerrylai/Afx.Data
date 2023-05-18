using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Afx.Data.Schema
{
    /// <summary>
    /// 
    /// </summary>
    public class SQLiteDatabaseSchema : DatabaseSchema
    {
        private Database db;
        private string file;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="db"></param>
        /// <param name="file"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public SQLiteDatabaseSchema(Database db, string file)
        {
            if (string.IsNullOrEmpty(file)) throw new ArgumentNullException("file");
            //Data Source=C:\FileSystem.db;Password=mycsv.cn;Version=3;Pooling=True;UseUTF16Encoding=False;DateTimeKind=Local
//            this.connectionString = connectionString;
//#if NETCOREAPP || NETSTANDARD
//            var stringBuilder = new SqliteConnectionStringBuilder(connectionString);
//#else
//            var stringBuilder = new SQLiteConnectionStringBuilder(connectionString);
//#endif
            this.db = db;
            this.file = file;
        }
        /// <summary>
        /// 是否存在数据库
        /// </summary>
        /// <returns>true：存在，false：不存在</returns>
        public override bool Exist()
        {
            int count = System.IO.File.Exists(this.file) ? 1 : 0;

            return count > 0;
        }
        /// <summary>
        /// 创建数据库
        /// </summary>
        /// <returns>true：创建成功，false：创建失败</returns>
        public override bool CreateDatabase()
        {
            string path = System.IO.Path.GetDirectoryName(this.file);
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }
            db.ExecuteNonQuery("create table _tb_create_db_temp(id int not null)");
            db.ExecuteNonQuery("drop table _tb_create_db_temp");

            return true;
        }
        /// <summary>
        /// 删除数据库
        /// </summary>
        /// <returns>true：删除成功，false：删除失败</returns>
        public override bool DeleteDatabase()
        {
            if (System.IO.File.Exists(this.file))
            {
                System.IO.File.Delete(this.file);
            }

            return true;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public override void Dispose()
        {
            if (db != null) db.Dispose();
            this.db = null;
            this.file = null;
            base.Dispose();
        }
    }
}
