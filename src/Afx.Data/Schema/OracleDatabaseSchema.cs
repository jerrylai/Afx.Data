using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Afx.Data.Schema
{
    /// <summary>
    /// 
    /// </summary>
    public class OracleDatabaseSchema : DatabaseSchema
    {
        private Database db;
       /// <summary>
       /// 
       /// </summary>
       /// <param name="db"></param>
       /// <exception cref="ArgumentNullException"></exception>
        public OracleDatabaseSchema(Database db)
        {
            if (db == null) throw new ArgumentNullException("db");
            this.db = db;
        }
 
        /// <summary>
        /// 是否存在数据库
        /// </summary>
        /// <returns>true：存在，false：不存在</returns>
        public override bool Exist()
        {
            object obj = this.db.ExecuteScalar<object>("SELECT 1 FROM DUAL");
            int count = obj != null && obj != DBNull.Value ? Convert.ToInt32(obj) : 0;

            return count > 0;
        }
        /// <summary>
        /// 创建数据库
        /// </summary>
        /// <returns>true：创建成功，false：创建失败</returns>
        public override bool CreateDatabase()
        {
            var obj = this.db.ExecuteScalar<long?>("SELECT 1 FROM DUAL");
            int count = obj != null ? Convert.ToInt32(obj) : 0;

            return count > 0;
        }
        /// <summary>
        /// 删除数据库
        /// </summary>
        /// <returns>true：删除成功，false：删除失败</returns>
        public override bool DeleteDatabase()
        {

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Dispose()
        {
            if (db != null) db.Dispose();
            this.db = null;
            base.Dispose();
        }
    }
}
