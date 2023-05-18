using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;

namespace Afx.Data.Schema
{
    /// <summary>
    /// 获取数据库结构信息
    /// </summary>
    public abstract class DatabaseSchema : IDatabaseSchema
    {
        /// <summary>
        /// 是否存在数据库
        /// </summary>
        /// <returns>true：存在，false：不存在</returns>
        public abstract bool Exist();

        /// <summary>
        /// 创建数据库
        /// </summary>
        /// <returns>true：创建成功，false：创建失败</returns>
        public abstract bool CreateDatabase();

        /// <summary>
        /// 删除数据库
        /// </summary>
        /// <returns>true：删除成功，false：删除失败</returns>
        public abstract bool DeleteDatabase();

        /// <summary>
        /// 释放所有资源
        /// </summary>
        public virtual void Dispose()
        {
            
        }
    }
}
