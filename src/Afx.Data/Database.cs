using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using Afx.Data.Schema;
using System.Threading.Tasks;

namespace Afx.Data
{
    /// <summary>
    /// 数据库访问基类
    /// </summary>
    public abstract partial class Database : IDatabase
    {
        const string ANONYMOUS_TYPE_NAME = "<>f__AnonymousType";

        private volatile int tran_version = 0;
        private IDbTransaction transaction = null;
        private IDbConnection connection;
        private bool isOwnsConnection = true;
        private volatile bool isOpenKeep = false;
        private List<Action<IDatabase>> commitCallbackList;
        private List<Func<IDatabase, Task>> commitCallbackAsyncList;

        /// <summary>
        /// 资源是否释放
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionString
        {
            get
            {
                return this.Connection != null ? this.Connection.ConnectionString : null;
            }
        }

        /// <summary>
        /// 是否开启保持连接
        /// </summary>
        public bool IsOpenKeepConnection { get { return this.isOpenKeep; } }

        /// <summary>
        /// DB 提供程序Factory
        /// </summary>
        public DbProviderFactory ProviderFactory { get; private set; }


        /// <summary>
        /// （以秒为单位）
        /// </summary>
        public int? CommandTimeout { get; set; }

        /// <summary>
        /// IDbConnection
        /// </summary>
        public IDbConnection Connection { get { return this.connection; } }

        /// <summary>
        /// 执行sql logs
        /// </summary>
        public Action<string> Log { get; set; }
        private bool isLog { get { return this.Log != null; } }
        /// <summary>
        /// 执行sql logs
        /// </summary>
        protected virtual void OnLog(string sql)
        {
            try { this.Log(sql ?? ""); }
            catch { }
        }

