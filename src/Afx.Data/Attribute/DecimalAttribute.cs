using System;
using System.Collections.Generic;
using System.Text;

namespace Afx.Data.DataAnnotations
{
    /// <summary>
    /// Decimal Attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple=false)]
    public class DecimalAttribute : Attribute
    {
        /// <summary>
        /// 总长度
        /// </summary>
        public int Precision { get; private set; }
        /// <summary>
        /// 小数位数
        /// </summary>
        public int Scale { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="precision">总长度</param>
        /// <param name="scale">小数位数</param>
        public DecimalAttribute(int precision, int scale)
        {
            this.Precision = precision;
            this.Scale = scale;
        }
    }
}
