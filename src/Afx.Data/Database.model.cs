using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;
using System.Reflection.Emit;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Afx.Data
{
    public abstract partial class Database
    {
        private bool CheckParamType(Type paramType)
        {
            //匿名对象
            if (paramType.IsClass && paramType.Name.StartsWith(ANONYMOUS_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
                return true;

            //非class 、泛型、数组
            if (!paramType.IsClass || paramType.IsGenericType || paramType.IsArray)
                return false;

            return true;
        }

        #region Get
        /// <summary>
        /// 获取表查询sql
        /// </summary>
        /// <typeparam name="T">数据库表对应model</typeparam>
        /// <param name="whereDic"></param>
        /// <returns></returns>
        protected virtual string GetSelectSqlByDic<T>(Dictionary<string, object> whereDic) where T : class
        {
            var t = typeof(T);
            var tps = t.GetProperties();
            if (tps == null || tps.Length == 0) throw new ArgumentException($"T({t.FullName}) is error!");

            StringBuilder sql = new StringBuilder();
            sql.Append("SELECT");
            foreach (var p in tps)
            {
                sql.Append($" {this.EncodeColumn(p.Name)},");
            }
            sql.Remove(sql.Length - 1, 1);
            sql.Append($" FROM {this.EncodeColumn(t.Name)}");

            if (whereDic != null && whereDic.Count > 0)
            {
                sql.Append(" WHERE");
                foreach (KeyValuePair<string, object> kv in whereDic)
                {
                    if (kv.Value == null || kv.Value == DBNull.Value)
                        sql.Append($" {this.EncodeColumn(kv.Key)} IS NULL AND");
                    else
                        sql.Append($" {this.EncodeColumn(kv.Key)} = @{kv.Key} AND");
                }
                sql.Remove(sql.Length - 4, 4);
            }

            return sql.ToString();
        }

        /// <summary>
        /// 获取表查询sql
        /// </summary>
        /// <typeparam name="T">数据库表对应model</typeparam>
        /// <param name="whereParam">new { id=10, name= "1"}</param>
        /// <returns></returns>
        protected virtual string GetSelectSql<T>(object whereParam) where T : class
        {
            if (whereParam == null) return this.GetSelectSqlByDic<T>(null);
            if (whereParam is Dictionary<string, object>)
            {
                return this.GetSelectSqlByDic<T>(whereParam as Dictionary<string, object>);
            }

            var wpt = whereParam.GetType();
            if (!this.CheckParamType(wpt)) throw new ArgumentException($"{nameof(whereParam)}({wpt.FullName}) is error!");

            StringBuilder sql = new StringBuilder();
            sql.Append(GetSelectSqlByDic<T>(null));
            var wpps = wpt.GetProperties();
            if (wpps != null && wpps.Length > 0)
            {
                sql.Append(" WHERE");
                foreach (var p in wpps)
                {
                    var o = p.GetValue(whereParam, null);
                    if (o == null || o == DBNull.Value)
                        sql.Append($" {this.EncodeColumn(p.Name)} IS NULL AND");
                    else
                        sql.Append($" {this.EncodeColumn(p.Name)} = @{p.Name} AND");
                }
                sql.Remove(sql.Length - 4, 4);
            }

            return sql.ToString();
        }

        /// <summary>
        /// 查询表数据
        /// </summary>
        /// <typeparam name="T">数据库表对应model</typeparam>
        /// <param name="whereParam">new { id=10, name= "1"}</param>
        /// <returns></returns>
        public virtual List<T> GetList<T>(object whereParam = null) where T : class, new()
        {
            string sql = this.GetSelectSql<T>(whereParam);
            var list = this.Query<T>(sql, whereParam);

            return list;
        }

        /// <summary>
        /// 查询表数据
        /// </summary>
        /// <typeparam name="T">数据库表对应model</typeparam>
        /// <param name="whereSql">whereSql: id = @id OR name = @name </param>
        /// <param name="whereParam">new { id=10, name= "1"}</param>
        /// <returns></returns>
        public virtual List<T> GetList<T>(string whereSql, object whereParam = null) where T : class, new()
        {
            string sql = this.GetSelectSqlByDic<T>(null);
            if (!string.IsNullOrEmpty(whereSql)) sql = $"{sql} WHERE {whereSql}";
            var list = this.Query<T>(sql, whereParam);

            return list;
        }

        /// <summary>
        /// 查询表一行数据
        /// </summary>
        /// <typeparam name="T">数据库表对应model</typeparam>
        /// <param name="whereParam">不能为空， new { id=10, name= "1"}</param>
        /// <returns></returns>
        public virtual T Get<T>(object whereParam) where T : class, new()
        {
            if (whereParam == null) throw new ArgumentNullException(nameof(whereParam));
            var list = this.GetList<T>(whereParam);

            return list.Count > 0 ? list[0] : default(T);
        }

        /// <summary>
        /// 查询表一行数据
        /// </summary>
        /// <typeparam name="T">数据库表对应model</typeparam>
        /// <param name="whereSql">不能为空，whereSql: id = @id OR name </param>
        /// <param name="whereParam">new { id=10, name= "1"}</param>
        /// <returns></returns>
        public virtual T Get<T>(string whereSql, object whereParam = null) where T : class, new()
        {
            if (string.IsNullOrEmpty(whereSql)) throw new ArgumentNullException(nameof(whereSql));
            var list = this.GetList<T>(whereSql, whereParam);

            return list.Count > 0 ? list[0] : default(T);
        }
        #endregion

        #region add
        /// <summary>
        /// GetInsertSqlByDic
        /// </summary>
        /// <param name="table"></param>
        /// <param name="dic"></param>
        /// <returns></returns>
        protected virtual string GetInsertSqlByDic(string table, Dictionary<string, object> dic)
        {
            if (string.IsNullOrEmpty(table)) throw new ArgumentNullException(nameof(table));
            if (dic == null || dic.Count == 0) throw new ArgumentNullException(nameof(dic));

            StringBuilder sql = new StringBuilder();
            sql.Append($"INSERT INTO {this.EncodeColumn(table)}(");
            StringBuilder paramSql = new StringBuilder();
            paramSql.Append(" VALUES(");
            foreach (KeyValuePair<string, object> kv in dic)
            {
                sql.Append($"{this.EncodeColumn(kv.Key)},");
                paramSql.Append($"@{kv.Key},");
            }

            sql.Remove(sql.Length - 1, 1);
            paramSql.Remove(paramSql.Length - 1, 1);

            sql.Append(")");
            paramSql.Append(")");

            return sql.ToString() + paramSql.ToString();
        }
        /// <summary>
        /// GetInsertSql
        /// </summary>
        /// <param name="table"></param>
        /// <param name="propertyInfos"></param>
        /// <returns></returns>
        protected virtual string GetInsertSql(string table, List<PropertyInfo> propertyInfos)
        {
            if (string.IsNullOrEmpty(table)) throw new ArgumentNullException(nameof(table));
            if (propertyInfos == null || propertyInfos.Count == 0) throw new ArgumentNullException(nameof(propertyInfos));

            StringBuilder sql = new StringBuilder();
            sql.Append($"INSERT INTO {this.EncodeColumn(table)}(");
            StringBuilder paramSql = new StringBuilder();
            paramSql.Append(" VALUES(");
            foreach (var p in propertyInfos)
            {
                sql.Append($"{this.EncodeColumn(p.Name)},");
                paramSql.Append($"@{p.Name},");
            }

            sql.Remove(sql.Length - 1, 1);
            paramSql.Remove(paramSql.Length - 1, 1);

            sql.Append(")");
            paramSql.Append(")");

            return sql.ToString() + paramSql.ToString();
        }
        /// <summary>
        /// 添加数据
        /// </summary>
        /// <typeparam name="T">插入表</typeparam>
        /// <param name="m">参数</param>
        /// <param name="ignore">忽略插入列</param>
        /// <returns></returns>
        public virtual async Task<int> Add<T>(T m, params string[] ignore) where T : class
        {
            if (m == null) throw new ArgumentNullException(nameof(m));
            var t = typeof(T);
            if (t.IsGenericType || t.IsArray) throw new ArgumentException($"T({t.FullName}) is error!");
            var ps = new List<PropertyInfo>(t.GetProperties());
            if(ignore != null && ignore.Length > 0)
            {
                var wlist = new List<string>(ignore);
                ps = ps.FindAll(q => !wlist.Exists(a => a.Equals(q.Name, StringComparison.OrdinalIgnoreCase)));
            }
            if (ps.Count == 0) throw new ArgumentException($"T({t.FullName}) is error!");

            var sql = this.GetInsertSql(t.Name, ps);
            int count = await this.ExecuteNonQuery(sql, m);
            return count;
        }
        /// <summary>
        /// 添加数据
        /// </summary>
        /// <param name="table">插入表</param>
        /// <param name="param"></param>
        /// <returns></returns>
        public virtual async Task<int> Add(string table, object param)
        {
            if (string.IsNullOrEmpty(table)) throw new ArgumentNullException(nameof(table));
            if (param == null) throw new ArgumentNullException(nameof(param));
            string sql = null;
            if (param is Dictionary<string, object>)
            {
                var dic = param as Dictionary<string, object>;
                if (dic.Count == 0) throw new ArgumentException($"{nameof(param)}(count == 0) is error!");
                sql = this.GetInsertSqlByDic(table, dic);
            }
            else
            {
                var pt = param.GetType();
                if (!CheckParamType(pt)) throw new ArgumentException($"{nameof(param)}({pt.FullName}) is error!");
                var pps = new List<PropertyInfo>(pt.GetProperties());
                if (pps.Count == 0) throw new ArgumentException($"{nameof(param)} is error!");
                sql = this.GetInsertSql(table, pps);
            }

            int count = await this.ExecuteNonQuery(sql, param);
            return count;
        }

        #endregion

        #region update
        /// <summary>
        /// GetUpdateSetSqlByDic
        /// </summary>
        /// <param name="table"></param>
        /// <param name="dic"></param>
        /// <param name="allParamDic"></param>
        /// <returns></returns>
        protected virtual string GetUpdateSetSqlByDic(string table, Dictionary<string, object> dic, ref Dictionary<string, object> allParamDic)
        {
            if (string.IsNullOrEmpty(table)) throw new ArgumentNullException(nameof(table));
            if (dic == null || dic.Count == 0) throw new ArgumentNullException(nameof(dic));
            var sql = new StringBuilder();
            sql.Append($"UPDATE {this.EncodeColumn(table)} SET");
            if (allParamDic == null) allParamDic = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> kv in dic)
            {
                var pname = kv.Key;
                int i = 0;
                while (allParamDic.ContainsKey(pname)) pname = i == 0 ? $"{kv.Key}_set" : $"{kv.Key}_set_{++i}";
                sql.Append($" {this.EncodeColumn(kv.Key)} = @{pname},");
                allParamDic.Add(pname, kv.Value);
            }
            sql.Remove(sql.Length - 1, 1);

            return sql.ToString();
        }
        /// <summary>
        /// GetUpdateSetSql
        /// </summary>
        /// <param name="table"></param>
        /// <param name="setParam"></param>
        /// <param name="allParamDic"></param>
        /// <returns></returns>
        protected virtual string GetUpdateSetSql(string table, object setParam, ref Dictionary<string, object> allParamDic)
        {
            if (string.IsNullOrEmpty(table)) throw new ArgumentNullException(nameof(table));
            if (setParam == null) throw new ArgumentNullException(nameof(setParam));

            if (setParam is Dictionary<string, object>)
            {
                return this.GetUpdateSetSqlByDic(table, setParam as Dictionary<string, object>, ref allParamDic);
            }

            var spt = setParam.GetType();
            if (!CheckParamType(spt)) throw new ArgumentException($"{nameof(setParam)}({spt.FullName}) is error!");
            var spps = spt.GetProperties();
            if (spps.Length == 0) throw new ArgumentException($"{nameof(setParam)} is error!");

            var sql = new StringBuilder();
            sql.Append($"UPDATE {this.EncodeColumn(table)} SET");
            if (allParamDic == null) allParamDic = new Dictionary<string, object>();
            foreach (var p in spps)
            {
                var pname = p.Name;
                int i = 0;
                while (allParamDic.ContainsKey(pname)) pname = i == 0 ? $"{p.Name}_set" : $"{p.Name}_set_{++i}";
                sql.Append($" {this.EncodeColumn(p.Name)} = @{pname},");
                allParamDic.Add(pname, p.GetValue(setParam, null));
            }
            sql.Remove(sql.Length - 1, 1);

            return sql.ToString();
        }
        /// <summary>
        /// GetUpdateWhereSqlByDic
        /// </summary>
        /// <param name="whereDic"></param>
        /// <param name="allParamDic"></param>
        /// <returns></returns>
        protected virtual string GetUpdateWhereSqlByDic(Dictionary<string, object> whereDic, ref Dictionary<string, object> allParamDic)
        {
            if (whereDic == null || whereDic.Count == 0) return string.Empty;
            var sql = new StringBuilder();
            sql.Append(" WHERE");
            if (allParamDic == null) allParamDic = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> kv in whereDic)
            {
                if (kv.Value == null || kv.Value == DBNull.Value)
                {
                    sql.Append($" {this.EncodeColumn(kv.Key)} IS NULL AND");
                }
                else
                {
                    var pname = kv.Key;
                    int i = 0;
                    while (allParamDic.ContainsKey(pname)) pname = i == 0 ? $"{kv.Key}_where" : $"{kv.Key}_where_{++i}";
                    sql.Append($" {this.EncodeColumn(kv.Key)} = @{pname} AND");
                    allParamDic.Add(pname, kv.Value);
                }
            }
            sql.Remove(sql.Length - 4, 4);

            return sql.ToString();
        }
        /// <summary>
        /// GetUpdateWhereSql
        /// </summary>
        /// <param name="whereParam"></param>
        /// <param name="allParamDic"></param>
        /// <returns></returns>
        protected virtual string GetUpdateWhereSql(object whereParam, ref Dictionary<string, object> allParamDic)
        {
            if (whereParam == null) return string.Empty;
            if (whereParam is Dictionary<string, object>)
            {
                return this.GetUpdateWhereSqlByDic(whereParam as Dictionary<string, object>, ref allParamDic);
            }

            var t = whereParam.GetType();
            if (!CheckParamType(t)) throw new ArgumentException($"{nameof(whereParam)}({t.FullName}) is error!");
            var ps = t.GetProperties();
            if (ps == null || ps.Length == 0) return string.Empty;

            var sql = new StringBuilder();
            sql.Append(" WHERE");
            if (allParamDic == null) allParamDic = new Dictionary<string, object>();
            foreach (var p in ps)
            {
                var v = p.GetValue(whereParam, null);
                if (v == null || v == DBNull.Value)
                {
                    sql.Append($" {this.EncodeColumn(p.Name)} IS NULL AND");
                }
                else
                {
                    var pname = p.Name;
                    int i = 0;
                    while (allParamDic.ContainsKey(pname)) pname = i == 0 ? $"{p.Name}_where" : $"{p.Name}_where_{++i}";
                    sql.Append($" {this.EncodeColumn(p.Name)} = @{pname} AND");
                    allParamDic.Add(pname, v);
                }
            }
            sql.Remove(sql.Length - 4, 4);

            return sql.ToString();
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="table">数据表</param>
        /// <param name="setParam">set 参数</param>
        /// <param name="whereParam">where参数</param>
        /// <returns></returns>
        public virtual async Task<int> Update(string table, object setParam, object whereParam)
        {
            if (string.IsNullOrEmpty(table)) throw new ArgumentNullException(nameof(table));
            if (setParam == null) throw new ArgumentNullException(nameof(setParam));
            Dictionary<string, object> allParamDic = null;
            string setSql = this.GetUpdateSetSql(table, setParam, ref allParamDic);

            string whereSql = this.GetUpdateWhereSql(whereParam, ref allParamDic);

            var sql = setSql + whereSql;

            int count = await this.ExecuteNonQuery(sql, allParamDic);
            return count;
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <typeparam name="T">数据表</typeparam>
        /// <param name="setParam">set 参数</param>
        /// <param name="whereParam">where参数</param>
        /// <returns></returns>
        public virtual async Task<int> Update<T>(object setParam, object whereParam) where T : class
        {
            return await this.Update(typeof(T).Name, setParam, whereParam);
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="table">数据表</param>
        /// <param name="setParam">set 参数</param>
        /// <param name="whereSql">where sql</param>
        /// <param name="whereParam">where参数</param>
        /// <returns></returns>
        public virtual async Task<int> Update(string table, object setParam, string whereSql, object whereParam)
        {
            if (string.IsNullOrEmpty(table)) throw new ArgumentNullException(nameof(table));
            if (setParam == null) throw new ArgumentNullException(nameof(setParam));
            Dictionary<string, object> allParamDic = null;
            string sql = null;
            if (setParam is Dictionary<string, object>)
            {
                var paramDic = setParam as Dictionary<string, object>;
                sql = this.GetUpdateSetSqlByDic(table, paramDic, ref allParamDic);
            }
            else
            {
                sql = this.GetUpdateSetSql(table, setParam, ref allParamDic);
            }

            if (whereParam != null)
            {
                if (whereParam is Dictionary<string, object>)
                {
                    var dic = whereParam as Dictionary<string, object>;
                    if (dic.Count > 0)
                    {
                        if (allParamDic == null) allParamDic = new Dictionary<string, object>();
                        foreach (KeyValuePair<string, object> kv in dic)
                        {
                            if (allParamDic.ContainsKey(kv.Key)) throw new ArgumentException($"setParam and whereParam key({kv.Key}) repeat!");
                            allParamDic.Add(kv.Key, kv.Value);
                        }
                    }
                }
                else
                {
                    var t = whereParam.GetType();
                    if (!CheckParamType(t)) throw new ArgumentException($"{nameof(whereParam)}({t.FullName}) is error!");
                    var ps = t.GetProperties();
                    if (ps != null && ps.Length > 0)
                    {
                        if (allParamDic == null) allParamDic = new Dictionary<string, object>();
                        foreach (var p in ps)
                        {
                            if (allParamDic.ContainsKey(p.Name)) throw new ArgumentException($"setParam and whereParam name({p.Name}) repeat!");
                            allParamDic.Add(p.Name, p.GetValue(whereParam, null));
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(whereSql)) sql = $"{sql} WHERE {whereSql}";

            int count = await this.ExecuteNonQuery(sql, allParamDic);

            return count;
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <typeparam name="T">数据表</typeparam>
        /// <param name="setParam">set 参数</param>
        /// <param name="whereSql">where sql</param>
        /// <param name="whereParam">where参数</param>
        /// <returns></returns>
        public virtual async Task<int> Update<T>(object setParam, string whereSql, object whereParam) where T : class
        {
            return await this.Update(typeof(T).Name, setParam, whereSql, whereParam);
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="table">数据表</param>
        /// <param name="setSql">set sql</param>
        /// <param name="setParam">set 参数</param>
        /// <param name="whereSql">where sql</param>
        /// <param name="whereParam">where参数</param>
        /// <returns></returns>
        public virtual async Task<int> Update(string table, string setSql, object setParam, string whereSql, object whereParam)
        {
            if (string.IsNullOrEmpty(table)) throw new ArgumentNullException(nameof(table));
            if (string.IsNullOrEmpty(setSql)) throw new ArgumentNullException(nameof(setSql));
            Dictionary<string, object> allParamDic = null;
            string sql = $"UPDATE {this.EncodeColumn(table)} SET {setSql}";
            if (setParam != null)
            {
                if (setParam is Dictionary<string, object>)
                {
                    var dic = setParam as Dictionary<string, object>;
                    if (dic.Count > 0)
                    {
                        if (allParamDic == null) allParamDic = new Dictionary<string, object>();
                        foreach (KeyValuePair<string, object> kv in dic)
                        {
                            if (allParamDic.ContainsKey(kv.Key)) throw new ArgumentException($"setParam key({kv.Key}) repeat!");
                            allParamDic.Add(kv.Key, kv.Value);
                        }
                    }
                }
                else
                {
                    var t = setParam.GetType();
                    if (!CheckParamType(t)) throw new ArgumentException($"{nameof(setParam)}({t.FullName}) is error!");
                    var ps = t.GetProperties();
                    if (ps != null && ps.Length > 0)
                    {
                        if (allParamDic == null) allParamDic = new Dictionary<string, object>();
                        foreach (var p in ps)
                        {
                            if (allParamDic.ContainsKey(p.Name)) throw new ArgumentException($"setParam name({p.Name}) repeat!");
                            allParamDic.Add(p.Name, p.GetValue(setParam, null));
                        }
                    }
                }
            }

            if (whereParam != null)
            {
                if (whereParam is Dictionary<string, object>)
                {
                    var dic = whereParam as Dictionary<string, object>;
                    if (dic.Count > 0)
                    {
                        if (allParamDic == null) allParamDic = new Dictionary<string, object>();
                        foreach (KeyValuePair<string, object> kv in dic)
                        {
                            if (allParamDic.ContainsKey(kv.Key)) throw new ArgumentException($"setParam and whereParam key({kv.Key}) repeat!");
                            allParamDic.Add(kv.Key, kv.Value);
                        }
                    }
                }
                else
                {
                    var t = whereParam.GetType();
                    if (!CheckParamType(t)) throw new ArgumentException($"{nameof(whereParam)}({t.FullName}) is error!");
                    var ps = t.GetProperties();
                    if (ps != null && ps.Length > 0)
                    {
                        if (allParamDic == null) allParamDic = new Dictionary<string, object>();
                        foreach (var p in ps)
                        {
                            if (allParamDic.ContainsKey(p.Name)) throw new ArgumentException($"setParam and whereParam name({p.Name}) repeat!");
                            allParamDic.Add(p.Name, p.GetValue(whereParam, null));
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(whereSql)) sql = $"{sql} WHERE {whereSql}";
            int count = await this.ExecuteNonQuery(sql, allParamDic);

            return count;
        }
        /// <summary>
        /// 更新数据
        /// </summary>
        /// <typeparam name="T">数据表</typeparam>
        /// <param name="setSql">set sql</param>
        /// <param name="setParam">set 参数</param>
        /// <param name="whereSql">where sql</param>
        /// <param name="whereParam">where参数</param>
        /// <returns></returns>
        public virtual async Task<int> Update<T>(string setSql, object setParam, string whereSql, object whereParam) where T : class
        {
            return await this.Update(typeof(T).Name, setSql, setParam, whereSql, whereParam);
        }

        #endregion

        #region delete

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="table">数据表</param>
        /// <param name="whereParam">where参数</param>
        /// <returns></returns>
        public virtual async Task<int> Delete(string table, object whereParam)
        {
            if (string.IsNullOrEmpty(table)) throw new ArgumentNullException(nameof(table));
            var sql = new StringBuilder();
            sql.Append($"DELETE FROM {table}");
            if (whereParam != null)
            {
                if (whereParam is Dictionary<string, object>)
                {
                    var dic = whereParam as Dictionary<string, object>;
                    if (dic.Count > 0)
                    {
                        sql.Append(" WHERE");
                        foreach (KeyValuePair<string, object> kv in dic)
                        {
                            if (kv.Value == null || kv.Value == DBNull.Value)
                                sql.Append($" {this.EncodeColumn(kv.Key)} IS NULL AND");
                            else
                                sql.Append($" {this.EncodeColumn(kv.Key)} = @{kv.Key} AND");
                        }
                        sql.Remove(sql.Length - 4, 4);
                    }
                }
                else
                {
                    var t = whereParam.GetType();
                    if (!CheckParamType(t)) throw new ArgumentException($"{nameof(whereParam)}({t.FullName}) is error!");
                    var ps = t.GetProperties();
                    if (ps != null && ps.Length > 0)
                    {
                        sql.Append(" WHERE");
                        foreach (var p in ps)
                        {
                            var v = p.GetValue(whereParam, null);
                            if (v == null || v == DBNull.Value)
                                sql.Append($" {this.EncodeColumn(p.Name)} IS NULL AND");
                            else
                                sql.Append($" {this.EncodeColumn(p.Name)} = @{p.Name} AND");
                        }
                        sql.Remove(sql.Length - 4, 4);
                    }
                }
            }
            int count = await this.ExecuteNonQuery(sql.ToString(), whereParam);
            return count;
        }
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <typeparam name="T">数据表</typeparam>
        /// <param name="whereParam">where参数</param>
        /// <returns></returns>
        public virtual async Task<int> Delete<T>(object whereParam) where T : class
        {
            return await this.Delete(typeof(T).Name, whereParam);
        }
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="table">数据表</param>
        /// <param name="whereSql">where sql</param>
        /// <param name="whereParam">where参数</param>
        /// <returns></returns>
        public virtual async Task<int> Delete(string table, string whereSql, object whereParam)
        {
            if (string.IsNullOrEmpty(table)) throw new ArgumentNullException(nameof(table));

            var sql = $"DELETE FROM {table}";
            if (!string.IsNullOrEmpty(whereSql)) sql = $"{sql} WHERE {whereSql}";
            int count = await this.ExecuteNonQuery(sql, whereParam);
            return count;
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <typeparam name="T">数据表</typeparam>
        /// <param name="whereSql">where sql</param>
        /// <param name="whereParam">where参数</param>
        /// <returns></returns>
        public virtual async Task<int> Delete<T>(string whereSql, object whereParam) where T : class
        {
            return await this.Delete(typeof(T).Name, whereSql, whereParam);
        }

        #endregion



        /// <summary>
        /// 添加匹配符%
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual string GetLikeValue(string value, DbLikeType type = DbLikeType.All)
        {
            if (!string.IsNullOrEmpty(value) && !value.Contains("%"))
            {
                switch (type)
                {
                    case DbLikeType.All:
                        value = "%" + value + "%";
                        break;
                    case DbLikeType.Left:
                        value = "%" + value;
                        break;
                    case DbLikeType.Right:
                        value = value + "%";
                        break;
                }
            }

            return value;
        }

        /// <summary>
        /// 校验排序列
        /// </summary>
        /// <param name="orderby"></param>
        /// <param name="propertyInfos"></param>
        /// <returns></returns>
        private List<OrderbyModel> GetOrderbyDic(string orderby, List<PropertyInfo> propertyInfos)
        {
            List<OrderbyModel> list = null;
            if (!string.IsNullOrEmpty(orderby))
            {
                var propertyrderarr = orderby.Split(',');
                list = new List<OrderbyModel>(propertyrderarr.Length);
                foreach (var s in propertyrderarr)
                {
                    var propertyorder = s.Trim();
                    if (!string.IsNullOrEmpty(propertyorder))
                    {
                        var arr = propertyorder.Split(' ');
                        if (arr.Length > 2)
                        {
                            throw new ArgumentException($"orderby({propertyorder}) is error!", nameof(orderby));
                        }
                        var col = arr[0].Trim();
                        var p = propertyInfos.Find(q => q.Name.Equals(col, StringComparison.OrdinalIgnoreCase));
                        if (p == null) throw new ArgumentException($"orderby({col}) is error!", nameof(orderby));
                        string sort = null;
                        if (arr.Length == 2)
                        {
                            var st = arr[1].Trim();
                            if (string.Equals(st, "asc", StringComparison.OrdinalIgnoreCase))
                            {
                                sort = "ASC";
                            }
                            else if (string.Equals(st, "desc", StringComparison.OrdinalIgnoreCase))
                            {
                                sort = "DESC";
                            }
                            else
                            {
                                throw new ArgumentException($"orderby({propertyorder}) is error!", nameof(orderby));
                            }
                        }

                        if (!list.Exists(q => q.property == p.Name))
                        {
                            list.Add(new OrderbyModel() { property = p.Name, sort = sort });
                        }
                    }
                }
            }

            return list ?? new List<OrderbyModel>(0);
        }

        /// <summary>
        /// 获取最终排序, order by id, name
        /// </summary>
        /// <typeparam name="T">排序model</typeparam>
        /// <param name="orderby">排序: id desc, name asc</param>
        /// <param name="defaultOrderby">默认排序: id desc</param>
        /// <param name="tb"></param>
        /// <returns></returns>
        public virtual string GetOrderby<T>(string orderby, string defaultOrderby, string tb = null) where T : class
        {
            var t = typeof(T);
            if (t.IsGenericType || t.IsArray) throw new ArgumentException($"T({t.FullName}) is error!");
            var propertyInfos = new List<PropertyInfo>(t.GetProperties());
            if ((!string.IsNullOrEmpty(orderby) || !string.IsNullOrEmpty(defaultOrderby))
                && (propertyInfos == null || propertyInfos.Count == 0))
                throw new ArgumentException($"T({t.FullName}) is error!");
            var defaultOrderList = this.GetOrderbyDic(defaultOrderby, propertyInfos);
            var orderbyList = this.GetOrderbyDic(orderby, propertyInfos);

            StringBuilder orderSql = new StringBuilder();
            foreach (var item in orderbyList)
            {
                orderSql.Append(!string.IsNullOrEmpty(tb) ? $" {this.EncodeColumn(tb)}." : " ");
                if (string.IsNullOrEmpty(item.sort)) orderSql.Append($"{this.EncodeColumn(item.property)},");
                else orderSql.Append($"{this.EncodeColumn(item.property)} {item.sort},");
            }

            foreach (var item in defaultOrderList)
            {
                if (!orderbyList.Exists(q => q.property == item.property))
                {
                    orderSql.Append(!string.IsNullOrEmpty(tb) ? $" {this.EncodeColumn(tb)}." : " ");
                    if (string.IsNullOrEmpty(item.sort)) orderSql.Append($"{this.EncodeColumn(item.property)},");
                    else orderSql.Append($"{this.EncodeColumn(item.property)} {item.sort},");
                }
            }

            if (orderSql.Length > 0)
            {
                orderSql.Remove(orderSql.Length - 1, 1);
                return $" ORDER BY{orderSql.ToString()}";
            }


            return string.Empty;
        }

        class OrderbyModel
        {
           public string property { get; set; }
            public string sort { get; set; }
        }
    }
}
