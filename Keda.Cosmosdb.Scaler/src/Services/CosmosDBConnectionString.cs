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

            AuthKey = builder[Constants.AccountKey].ToString();

            ServiceEndpoint = new Uri(builder[Constants.AccountEndpoint].ToString());
        }

        public Uri ServiceEndpoint { get; set; }
        public string AuthKey { get; set; }
    }
}
