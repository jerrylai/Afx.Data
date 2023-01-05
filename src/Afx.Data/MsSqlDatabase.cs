using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.ComponentModel;
using Afx.Data.Schema;

namespace Afx.Data
{
    /// <summary>
    /// 
    /// </summary>
    public class MsSqlDatabase : Database
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString"></param>
        public MsSqlDatabase(string connectionString) : base(connectionString, SqlClientFactory.Instance)
        {

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="isOwnsConnection"></param>
        public MsSqlDatabase(SqlConnection connection, bool isOwnsConnection = true) : base(connection, isOwnsConnection)
        {

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public override string EncodeColumn(string column)
        {
            if (string.IsNullOrEmpty(column)) throw new ArgumentNullException("column");

            return $"[{column}]";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public override string EncodeParameterName(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            return $"@{name}";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override ITableSchema GeTableSchema()
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder(this.ConnectionString);
            return new MsSqlServerTableSchema(new MsSqlDatabase(this.ConnectionString), connectionStringBuilder.InitialCatalog);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override IDatabaseSchema GetDatabaseSchema()
        {
            //Data Source=127.0.0.1;Initial Catalog=master;User ID=sa;Password=123;Pooling=False;Min Pool Size=1;Max Pool Size=100;Load Balance Timeout=30;Application Name=Afx.Data
            var connectionStringBuilder = new SqlConnectionStringBuilder(this.ConnectionString);
            var database = connectionStringBuilder.InitialCatalog;
            connectionStringBuilder.InitialCatalog = "master";
            connectionStringBuilder.Remove("Min Pool Size");
            connectionStringBuilder.Remove("Max Pool Size");
            connectionStringBuilder.Remove("Load Balance Timeoute");
            connectionStringBuilder.Pooling = false;
            return new MsSqlServerDatabaseSchema(new MsSqlDatabase(connectionStringBuilder.ConnectionString), database);
        }
    }
}
