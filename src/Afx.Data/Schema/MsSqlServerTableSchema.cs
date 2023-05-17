using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Afx.Data.Schema
{
    /// <summary>
    /// 
    /// </summary>
    public class MsSqlServerTableSchema : TableSchema
    {
        private Database db;
        private string database;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="db"></param>
        /// <param name="database"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public MsSqlServerTableSchema(Database db, string database)
        {
            if (db == null) throw new ArgumentNullException("db");
            if (string.IsNullOrEmpty(database)) throw new ArgumentNullException("database");
            this.database = database;
            this.db = db;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
       public override List<string> GetTables()
        {
            var sql = @"SELECT t.TABLE_NAME
FROM information_schema.tables t
LEFT JOIN sysobjects o on t.TABLE_NAME=o.name
LEFT JOIN sys.extended_properties ex on o.id=ex.major_id and ex.minor_id=0
WHERE t.TABLE_CATALOG = @database AND t.TABLE_TYPE='BASE TABLE'";

           var  list = db.Query<string>(sql, new { database });

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
                    List<string> columnList = new List<string>(item.Count());
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
            if (table == null || string.IsNullOrEmpty(table)) throw new ArgumentNullException("table");
            if (columns == null) throw new ArgumentNullException("columns");
            if (columns.Count == 0) return false;
            int count = 0;
            StringBuilder createTableSql = new StringBuilder();
            StringBuilder createKeySql = new StringBuilder();
            List<ColumnInfoModel> keyColumns = columns.Where(q => q.IsKey).ToList();
            List<IndexModel> indexs = new List<IndexModel>();
            createTableSql.AppendFormat("CREATE TABLE [{0}](", table);
            foreach (var column in columns)
            {
                createTableSql.AppendFormat("[{0}] {1} {2} NULL", column.Name, column.DataType, column.IsNullable ? "" : "NOT");
                if (column.IsAutoIncrement) createTableSql.Append(" IDENTITY (1, 1)");
                createTableSql.Append(",");

                if (column.Indexs != null && column.Indexs.Count > 0) indexs.AddRange(column.Indexs);
            }
            createTableSql.Remove(createTableSql.Length - 1, 1);
            createTableSql.Append(")");

            if (keyColumns.Count > 0)
            {
                createTableSql.Append(" ON [PRIMARY]");
                string s = keyColumns.Count(q => q.IsNonClustered == true) > 0 ? "NONCLUSTERED" : "CLUSTERED";
                createKeySql.AppendFormat("ALTER TABLE [{0}] ADD CONSTRAINT PK_{0} PRIMARY KEY {1}(", table, s);
                foreach (var column in keyColumns)
                {
                    createKeySql.AppendFormat("[{0}],", column.Name);
                }
                createKeySql.Remove(createKeySql.Length - 1, 1);
                createKeySql.Append(") WITH(STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]");
            }
            count = await this.db.ExecuteNonQuery(createTableSql.ToString());
            createTableSql.Clear();
            if (createKeySql.Length > 0)
            {
                await this.db.ExecuteNonQuery(createKeySql.ToString());
            }

            if (indexs.Count > 0)
            {
               await  this.AddIndex(table, indexs);
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
            if (string.IsNullOrEmpty(table)) throw new ArgumentNullException("table");
            if (column == null) throw new ArgumentNullException("column");

            var sql = string.Format(AddColumnSql, table, column.Name, column.DataType, column.IsNullable ? "" : "NOT");
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
            if (string.IsNullOrEmpty(table)) throw new ArgumentNullException("table");
            if (string.IsNullOrEmpty(indexName)) throw new ArgumentNullException("indexName");
            if (columns == null) throw new ArgumentNullException("columns");
            if (columns.Count == 0)
                return false;

            StringBuilder strColumns = new StringBuilder();
            foreach (var s in columns)
                strColumns.AppendFormat("[{0}],", s);
            strColumns.Remove(strColumns.Length - 1, 1);

            var sql = string.Format(AddIndexSql, isUnique ? "UNIQUE" : "", indexName,
                table, strColumns.ToString());

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
                var sql = string.Format(AddIndexSql, index.IsUnique ? "UNIQUE" : "", index.Name, table, "[" + index.ColumnName + "]");

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
            if (string.IsNullOrEmpty(table)) throw new ArgumentNullException("table");
            if (string.IsNullOrEmpty(index)) throw new ArgumentNullException("index");

            int count = 0;
            var sql = string.Format(DeleteIndexSql, index, table);
            try {
                count = await this.db.ExecuteNonQuery(sql);
            }
            catch { }

            return count > 0;
        }

        /// <summary>
        /// 获取表列信息
        /// </summary>
        /// <param name="table">表名</param>
        /// <returns>列信息</returns>
        public override List<ColumnInfoModel> GetTableColumns(string table)
        {
            if (string.IsNullOrEmpty(table)) throw new ArgumentNullException("table");
            var list = this.db.Query<ColumnInfoModel>(SelectColumnSql, new { table });

            var indexlist = this.db.Query<DbIndexInfoModel>(SelectTableIndexSql, new { table });

            foreach (var m in list)
            {
                var index_m = indexlist.Find(q => q.IsKey == 1 && q.Order == m.Order);
                m.IsKey = index_m != null;
                m.IsNonClustered = false;
                if (m.IsKey)
                {
                    index_m = indexlist.Find(q => q.IsKey == 1 && q.Order == m.Order && q.IndexType == 2);
                    m.IsNonClustered = index_m != null;
                }
                var indexs = indexlist.FindAll(q => q.IsKey == 0 && q.Order == m.Order);
                if (indexs != null && indexs.Count > 0)
                {
                    m.Indexs = new List<IndexModel>(indexs.Count);
                    foreach (var r in indexs)
                    {
                        IndexModel index = new IndexModel();
                        m.Indexs.Add(index);
                        index.ColumnName = m.Name;
                        index.Name = r.IndexName;
                        index.IsUnique = r.IsUnique;
                    }
                }
            }



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
            if (propertyType == null) throw new ArgumentNullException("propertyType");
            string type = null;
            if (propertyType.IsEnum)
            {
                propertyType = typeof(int);
            }
            else if (typeof(string) == propertyType)
            {
                if (maxLength > 8192)
                    type = "text";
                if (maxLength <= 0) maxLength = 50;
            }
            else if (typeof(decimal) == propertyType || typeof(decimal?) == propertyType)
            {
                if (minLength == 0 && maxLength == 0)
                {
                    maxLength = 18;
                    minLength = 7;
                }
                if (maxLength <= 0) maxLength = 18;
                if (minLength >= maxLength) minLength = maxLength - 1;
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
        static MsSqlServerTableSchema()
        {
            dic.Add(typeof(int), "int");

            dic.Add(typeof(IntPtr), "int");

            dic.Add(typeof(long), "bigint");

            dic.Add(typeof(bool), "bit");

            dic.Add(typeof(short), "smallint");

            dic.Add(typeof(char), "nchar(1)");

            dic.Add(typeof(char[]), "nchar({0})");

            dic.Add(typeof(byte), "tinyint");

            dic.Add(typeof(decimal), "decimal({0},{1})");

            dic.Add(typeof(float), "real");

            dic.Add(typeof(double), "float");

            dic.Add(typeof(DateTime), "datetime");

            dic.Add(typeof(DateTimeOffset), "datetimeoffset(7)");

            dic.Add(typeof(TimeSpan), "timestamp");

            dic.Add(typeof(Guid), "Uniqueidentifier");

            dic.Add(typeof(string), "nvarchar({0})");

            dic.Add(typeof(byte[]), "image");
        }

        private const string SelectColumnSql = @"SELECT col.column_id [Order], col.name [Name], t.name [DataType],
(CASE WHEN t.name='nvarchar' OR t.name='nchar' THEN col.max_length/2
ELSE (CASE WHEN t.name='decimal' OR t.name = 'numeric' THEN col.[precision] ELSE col.max_length END) END) [MaxLength],
ISNULL(col.scale, 0) [MinLength],
col.is_nullable IsNullable,
col.is_identity IsAutoIncrement,
ex.value Comment
FROM sys.columns col
INNER JOIN sys.objects o ON col.object_id=o.object_id AND o.type='U'
INNER JOIN sys.types t ON col.system_type_id=t.system_type_id AND col.user_type_id=t.user_type_id
LEFT JOIN sys.extended_properties ex on col.object_id=ex.major_id and ex.minor_id=col.column_id
WHERE o.name=@table";
        private const string SelectTableIndexSql = @"SELECT col.column_id [Order],
ISNULL(idx.is_primary_key, 0) IsKey,
ISNULL(idx.name,'') IndexName, ISNULL(idx.is_unique,0) IsUnique, idx.[type] IndexType
FROM sys.columns col
INNER JOIN sys.objects o ON col.[object_id] = o.[object_id] AND o.[type] = 'U'
INNER JOIN sys.index_columns idx_col ON col.[object_id] = idx_col.[object_id] AND col.column_id = idx_col.column_id
INNER JOIN sys.indexes idx ON col.[object_id]=idx.[object_id] AND idx_col.index_id=idx.index_id
WHERE o.name=@table";
        private const string AddColumnSql = @"ALTER TABLE [{0}] ADD [{1}] {2} {3} NULL";
        private const string AddIndexSql = @"CREATE {0} NONCLUSTERED INDEX [{1}] ON [{2}]	({3})
WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]";
    //    private const string DeleteColumnSql = @"ALTER TABLE [{0}] DROP COLUMN [{1}]";
        private const string DeleteIndexSql = @"DROP INDEX [{0}] ON [{1}]";
     //   private const string AlterColumnSql = @"ALTER TABLE [{0}] ALTER COLUMN [{1}] {2} {3} NULL";

        /// <summary>
        /// 
        /// </summary>
        public class DbIndexInfoModel
        {
            /// <summary>
            /// 
            /// </summary>
            public int Order { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int IsKey { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string IndexName { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public bool IsUnique { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int IndexType { get; set; }
        }
    }
}
