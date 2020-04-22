using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace Afx.Data
{
    /// <summary>
    /// IDataReader 读取结果转换成实体
    /// </summary>
    public interface IReaderToModel : IDisposable
    {
        /// <summary>
        /// 转换实体
        /// </summary>
        /// <param name="reader">IDataReader</param>
        /// <returns></returns>
        object To(IDataReader reader);
    }
}
