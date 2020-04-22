using System;

namespace Afx.Data
{
    /// <summary>
    /// like 查询
    /// </summary>
    public enum DbLikeType
    {
        /// <summary>
        /// like '%ss'
        /// </summary>
        Left = 1,
        /// <summary>
        /// like 'ss%'
        /// </summary>
        Right = 2,
        /// <summary>
        /// like '%ss%'
        /// </summary>
        All = 3
    }
}
