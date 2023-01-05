using System;

namespace Afx.Data.DataAnnotations
{
    /// <summary>
    /// 索引 Attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple=false)]
    public class NonClusteredAttribute : Attribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public NonClusteredAttribute()
        {

        }
    }
}
