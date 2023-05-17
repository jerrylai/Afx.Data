using System;
using System.Text;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Afx.Data
{
    /// <summary>
    /// 数据访问
    /// </summary>
    public interface IDatabase : IDisposable
    {
        /// <summary>
        /// 资源是否释放
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// 连接字符串
        /// </summary>
        string ConnectionString { get; }

        /// <summary>
        /// 是否开启保持连接
        /// </summary>
        bool IsOpenKeepConnection { get; }

        /// <summary>
        /// DB 提供程序Factory
        /// </summary>
        DbProviderFactory ProviderFactory { get; }


        /// <summary>
        /// （以秒为单位）
        /// </summary>
        int? CommandTimeout { get; set; }

        /// <summary>
        /// IDbConnection
        /// </summary>
        IDbConnection Connection { get; }

        /// <summary>
        /// 执行sql logs
        /// </summary>
        Action<string> Log { get; set; }

        /// <summary>
        /// 执行CommitCallback action 错误
        /// </summary>
        Action<Exception> CommitCallbackError { get; set; }

        /// <summary>
        ///  commit or SaveChanges 成功之后执行action list
        /// </summary>
        List<Action<IDatabase>> CommitCallbackList { get; }
        /// <summary>
        /// 添加 commit or SaveChanges 成功之后执行action
        /// action 只执行一次
        /// </summary>
        /// <param name="action">需要执行的action</param>
        /// <returns>添加成功，返回所在的位置</returns>
        int AddCommitCallback(Action<IDatabase> action);

        /// <summary>
        /// 移除commit or SaveChanges 成功之后执行action
        /// </summary>
        /// <param name="action">需要执行的action</param>
        /// <returns>移除成功返回true</returns>
        bool RemoveCommitCallback(Action<IDatabase> action);
        /// <summary>
        ///  commit or SaveChanges 成功之后执行action list
        /// </summary>
        List<Func<IDatabase, Task>> CommitCallbackAsyncList { get; }
        /// <summary>
        /// 添加 commit or SaveChanges 成功之后执行action
        /// action 只执行一次
        /// </summary>
        /// <param name="action">需要执行的action</param>
        /// <returns>添加成功，返回所在的位置</returns>
        int AddCommitCallback(Func<IDatabase, Task> action);

        /// <summary>
        /// 移除commit or SaveChanges 成功之后执行action
        /// </summary>
        /// <param name="action">需要执行的action</param>
        /// <returns>移除成功返回true</returns>
        bool RemoveCommitCallback(Func<IDatabase, Task> action);

        /// <summary>
        /// 移除所有action
        /// </summary>
        void ClearCommitCallback();


        /// <summary>
        /// 保持连接一直打开
        /// </summary>
        void OpenKeepConnection();
        /// <summary>
        /// 关闭连接一直打开
        /// </summary>
        void CloseKeepConnection();

#region
        /// <summary>
        /// 参数化查询名称加前缀
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        string EncodeParameterName(string name);

        /// <summary>
        /// 列名转义
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        string EncodeColumn(string column);

        /// <summary>
        /// 创建全新 DbConnection
        /// </summary>
        /// <returns>DbConnection</returns>
        DbConnection CreateConnection();

        /// <summary>
        /// 创建全新 DbCommand
        /// </summary>
        /// <returns>DbCommand</returns>
        DbCommand CreateCommand();

        /// <summary>
        /// 创建全新 DbParameter
        /// </summary>
        /// <returns>DbParameter</returns>
        DbParameter CreateParameter();

        /// <summary>
        /// 创建全新 DbParameter
        /// </summary>
        /// <param name="name">ParameterName</param>
        /// <param name="value">Value</param>
        /// <returns>DbParameter</returns>
        DbParameter CreateParameter(string name, object value);

#endregion

#region 事务
        /// <summary>
        /// 是否开启事务
        /// </summary>
        bool IsTransaction { get; }

        /// <summary>
        /// 开启事务
        /// </summary>
        IDisposable BeginTransaction();

        /// <summary>
        /// 开启事务
        /// </summary>
        /// <param name="isolationLevel">事务级别</param>
        IDisposable BeginTransaction(IsolationLevel isolationLevel);

        /// <summary>
        /// 提交事务
        /// </summary>
        Task Commit();

        /// <summary>
        /// 回滚事务
        /// </summary>
        void Rollback();
#endregion

#region common
        /// <summary>
        /// 查询数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="param">sql参数，model or dictionary string object or IEnumerable&lt;DbParameter&gt; or IEnumerable&lt;IDataParameter&gt;</param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        List<T> Query<T>(string sql, object param = null, CommandType? commandType = null);

        /// <summary>
        /// 执行sql，返回影响行数
        /// </summary>
        /// <param name="sql">sql</param>
        /// <param name="param">sql参数，model or dictionary string object or IEnumerable&lt;DbParameter&gt; or IEnumerable&lt;IDataParameter&gt;</param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        Task<int> ExecuteNonQuery(string sql, object param = null, CommandType? commandType = null);


        /// <summary>
        /// 执行sql，返回第一行的第一列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="param">sql参数，model or dictionary string object or IEnumerable&lt;DbParameter&gt; or IEnumerable&lt;IDataParameter&gt;</param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        T ExecuteScalar<T>(string sql, object param = null, CommandType? commandType = null);
#endregion


#region Get
        /// <summary>
        /// 查询表数据
        /// </summary>
        /// <typeparam name="T">数据库表对应model</typeparam>
        /// <param name="whereParam">new { id=10, name= "1"}</param>
        /// <returns></returns>
        List<T> GetList<T>(object whereParam = null) where T : class, new();

        /// <summary>
        /// 查询表数据
        /// </summary>
        /// <typeparam name="T">数据库表对应model</typeparam>
        /// <param name="whereSql">whereSql: id = @id OR name = @name </param>
        /// <param name="whereParam">new { id=10, name= "1"}</param>
        /// <returns></returns>
        List<T> GetList<T>(string whereSql, object whereParam = null) where T : class, new();

        /// <summary>
        /// 查询表一行数据
        /// </summary>
        /// <typeparam name="T">数据库表对应model</typeparam>
        /// <param name="whereParam">不能为空， new { id=10, name= "1"}</param>
        /// <returns></returns>
        T Get<T>(object whereParam) where T : class, new();

        /// <summary>
        /// 查询表一行数据
        /// </summary>
        /// <typeparam name="T">数据库表对应model</typeparam>
        /// <param name="whereSql">不能为空，whereSql: id = @id OR name </param>
        /// <param name="whereParam">new { id=10, name= "1"}</param>
        /// <returns></returns>
        T Get<T>(string whereSql, object whereParam = null) where T : class, new();
#endregion

#region add

        /// <summary>
        /// 添加数据
        /// </summary>
        /// <typeparam name="T">插入表</typeparam>
        /// <param name="m">参数</param>
        /// <param name="ignore">忽略插入列</param>
        /// <returns></returns>
        Task<int> Add<T>(T m, params string[] ignore) where T : class;

        /// <summary>
        /// 添加数据
        /// </summary>
        /// <param name="table">插入表</param>
        /// <param name="param"></param>
        /// <returns></returns>
        Task<int> Add(string table, object param);

        #endregion

        #region update

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="table">数据表</param>
        /// <param name="setParam">set 参数</param>
        /// <param name="whereParam">where参数</param>
        /// <returns></returns>
        Task<int> Update(string table, object setParam, object whereParam);

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <typeparam name="T">数据表</typeparam>
        /// <param name="setParam">set 参数</param>
        /// <param name="whereParam">where参数</param>
        /// <returns></returns>
        Task<int> Update<T>(object setParam, object whereParam) where T : class;

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="table">数据表</param>
        /// <param name="setParam">set 参数</param>
        /// <param name="whereSql">where sql</param>
        /// <param name="whereParam">where参数</param>
        /// <returns></returns>
        Task<int> Update(string table, object setParam, string whereSql, object whereParam);
        /// <summary>
        /// 更新数据
        /// </summary>
        /// <typeparam name="T">数据表</typeparam>
        /// <param name="setParam">set 参数</param>
        /// <param name="whereSql">where sql</param>
        /// <param name="whereParam">where参数</param>
        /// <returns></returns>
        Task<int> Update<T>(object setParam, string whereSql, object whereParam) where T : class;

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="table">数据表</param>
        /// <param name="setSql">set sql</param>
        /// <param name="setParam">set 参数</param>
        /// <param name="whereSql">where sql</param>
        /// <param name="whereParam">where参数</param>
        /// <returns></returns>
        Task<int> Update(string table, string setSql, object setParam, string whereSql, object whereParam);

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <typeparam name="T">数据表</typeparam>
        /// <param name="setSql">set sql</param>
        /// <param name="setParam">set 参数</param>
        /// <param name="whereSql">where sql</param>
        /// <param name="whereParam">where参数</param>
        /// <returns></returns>
        Task<int> Update<T>(string setSql, object setParam, string whereSql, object whereParam) where T : class;

        #endregion

        #region delete

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="table">数据表</param>
        /// <param name="whereParam">where参数</param>
        /// <returns></returns>
        Task<int> Delete(string table, object whereParam);

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <typeparam name="T">数据表</typeparam>
        /// <param name="whereParam">where参数</param>
        /// <returns></returns>
        Task<int> Delete<T>(object whereParam) where T : class;

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="table">数据表</param>
        /// <param name="whereSql">where sql</param>
        /// <param name="whereParam">where参数</param>
        /// <returns></returns>
        Task<int> Delete(string table, string whereSql, object whereParam);

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <typeparam name="T">数据表</typeparam>
        /// <param name="whereSql">where sql</param>
        /// <param name="whereParam">where参数</param>
        /// <returns></returns>
        Task<int> Delete<T>(string whereSql, object whereParam) where T : class;

        #endregion

        /// <summary>
        /// 更新表结构，不存在创建，存在添加不存在列
        /// </summary>
        /// <param name="modelTypeList"></param>
        bool CreateOrAlterTable(List<Type> modelTypeList);

        /// <summary>
        /// 添加匹配符%
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        string GetLikeValue(string value, DbLikeType type = DbLikeType.All);

        /// <summary>
        /// 获取最终排序, order by id, name
        /// </summary>
        /// <typeparam name="T">排序model</typeparam>
        /// <param name="orderby">排序: id desc, name asc</param>
        /// <param name="defaultOrderby">默认排序: id desc</param>
        /// <param name="tb"></param>
        /// <returns></returns>
        string GetOrderby<T>(string orderby, string defaultOrderby, string tb = null) where T : class;

    }
}
