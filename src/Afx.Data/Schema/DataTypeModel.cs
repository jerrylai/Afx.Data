using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Afx.Data.Schema
{
    /// <summary>
    /// 数据库类型model
    /// </summary>
    public class DataTypeModel
    {
        /// <summary>
        /// 数据库类型
        /// </summary>
        public string DbDataType { get; set; }

        /// <summary>
        /// C# 类型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        ///  C# 可空类型
        /// </summary>
        public string NullableType { get; set; }
    }
}
