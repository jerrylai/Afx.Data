using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.ComponentModel;

namespace Afx.Data
{
    public class MsSqlDatabase : Database
    {
        public MsSqlDatabase(string connectionString) : base(connectionString, SqlClientFactory.Instance)
        {

        }

        public MsSqlDatabase(SqlConnection connection, bool isOwnsConnection = true) : base(connection, isOwnsConnection)
        {

        }

        public override string EncodeColumn(string column)
        {
            if (string.IsNullOrEmpty(column)) throw new ArgumentNullException("column");

            return $"[{column}]";
        }

        public override string EncodeParameterName(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            return $"@{name}";
        }
    }
}
