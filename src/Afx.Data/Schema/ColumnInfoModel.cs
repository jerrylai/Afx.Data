using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Afx.Data.Schema
{
    /// <summary>
    /// 列信息model
    /// </summary>
    public class ColumnInfoModel
    {
        /// <summary>
        /// 列顺序位置
        /// </summary>
        public int Order { get; set; }
        
        /// <summary>
        /// 列名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 列数据类型
        /// </summary>
        public string DataType { get; set; }
        
        /// <summary>
        /// 列最大长度
        /// </summary>
        public int MaxLength { get; set; }

        /// <summary>
        /// 列最小长度
        /// </summary>
        public int MinLength { get; set; }
        
        /// <summary>
        /// 是否可空
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// 是否主键
        /// </summary>
        public bool IsKey { get; set; }

        /// <summary>
        /// 是否自增
        /// </summary>
        public bool IsAutoIncrement { get; set; }

        /// <summary>
        /// 索引
        /// </summary>
        public List<IndexModel> Indexs { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool IsNonClustered { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class IndexModel
    {
        /// <summary>
        /// 索引名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 是否唯一索引
        /// </summary>
        public bool IsUnique { get; set; }

        /// <summary>
        /// 列名
        /// </summary>
        public string ColumnName { get; set; }
    }
}
