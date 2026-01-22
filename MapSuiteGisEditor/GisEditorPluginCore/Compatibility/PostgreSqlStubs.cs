using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThinkGeo.Core;

namespace ThinkGeo.Core
{
    // Stub implementations to keep Postgre features compiling when the provider
    // package is not available. These do not provide real data access.
    public class PostgreSqlFeatureLayer : FeatureLayer
    {
        public PostgreSqlFeatureLayer(string connectionString, string tableName, string featureIdColumn)
        {
            ConnectionString = connectionString;
            TableName = tableName;
            FeatureIdColumn = featureIdColumn;
            FeatureSource = new PostgreSqlFeatureSource(connectionString, tableName, featureIdColumn);
        }

        public string ConnectionString { get; }
        public string TableName { get; }
        public string FeatureIdColumn { get; }
        public string SchemaName { get; set; }
        public int CommandTimeout { get; set; }
    }

    public class PostgreSqlFeatureSource : FeatureSource
    {
        public PostgreSqlFeatureSource(string connectionString, string tableName, string featureIdColumn)
        {
            ConnectionString = connectionString;
            TableName = tableName;
            FeatureIdColumn = featureIdColumn;
        }

        public string ConnectionString { get; }
        public string TableName { get; }
        public string FeatureIdColumn { get; }
        public string SchemaName { get; set; }

        public static Collection<string> GetDatabaseNames(string server, string port, string userName, string password)
        {
            return new Collection<string>();
        }

        public static Collection<string> GetTableNames(string connectionString)
        {
            return new Collection<string>();
        }

        public static Collection<string> GetViewNames(string connectionString)
        {
            return new Collection<string>();
        }

        protected override Collection<Feature> GetAllFeaturesCore(IEnumerable<string> returningColumnNames)
        {
            return new Collection<Feature>();
        }
    }
}
