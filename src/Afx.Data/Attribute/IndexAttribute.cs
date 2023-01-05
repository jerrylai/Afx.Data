using System;
using System.Collections.Generic;
using System.Text;

namespace Afx.Data.DataAnnotations
{
    /// <summary>
    /// 索引 Attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple=false)]
    public class IndexAttribute : Attribute
    {
        /// <summary>
        /// 索引名称
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// 是否唯一索引
        /// </summary>
        public bool IsUnique { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public IndexAttribute() : this(null, false)
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">索引名称</param>
        public IndexAttribute(string name) : this(name, false)
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">索引名称</param>
        /// <param name="isUnique">是否唯一索引</param>
        public IndexAttribute(string name, bool isUnique)
        {
            this.Name = name;
            this.IsUnique = isUnique;
        }
    }
}