        /// <summary>
        /// 执行CommitCallback action 错误
        /// </summary>
        public Action<Exception> CommitCallbackError { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="isOwnsConnection"></param>
        protected Database(IDbConnection connection, bool isOwnsConnection = true)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            this.connection = connection;
            var t = this.connection.GetType();
            var p = t.GetProperty("DbProviderFactory", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            this.ProviderFactory = p.GetValue(this.connection, null) as System.Data.Common.DbProviderFactory;
            this.isOwnsConnection = isOwnsConnection;
            this.IsDisposed = false;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="providerFactory"></param>
        protected Database(string connectionString, DbProviderFactory providerFactory)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException("ConnectionString");
            if (providerFactory == null) throw new ArgumentNullException("providerFactory");
            this.ProviderFactory = providerFactory;
            this.connection = this.CreateConnection();
            this.connection.ConnectionString = connectionString;
            this.IsDisposed = false;
        }

        /// <summary>
        ///  commit or SaveChanges 成功之后执行action list
        /// </summary>
        public List<Action<IDatabase>> CommitCallbackList { get { return this.commitCallbackList ?? new List<Action<IDatabase>>(0); } }

        /// <summary>
        /// 添加 commit or SaveChanges 成功之后执行action
        /// action 只执行一次
        /// </summary>
        /// <param name="action">需要执行的action</param>
        /// <returns>添加成功，返回所在的位置</returns>
        public virtual int AddCommitCallback(Action<IDatabase> action)
        {
            if (action == null) throw new ArgumentNullException("action");
            int index = -1;
            if (this.commitCallbackList == null) this.commitCallbackList = new List<Action<IDatabase>>(5);
            else index = this.commitCallbackList.IndexOf(action);

            if (index < 0)
            {
                this.commitCallbackList.Add(action);
                index = this.commitCallbackList.Count - 1;
            }

            return index;
        }

        /// <summary>
        /// 移除commit or SaveChanges 成功之后执行action
        /// </summary>
        /// <param name="action">需要执行的action</param>
        /// <returns>移除成功返回true</returns>
        public virtual bool RemoveCommitCallback(Action<IDatabase> action)
        {
            bool result = false;
            if (action == null) throw new ArgumentNullException("action");
            if (this.commitCallbackList == null) return true;
            result = this.commitCallbackList.Remove(action);

            return result;
        }

        /// <summary>
        /// 移除所有action
        /// </summary>
        public virtual void ClearCommitCallback()
        {
            if (this.commitCallbackList != null) this.commitCallbackList.Clear();
            if (this.commitCallbackAsyncList != null) this.commitCallbackAsyncList.Clear();
        }

        /// <summary>
        ///  commit or SaveChanges 成功之后执行action list
        /// </summary>
        public List<Func<IDatabase, Task>> CommitCallbackAsyncList { get { return this.commitCallbackAsyncList ?? new List<Func<IDatabase, Task>>(0); } }

        /// <summary>
        /// 添加 commit or SaveChanges 成功之后执行action
        /// action 只执行一次
        /// </summary>
        /// <param name="action">需要执行的action</param>
        /// <returns>添加成功，返回所在的位置</returns>
        public virtual int AddCommitCallback(Func<IDatabase, Task> action)
        {
            if (action == null) throw new ArgumentNullException("action");
            int index = -1;
            if (this.commitCallbackAsyncList == null) this.commitCallbackAsyncList = new List<Func<IDatabase, Task>>(5);
            else index = this.commitCallbackAsyncList.IndexOf(action);

            if (index < 0)
            {
                this.commitCallbackAsyncList.Add(action);
                index = this.commitCallbackAsyncList.Count - 1;
            }

            return index;
        }

        /// <summary>
        /// 移除commit or SaveChanges 成功之后执行action
        /// </summary>
        /// <param name="action">需要执行的action</param>
        /// <returns>移除成功返回true</returns>
        public virtual bool RemoveCommitCallback(Func<IDatabase, Task> action)
        {
            bool result = false;
            if (action == null) throw new ArgumentNullException("action");
            if (this.commitCallbackAsyncList == null) return true;
            result = this.commitCallbackAsyncList.Remove(action);

            return result;
        }

        private async Task OnCommitCallback()
        {
            if (this.commitCallbackList != null)
            {
                foreach (var action in this.commitCallbackList)
                {
                    try { action(this); }
                    catch (Exception ex)
                    {
                        CommitCallbackError?.Invoke(ex);
                    }
                }
            }

            if (this.commitCallbackAsyncList != null)
            {
                foreach (var action in this.commitCallbackAsyncList)
                {
                    try { await action(this); }
                    catch (Exception ex)
                    {
                        CommitCallbackError?.Invoke(ex);
                    }
                }
            }

            this.ClearCommitCallback();
        }

        /// <summary>
        /// 保持连接一直打开
        /// </summary>
        public virtual void OpenKeepConnection()
        {
            if (!this.isOpenKeep) this.isOpenKeep = true;
        }
        /// <summary>
        /// 关闭连接一直打开
        /// </summary>
        public virtual void CloseKeepConnection()
        {
            if (this.isOpenKeep)
            {
                this.isOpenKeep = false;
                this.Close();
            }
        }


        private IDisposable Open()
        {
            if (ConnectionState.Open != this.Connection.State)
            {
                this.Connection.Open();
                if (this.isLog) this.OnLog("-- Connection Open");
            }

            return new CloseConnection(this);
        }

        private void Close()
        {
            if (this.connection != null
                && !this.isOpenKeep
                && !this.IsTransaction
                && this.connection.State == ConnectionState.Open)
            {
                this.connection.Close();
                if (this.isLog) this.OnLog("-- Connection Close");
            }
        }

        #region
        /// <summary>
        /// 参数化查询名称加前缀
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public abstract string EncodeParameterName(string name);

        /// <summary>
        /// 列名转义
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public abstract string EncodeColumn(string column);

        /// <summary>
        /// 创建全新 DbConnection
        /// </summary>
        /// <returns>DbConnection</returns>
        public virtual DbConnection CreateConnection()
        {
            var result = this.ProviderFactory.CreateConnection();
            if (result == null) throw new InvalidConstraintException("ProviderFactory.CreateConnection is null.");

            return result;
        }

        /// <summary>
        /// 创建全新 DbCommand
        /// </summary>
        /// <returns>DbCommand</returns>
        public virtual DbCommand CreateCommand()
        {
            var result = this.ProviderFactory.CreateCommand();
            if (result == null) throw new InvalidConstraintException("ProviderFactory.CreateCommand is null.");

            return result;
        }

        /// <summary>
        /// 创建全新 DbParameter
        /// </summary>
        /// <returns>DbParameter</returns>
        public virtual DbParameter CreateParameter()
        {
            var parameter = this.ProviderFactory.CreateParameter();
            if (parameter == null) throw new InvalidConstraintException("ProviderFactory.CreateParameter is null.");
            return parameter;
        }

        /// <summary>
        /// 创建全新 DbParameter
        /// </summary>
        /// <param name="name">ParameterName</param>
        /// <param name="value">Value</param>
        /// <returns>DbParameter</returns>
        public virtual DbParameter CreateParameter(string name, object value)
        {
            var parameter = this.ProviderFactory.CreateParameter();
            if (parameter == null) throw new InvalidConstraintException("ProviderFactory.CreateParameter is null.");
            parameter.ParameterName = name;
            parameter.Value = value;
            return parameter;
        }

        #endregion

        #region 事务
        /// <summary>
        /// 是否开启事务
        /// </summary>
        public bool IsTransaction
        {
            get { return null != this.transaction; }
        }

        /// <summary>
        /// 开启事务
        /// </summary>
        public IDisposable BeginTransaction()
        {
            if (null != this.transaction) throw new InvalidOperationException("事务已开启，不能重复开启！");
            if (this.connection.State != ConnectionState.Open) this.Open();
            this.transaction = this.Connection.BeginTransaction();
            this.tran_version++;
            if (this.isLog) this.OnLog("-- BeginTransaction");

            return new TranRollback(this, this.tran_version);
        }

        /// <summary>
        /// 开启事务
        /// </summary>
        /// <param name="isolationLevel">事务级别</param>
        public IDisposable BeginTransaction(IsolationLevel isolationLevel)
        {
            if (null != this.transaction) throw new InvalidOperationException("已开启事务，不能重复开启！");
            if (this.connection.State != ConnectionState.Open) this.Open();
            this.transaction = this.Connection.BeginTransaction(isolationLevel);
            this.tran_version++;
            if (this.isLog) this.OnLog("-- BeginTransaction " + isolationLevel.ToString());

            return new TranRollback(this, this.tran_version);
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public async Task Commit()
        {
            if (null == this.transaction) throw new InvalidOperationException("未开启事务，不能提交！");
            this.transaction.Commit();
            this.transaction.Dispose();
            this.transaction = null;
            if (this.isLog) this.OnLog("-- Commit");
            this.Close();
            await this.OnCommitCallback();
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public void Rollback()
        {
            if (null != this.transaction)
            {
                this.transaction.Rollback();
                this.transaction.Dispose();
                this.transaction = null;
                if (this.isLog) this.OnLog("-- Rollback");
                this.Close();
                this.ClearCommitCallback();
            }
        }
        #endregion

        private void CheckSql(string sql)
        {
            if (string.IsNullOrEmpty(sql)) throw new ArgumentNullException("sql", "sql is null!");
        }

        private void AddParam(IDbCommand command, string sql, object parameters)
        {
            if (parameters != null && (parameters is IEnumerable<DbParameter> || parameters is IEnumerable<IDataParameter>))
            {
                if (parameters is IEnumerable<DbParameter>)
                {
                    var ps = parameters as IEnumerable<DbParameter>;
                    foreach (var p in ps) command.Parameters.Add(p);
                }
                else
                {
                    var ps = parameters as IEnumerable<IDataParameter>;
                    foreach (var p in ps) command.Parameters.Add(p);
                }
                command.CommandText = sql;
            }
            else if (parameters != null && parameters is IEnumerable<KeyValuePair<string, object>>)
            {
                IModelToParam toparam = new DicToParam();
                toparam.To(this, command, sql, parameters);
            }
            else if (parameters != null)
            {
                IModelToParam toparam = new ModelToParam();
                toparam.To(this, command, sql, parameters);
            }
            else
            {
                command.CommandText = sql;
            }
        }

        private object ChangeType(object o, Type t)
        {
            if (o != null && o != DBNull.Value)
            {
                if (t == typeof(object))
                {
                    return o;
                }

                if (t == typeof(string))
                {
                    return o != null ? o.ToString() : null;
                }

                var ot = o.GetType();
                if (ot == t)
                {
                    return o;
                }

                MethodInfo methodInfo;
                if (ModelMaping.convertDic.TryGetValue(t, out methodInfo))
                {
                    return methodInfo.Invoke(null, new object[] { o });
                }

                var gt = t;
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var _gt = t.GetGenericArguments()[0];
                    if (_gt == ot) return o;
                    if (gt.IsPrimitive && gt.IsValueType) gt = _gt;
                }

                if (gt.IsPrimitive && gt.IsValueType)
                {
                    return Convert.ChangeType(o, gt);
                }
                else
                {
                    throw new InvalidCastException("ChangeType " + ot.FullName + " to " + t.FullName + " error!");
                }
            }

            return null;
        }

        private void WriteLog(IDbCommand command)
        {
            if (this.isLog)
            {
                this.OnLog(command.CommandText);
                this.OnLog("-- CommandType = " + command.CommandType.ToString());
                if (this.CommandTimeout.HasValue) this.OnLog("-- CommandTimeout = " + this.CommandTimeout.Value.ToString());
                if (command.Parameters.Count > 0)
                {
                    foreach (IDataParameter p in command.Parameters)
                    {
                        this.OnLog(string.Format("-- {0} = {1}", p.ParameterName, p.Value == DBNull.Value ? "null" : p.Value.ToString()));
                    }
                }
            }
        }

        private bool CheckModel(Type t)
        {
            bool result = false;
            if (t.IsArray || t.IsAbstract || !(t.IsClass || t.IsValueType))
                throw new ArgumentException(t.FullName + " is error!");
            if (t.IsGenericType)
            {
                var gt = t.GetGenericTypeDefinition();
                if (gt != typeof(Nullable<>))
                {
                    throw new ArgumentException(t.FullName + " is error!");
                }

                result = true;
            }

            return result;
        }

        #region common
        /// <summary>
        /// 查询数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="param">sql参数，model or dictionary string object or IEnumerable&lt;DbParameter&gt; or IEnumerable&lt;IDataParameter&gt; </param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public virtual List<T> Query<T>(string sql, object param = null, CommandType? commandType = null)
        {
            this.CheckSql(sql);
            var t = typeof(T);
            bool isvalue = CheckModel(t);
            ReaderToModel ic = null;
            if (!isvalue && (t == typeof(string) || t.IsPrimitive && t.IsValueType))
            {
                isvalue = true;
            }
            if (!isvalue && t.IsClass)
            {
                ic = ModelMaping.GetReaderToModel(t);
            }

            using (this.Open())
            {
                using (IDbCommand command = this.Connection.CreateCommand())
                {
                    command.Connection = this.Connection;
                    if (this.CommandTimeout.HasValue) command.CommandTimeout = this.CommandTimeout.Value;
                    if (commandType.HasValue) command.CommandType = commandType.Value;
                    if (this.transaction != null) command.Transaction = this.transaction;
                    AddParam(command, sql, param);
                    WriteLog(command);
                    using (var reader = command.ExecuteReader())
                    {
                        var list = new List<T>();
                        while (reader.Read())
                        {
                            object o = default(T);
                            if (ic != null)
                            {
                                o = ic.To(reader);
                            }
                            else
                            {
                                var v = reader.GetValue(0);
                                o = ChangeType(v, t) ?? default(T);
                            }
                            list.Add((T)o);
                        }
                        return list;
                    }
                }
            }
        }
        /// <summary>
        /// 执行sql，返回影响行数
        /// </summary>
        /// <param name="sql">sql</param>
        /// <param name="param">sql参数，model or dictionary string object or IEnumerable&lt;DbParameter&gt; or IEnumerable&lt;IDataParameter&gt;</param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public virtual async Task<int> ExecuteNonQuery(string sql, object param = null, CommandType? commandType = null)
        {
            this.CheckSql(sql);
            using (this.Open())
            {
                using (IDbCommand command = this.Connection.CreateCommand())
                {
                    command.Connection = this.Connection;
                    if (this.CommandTimeout.HasValue) command.CommandTimeout = this.CommandTimeout.Value;
                    if (commandType.HasValue) command.CommandType = commandType.Value;
                    if (this.transaction != null) command.Transaction = this.transaction;
                    AddParam(command, sql, param);
                    WriteLog(command);
                    var num = command.ExecuteNonQuery();
                    if (!this.IsTransaction)
                    {
                        await this.OnCommitCallback();
                    }

                    return num;
                }
            }
        }

        /// <summary>
        /// 执行sql，返回第一行的第一列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="param">sql参数，model or dictionary string object or IEnumerable&lt;DbParameter&gt; or IEnumerable&lt;IDataParameter&gt;</param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public virtual T ExecuteScalar<T>(string sql, object param = null, CommandType? commandType = null)
        {
            this.CheckSql(sql);
            T m = default(T);
            var t = typeof(T);
            if (t.IsGenericType)
            {
                if (t.GetGenericTypeDefinition() == typeof(Nullable<>))
                    t = t.GetGenericArguments()[0];
                else throw new ArgumentException(t.FullName + " is error!");
            }
            using (this.Open())
            {
                using (IDbCommand command = this.Connection.CreateCommand())
                {
                    command.Connection = this.Connection;
                    if (this.CommandTimeout.HasValue) command.CommandTimeout = this.CommandTimeout.Value;
                    if (commandType.HasValue) command.CommandType = commandType.Value;
                    if (this.transaction != null) command.Transaction = this.transaction;
                    AddParam(command, sql, param);
                    WriteLog(command);
                    object o = command.ExecuteScalar();
                    object v = ChangeType(o, t);
                    if (v != null) m = (T)v;

                    return m;
                }
            }
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected abstract IDatabaseSchema GetDatabaseSchema();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected abstract ITableSchema GeTableSchema();

        /// <summary>
        /// 更新表结构，不存在创建，存在添加不存在列
        /// </summary>
        /// <param name="modelTypeList"></param>
        public virtual bool CreateOrAlterTable(List<Type> modelTypeList)
        {
            if (modelTypeList == null || modelTypeList.Count == 0) return false;
            using (var databaseSchema = GetDatabaseSchema())
            {
                if (databaseSchema == null) return false;
                using (var tableSchema = GeTableSchema())
                {
                    if (tableSchema == null) return false;
                  if(!databaseSchema.Exist()) databaseSchema.CreateDatabase();
                    List<string> tables = tableSchema.GetTables();
                    foreach (var t in modelTypeList)
                    {
                        if (t == null || !t.IsClass || t.IsAbstract) continue;
                        var tb = tableSchema.GetTableName(t);
                        if (!tables.Exists(q => string.Equals(q, tb, StringComparison.OrdinalIgnoreCase)))
                        {
                            var columns = tableSchema.GetColumns(t, tb);
                            tableSchema.CreateTable(tb, columns);
                        }
                        else
                        {
                            var tableColumns = tableSchema.GetTableColumns(tb);
                            var modelColumns = tableSchema.GetColumns(t, tb);
                            List<IndexModel> addIndexs = new List<IndexModel>();
                            foreach (var mColumn in modelColumns)
                            {
                                var tColumn = tableColumns.Find(q => q.Name.Equals(mColumn.Name, StringComparison.OrdinalIgnoreCase));
                                if (tColumn == null)
                                {
                                    tableSchema.AddColumn(tb, mColumn);
                                    if (mColumn.Indexs != null && mColumn.Indexs.Count > 0)
                                    {
                                        foreach (var index in mColumn.Indexs)
                                        {
                                            if (string.IsNullOrEmpty(index.Name)) continue;
                                            if (string.IsNullOrEmpty(index.ColumnName)) index.ColumnName = mColumn.Name;
                                            addIndexs.Add(index);
                                        }
                                    }
                                }
                                else if (mColumn.Indexs != null && mColumn.Indexs.Count > 0)
                                {
                                    // 添加数据库不存在索引
                                    foreach (var index in mColumn.Indexs)
                                    {
                                        if (string.IsNullOrEmpty(index.Name)) continue;
                                        if (string.IsNullOrEmpty(index.ColumnName)) index.ColumnName = mColumn.Name;

                                        if (tColumn.Indexs == null || tColumn.Indexs.Count == 0)
                                        {
                                            addIndexs.Add(index);
                                        }
                                        else
                                        {
                                            var tindex = tColumn.Indexs.Find(q => index.Name.Equals(q.Name, StringComparison.OrdinalIgnoreCase));
                                            if (tindex == null || tindex.IsUnique != index.IsUnique)
                                            {
                                                addIndexs.Add(index);
                                            }
                                        }
                                    }
                                }
                            }

                            if (addIndexs.Count > 0) tableSchema.AddIndex(tb, addIndexs);
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            this.isOpenKeep = false;
            this.Rollback();
            this.Close();
            if (disposing)
            {
                if (this.connection != null && isOwnsConnection) this.connection.Dispose();
                this.connection = null;
                this.ProviderFactory = null;
                this.Log = null;
                if(this.commitCallbackList != null) this.commitCallbackList.Clear();
                this.commitCallbackList = null;
                if (this.commitCallbackAsyncList != null) this.commitCallbackAsyncList.Clear();
                this.commitCallbackAsyncList = null;
                this.Log = null;
                this.CommitCallbackError = null;

                this.IsDisposed = true;
            }
        }

        class CloseConnection : IDisposable
        {
            private Database db;
            public CloseConnection(Database db)
            {
                this.db = db;
            }
            /// <summary>
            /// 
            /// </summary>
            public void Dispose()
            {
                if (this.db != null)
                {
                    this.db.Close();
                    this.db = null;
                }
            }
        }

        class TranRollback : IDisposable
        {
            private int tran_ver = 0;
            private Database db;
            public TranRollback(Database db, int tran_ver)
            {
                this.db = db;
                this.tran_ver = tran_ver;
            }
            /// <summary>
            /// 
            /// </summary>
            public void Dispose()
            {
                if(this.db != null && this.db.transaction != null && this.db.tran_version == this.tran_ver)
                {
                    this.db.Rollback();
                }
                this.db = null;
            }
        }
    }
}
