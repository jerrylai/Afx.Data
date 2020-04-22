using System;
using System.Data;

namespace Afx.Data
{
    /// <summary>
    /// sql 参数 class 转换接口
    /// </summary>
    internal interface IModelToParam
    {
        /// <summary>
        /// sql 参数 class 转换
        /// </summary>
        /// <param name="db">IDatabase</param>
        /// <param name="command">IDbCommand</param>
        /// <param name="sql">sql</param>
        /// <param name="parameters">sql 参数</param>
        void To(IDatabase db, IDbCommand command, string sql, object parameters);
    }
}
