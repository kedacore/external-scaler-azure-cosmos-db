using Keda.CosmosDB.Scaler.Extensions;
using Xunit;

namespace Keda.CosmosDB.Scaler.UnitTest
{
    public class StringHelperTests
    {
        [Theory]
        [InlineData("", "")]
        [InlineData(null, null)]
        [InlineData("azure-cosmosdb-test.account-database:scope-collection", "azure-cosmosdb-test-account-database-scope-collection")]
        [InlineData("azure-cosmosdb-account-database-collection", "azure-cosmosdb-account-database-collection")]
        [InlineData("azure-cosmosdb-ACCOUNT-test.database-collection", "azure-cosmosdb-account-test-database-collection")]
        public void NormalizeStringTest(string input, string expected)
        {
            Assert.Equal(StringHelpers.NormalizeString(input), expected);
        }
    }
}
