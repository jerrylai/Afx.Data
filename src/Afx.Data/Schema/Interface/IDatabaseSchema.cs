using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Afx.Data.Schema
{
    /// <summary>
    /// 获取数据库结构信息
    /// </summary>
    public interface IDatabaseSchema : IDisposable
    {
        /// <summary>
        /// 是否存在数据库
        /// </summary>
        /// <returns>true：存在，false：不存在</returns>
        bool Exist();

        /// <summary>
        /// 创建数据库
        /// </summary>
        /// <returns>true：创建成功，false：创建失败</returns>
        bool CreateDatabase();

        /// <summary>
        /// 删除数据库
        /// </summary>
        /// <returns>true：删除成功，false：删除失败</returns>
        bool DeleteDatabase();
    }
}
