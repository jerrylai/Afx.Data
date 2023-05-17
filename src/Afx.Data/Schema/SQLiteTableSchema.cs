using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data.Schema
{
    /// <summary>
    /// 
    /// </summary>
    public class SQLiteTableSchema : TableSchema
    {
        private Database db;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="db"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public SQLiteTableSchema(Database db)
        {
            if (db == null) throw new ArgumentNullException("db");
//#if NETCOREAPP || NETSTANDARD
//            var stringBuilder = new SqliteConnectionStringBuilder(connectionString);
//#else
//            var stringBuilder = new SQLiteConnectionStringBuilder(connectionString);
//#endif

            this.db = db;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override List<string> GetTables()
        {
            var sql = @"SELECT [name] FROM [sqlite_master] WHERE [type]='table' AND [tbl_name] <> 'sqlite_sequence'";
            var list = db.Query<string>(sql);

            return list;
        }
        /// <summary>
        /// 添加索引
        /// </summary>
        /// <param name="table">表名</param>
        /// <param name="indexs">索引列信息</param>
        public override async Task AddIndex(string table, List<IndexModel> indexs)
        {
            if (string.IsNullOrEmpty(table)) throw new ArgumentNullException("table");
            if (indexs == null) throw new ArgumentNullException("indexs");
            var list = indexs.FindAll(q => !string.IsNullOrEmpty(q.Name) && !string.IsNullOrEmpty(q.ColumnName));
            if (list.Count > 0)
            {
                var group = list.GroupBy(q => q.Name, StringComparer.OrdinalIgnoreCase);
                foreach (var item in group)
                {
                    string indexName = item.Key;
                    bool isUnique = item.Count(q => q.IsUnique) > 0;
                    List<string> columnList = new List<string>();
                    foreach (var m in item)
                    {
                        columnList.Add(m.ColumnName);
                    }
                   await this.AddIndex(table, indexName, isUnique, columnList);
                }
            }
        }
        /// <summary>
        /// 创建数据库表
        /// </summary>
        /// <param name="table">表名</param>
        /// <param name="columns">列信息</param>
        /// <returns>是否成功</returns>
        public override async Task<bool> CreateTable(string table, List<ColumnInfoModel> columns)
        {
            if (string.IsNullOrEmpty(table) || columns == null && columns.Count == 0)
                return true;

            int count = 0;
            StringBuilder createTableSql = new StringBuilder();
            List<ColumnInfoModel> keyColumns = columns.Where(q => q.IsKey).ToList();
            List<IndexModel> indexs = new List<IndexModel>();
            createTableSql.AppendFormat("CREATE TABLE [{0}](", table);
            foreach (var column in columns)
            {
                createTableSql.AppendFormat("[{0}] {1} {2} NULL", column.Name, column.DataType, column.IsNullable ? "" : "NOT");
                if (column.IsKey && keyColumns.Count == 1)
                {
                    createTableSql.Append(" PRIMARY KEY");
                    if (column.IsAutoIncrement) createTableSql.Append(" AUTOINCREMENT");
                }
                createTableSql.Append(",");

                if (column.Indexs != null && column.Indexs.Count > 0) indexs.AddRange(column.Indexs);
            }
            createTableSql.Remove(createTableSql.Length - 1, 1);

            if (keyColumns.Count > 1)
            {
                createTableSql.AppendFormat(", CONSTRAINT PK_{0} PRIMARY KEY (", table);

                foreach (var column in keyColumns)
                {
                    createTableSql.AppendFormat("[{0}],", column.Name);
                }
                createTableSql.Remove(createTableSql.Length - 1, 1);
                createTableSql.Append(")");
            }

            createTableSql.Append(")");

            count = await this.db.ExecuteNonQuery(createTableSql.ToString());
            if (indexs.Count > 0)
            {
                await this.AddIndex(table, indexs);
            }

            return count > 0;
        }

        /// <summary>
        /// 添加列
        /// </summary>
        /// <param name="table">表名</param>
        /// <param name="column">列信息</param>
        /// <returns>是否成功</returns>
        public override async Task<bool> AddColumn(string table, ColumnInfoModel column)
        {
            var sql = string.Format("ALTER TABLE [{0}] ADD COLUMN [{1}] {2} {3} NULL;",
                table, column.Name, column.DataType, column.IsNullable ? "" : "NOT");

            int count = await this.db.ExecuteNonQuery(sql);
            return count > 0;
        }
        /// <summary>
        /// 添加索引
        /// </summary>
        /// <param name="table">表名</param>
        /// <param name="indexName">索引名称</param>
        /// <param name="isUnique">是否唯一索引</param>
        /// <param name="columns">列名</param>
        /// <returns>是否成功</returns>
        public override async Task<bool> AddIndex(string table, string indexName, bool isUnique, List<string> columns)
        {
            if (columns == null && columns.Count == 0)
                return false;

            StringBuilder strColumns = new StringBuilder();
            foreach (var s in columns)
                strColumns.AppendFormat("[{0}],", s);
            strColumns.Remove(strColumns.Length - 1, 1);

            var sql = string.Format("CREATE {0} INDEX [{1}] ON [{2}] ({3})",
                isUnique ? "UNIQUE" : "", indexName, table, strColumns.ToString());

            int count = await this.db.ExecuteNonQuery(sql);
            return count > 0;
        }
        /// <summary>
        /// 添加索引
        /// </summary>
        /// <param name="table">表名</param>
        /// <param name="index">索引列信息</param>
        /// <returns>是否成功</returns>
        public override async Task<bool> AddIndex(string table, IndexModel index)
        {
            if (string.IsNullOrEmpty(table)) throw new ArgumentNullException("table");
            if (index == null) throw new ArgumentNullException("index");
            int count = 0;
            if (!string.IsNullOrEmpty(index.Name) && !string.IsNullOrEmpty(index.ColumnName))
            {
                var sql = string.Format("CREATE {0} INDEX [{1}] ON [{2}] ([{3}])",
                    index.IsUnique ? "UNIQUE" : "", index.Name, table, index.ColumnName);
                count = await this.db.ExecuteNonQuery(sql);
            }

            return count > 0;
        }

        /// <summary>
        /// 删除索引
        /// </summary>
        /// <param name="table">表名</param>
        /// <param name="index">索引名称</param>
        /// <returns>是否成功</returns>
        public override async Task<bool> DeleteIndex(string table, string index)
        {
            int count = 0;
            var sql = string.Format("DROP INDEX [{0}] ON [{1}]", index, table);
            try {
                count = await this.db.ExecuteNonQuery(sql);
            }
            catch { }

            return count > 0;
        }


        private void GetIndexName(string table, List<ColumnInfoModel> columns)
        {
            var sql = @"SELECT [name],[sql] FROM [sqlite_master] WHERE [type]='index' AND [tbl_name]=@tb AND [sql] is not null";
            var list = db.Query<SqliteSqlInfoModel>(sql, new { tb = table });

            foreach (var row in list)
            {
                if (string.IsNullOrEmpty(row.sql)) continue;
                bool isUnique = row.sql.IndexOf("CREATE UNIQUE ", StringComparison.OrdinalIgnoreCase) > 0;
                int begin = row.sql.IndexOf("(");
                if (begin > 0)
                {
                    begin += 1;
                    int end = row.sql.IndexOf(")", begin);
                    if (end > begin)
                    {
                        string s = row.sql.Substring(begin, end - begin);
                        string[] arr = s.Split(',');
                        if (arr != null && arr.Length > 0)
                        {
                            foreach (var item in arr)
                            {
                                string col = item.Trim();
                                col = col.TrimStart('[');
                                col = col.TrimEnd(']');
                                if (!string.IsNullOrEmpty(col))
                                {
                                    var m = columns.Find(q => string.Compare(q.Name, col,
                                        StringComparison.OrdinalIgnoreCase) == 0);
                                    if (m != null)
                                    {
                                        if (m.Indexs == null) m.Indexs = new List<IndexModel>();
                                        var index = new IndexModel();
                                        m.Indexs.Add(index);
                                        index.ColumnName = m.Name;
                                        index.Name = row.name;
                                        index.IsUnique = isUnique;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 获取表列信息
        /// </summary>
        /// <param name="table">表名</param>
        /// <returns>列信息</returns>
        public override List<ColumnInfoModel> GetTableColumns(string table)
        {
            List<ColumnInfoModel> list = new List<ColumnInfoModel>();
            var sql = string.Format(@"PRAGMA TABLE_INFO('{0}')", table);
            var cols = db.Query<SqliteTableColumnInfoModel>(sql);

            foreach (var row in cols)
            {
                string typeString = row.type;
                ColumnInfoModel m = new ColumnInfoModel();
                list.Add(m);
                m.Name = row.name;
                m.IsNonClustered = false;
                m.IsKey = row.pk;
                m.IsNullable = !row.notnull;
                m.IsAutoIncrement = false;
                m.DataType = typeString;
                int index = typeString.IndexOf('(');
                if (index > 0)
                {
                    m.DataType = typeString.Substring(0, index);
                    string s = typeString.Substring(index + 1, typeString.Length - index - 2);
                    string[] sarr = s.Split(',');
                    int len = 0;
                    if (int.TryParse(sarr[0].Trim(), out len))
                        m.MaxLength = len;
                    if (sarr.Length > 1 && int.TryParse(sarr[1].Trim(), out len))
                        m.MinLength = len;
                }
                m.Order = row.cid;
            }
            if (list.Count(q => q.IsKey) == 1)
            {
                sql = string.Format(@"SELECT [sql] FROM [sqlite_master] WHERE [type]='table' AND [name]='{0}'", table);
                var sqls = (db.ExecuteScalar<string>(sql) ?? "").ToUpper();
                var m = list.Find(q => q.IsKey);
                m.IsAutoIncrement = sqls.Contains("PRIMARY KEY AUTOINCREMENT");
            }
            this.GetIndexName(table, list);


            return list;
        }



        /// <summary>
        /// 获取列数据库类型
        /// </summary>
        /// <param name="propertyType">model 属性类型</param>
        /// <param name="maxLength">类型最大长度</param>
        /// <param name="minLength">类型最小长度</param>
        /// <returns>列数据库类型</returns>
        public override string GetColumnType(Type propertyType, int maxLength, int minLength)
        {
            string type = null;
            if (propertyType.IsEnum)
            {
                propertyType = typeof(int);
            }
            else if (typeof(string) == propertyType)
            {
                if (maxLength > 8192 && maxLength < 65535)
                    type = "text";
                else if (maxLength <= 0)
                    maxLength = 50;
            }
            else if (typeof(decimal) == propertyType || typeof(decimal?) == propertyType)
            {
                if (minLength == 0 && maxLength == 0)
                {
                    maxLength = 18;
                    minLength = 7;
                }
                if (maxLength <= 0) maxLength = 38;
                if (minLength > maxLength) minLength = maxLength - 1;
            }

            if (null != type || dic.TryGetValue(propertyType, out type))
            {
                type = string.Format(type, maxLength, minLength);
            }

            return type;
        }
        /// <summary>
        /// 释放资源
        /// </summary>
        public override void Dispose()
        {
            if (db != null) db.Dispose();
            this.db = null;
            base.Dispose();
        }

        private static Dictionary<Type, string> dic = new Dictionary<Type, string>();
        static SQLiteTableSchema()
        {
            dic.Add(typeof(int), "integer");

            dic.Add(typeof(IntPtr), "integer");

            dic.Add(typeof(long), "bigint");

            dic.Add(typeof(bool), "boolean");

            dic.Add(typeof(short), "smallint");

            dic.Add(typeof(char), "nchar(1)");
            dic.Add(typeof(char[]), "nchar({0})");

            dic.Add(typeof(byte), "tinyint");

            dic.Add(typeof(decimal), "decimal({0},{1})");

            dic.Add(typeof(float), "float");

            dic.Add(typeof(double), "double");

            dic.Add(typeof(DateTime), "varchar(30)");
            dic.Add(typeof(DateTimeOffset), "varchar(30)");

            dic.Add(typeof(TimeSpan), "varchar(30)");

            dic.Add(typeof(Guid), "varchar(40)");

            dic.Add(typeof(string), "nvarchar({0})");

            dic.Add(typeof(byte[]), "blob");
        }
        /// <summary>
        /// 
        /// </summary>
        public class SqliteSqlInfoModel
        {
            /// <summary>
            /// 
            /// </summary>
            public string name { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string sql { get; set; }
        }
        /// <summary>
        /// 
        /// </summary>
        public class SqliteTableColumnInfoModel
        {
            /// <summary>
            /// 
            /// </summary>
            public string type { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string name { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public bool pk { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public bool notnull { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int cid { get; set; }
        }
    }
}
