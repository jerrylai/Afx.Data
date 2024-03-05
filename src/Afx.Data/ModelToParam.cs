using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Afx.Data
{
    /// <summary>
    /// sql 参数class 转换sqlParameter接口
    /// </summary>
    internal class ModelToParam : IModelToParam
    {
        /// <summary>
        /// sql 参数 class 转换
        /// </summary>
        /// <param name="db">IDatabase</param>
        /// <param name="command">IDbCommand</param>
        /// <param name="sql">sql</param>
        /// <param name="parameters">sql 参数</param>
        public virtual void To(IDatabase db, IDbCommand command, string sql, object parameters)
        {
            string commandText = sql;
            if (parameters != null)
            {
                var t = parameters.GetType();
                if (t.IsClass)
                {
                    var parr = t.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p=>p.CanRead).ToArray();
                    StringBuilder stringBuilder = new StringBuilder();
                    foreach (var p in parr)
                    {
                        bool addParam = false;
                        var pname = db.EncodeParameterName(p.Name);
                        if (command.CommandType == CommandType.Text)
                        {
                            var name = "@" + p.Name;
                            var regex = new Regex($"(\\s|[=<>;,\\(\\)]|^){name}(\\s|[=<>;,\\(\\)]|$)");
                            if (regex.IsMatch(commandText))
                            {
                                addParam = true;
                                if (pname != name)
                                {
                                    do
                                    {
                                        commandText = regex.Replace(commandText, (match) =>
                                        {
                                            return match.Value.Replace(name, pname);
                                        });
                                    } while (regex.IsMatch(commandText));
                                }
                            }
                        }
                        else //if(command.CommandType == CommandType.StoredProcedure)
                        {
                            addParam = true;
                        }

                        if (addParam)
                        {
                            var v = p.GetValue(parameters, null);
                            var parameter = command.CreateParameter();
                            parameter.ParameterName = pname;
                            parameter.Value = v ?? DBNull.Value;
                            if(v == null && p.PropertyType == typeof(string))
                            {
                                var atts = p.GetCustomAttributes(typeof(RequiredAttribute), false);
                                if (atts != null && atts.Length > 0) parameter.Value = string.Empty;
                            }
                            else if(p.PropertyType == typeof(DateTime) && ((DateTime)v) == DateTime.MinValue)
                            {
                                parameter.Value = DateTime.Now;
                            }

                            if (p.PropertyType.IsEnum)
                            {
                                parameter.Value = (int)v;
                                parameter.DbType = DbType.Int32;
                            }
                            else
                            {
                                DbType dbType;
                                if (ModelMaping.dbTypeDic.TryGetValue(p.PropertyType, out dbType))
                                    parameter.DbType = dbType;
                            }
                            command.Parameters.Add(parameter);
                        }
                    }
                }
            }

            command.CommandText = commandText;
        }
    }

    internal class DicToParam : IModelToParam
    {
        /// <summary>
        /// sql 参数 class 转换
        /// </summary>
        /// <param name="db">IDatabase</param>
        /// <param name="command">IDbCommand</param>
        /// <param name="sql">sql</param>
        /// <param name="parameters">sql 参数</param>
        /// <returns></returns>
        public virtual void To(IDatabase db, IDbCommand command, string sql, object parameters)
        {
            string commandText = sql;
            if (parameters != null && parameters is IEnumerable<KeyValuePair<string, object>>)
            {
                var dic = parameters as IEnumerable<KeyValuePair<string, object>>;
                StringBuilder stringBuilder = new StringBuilder();
                foreach (KeyValuePair<string, object> kv in dic)
                {
                    bool addParam = false;
                    var key = kv.Key.TrimStart('@');
                    var pname = db.EncodeParameterName(key);
                    if (command.CommandType == CommandType.Text)
                    {
                        string name = "@" + key;
                        var regex = new Regex($"(\\s|[=<>;,\\(\\)]|^){name}(\\s|[=<>;,\\(\\)]|$)");
                        if (regex.IsMatch(commandText))
                        {
                            addParam = true;
                            if (pname != name)
                            {
                                do
                                {
                                    commandText = regex.Replace(commandText, (match) =>
                                    {
                                        return match.Value.Replace(name, pname);
                                    });
                                } while (regex.IsMatch(commandText));
                            }

                        }
                    }
                    else //if (command.CommandType == CommandType.StoredProcedure)
                    {
                        addParam = true;
                    }

                    if (addParam)
                    {
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = pname;
                        parameter.Value = kv.Value ?? DBNull.Value;
                        if ((kv.Value is DateTime) && ((DateTime)kv.Value) == DateTime.MinValue)
                        {
                            parameter.Value = DateTime.Now;
                        }
                        Type kt = null;
                        if (kv.Value != null) kt = kv.Value.GetType();
                        if (kt != null && kt.IsEnum)
                        {
                            parameter.Value = (int)kv.Value;
                            parameter.DbType = DbType.Int32;
                        }
                        else
                        {
                            DbType dbType;
                            if (kt != null && ModelMaping.dbTypeDic.TryGetValue(kt, out dbType))
                                parameter.DbType = dbType;
                        }
                        command.Parameters.Add(parameter);
                    }
                }
            }

            command.CommandText = commandText;
        }
    }
}
