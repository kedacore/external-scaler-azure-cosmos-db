namespace Keda.CosmosDB.Scaler.Extensions
{
    public static class StringHelpers
    {
        public static string NormalizeString(string inputString)
        {
            return inputString?.Replace("/", "-").Replace(".", "-").Replace(":", "-").Replace("%", "-").ToLower();
        }
    }
}