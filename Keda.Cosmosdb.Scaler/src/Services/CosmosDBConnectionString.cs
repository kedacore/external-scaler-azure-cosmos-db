using Keda.Cosmosdb.Scaler;
using System;
using System.Data.Common;

namespace Keda.CosmosDB.Scaler.Services
{
    internal class CosmosDBConnectionString
    {
        public CosmosDBConnectionString(string connectionString)
        {
            DbConnectionStringBuilder builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            object key = null;
            if (builder.TryGetValue(Constants.AccountKey, out key))
            {
                AuthKey = key.ToString();
            }

            object uri;
            if (builder.TryGetValue(Constants.AccountEndpoint, out uri))
            {
                ServiceEndpoint = new Uri(uri.ToString());
            }
        }

        public Uri ServiceEndpoint { get; set; }
        public string AuthKey { get; set; }
    }
}
