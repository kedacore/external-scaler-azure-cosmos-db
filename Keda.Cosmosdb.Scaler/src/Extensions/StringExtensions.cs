namespace Keda.CosmosDB.Scaler.Extensions
{
    public class StringExtensions
    {
        public static string NormalizeString(string inputString)
        {
            return inputString?.Replace("/", "-").Replace(".", "-").Replace(":", "-").Replace("%", "-").ToLower();
        }
    }
}