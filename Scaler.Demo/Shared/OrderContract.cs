using System;
using Newtonsoft.Json;

namespace Keda.CosmosDb.Scaler.Demo.Shared
{
    public class Order
    {
        [JsonProperty("id")]
        public string Id { get; }

        public int Amount { get; set; }
        public string Article { get; set; }
        public Customer Customer { get; set; }

        public Order()
        {
            this.Id = Guid.NewGuid().ToString();
        }
    }

    public class Customer
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
