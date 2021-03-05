using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Keda.Cosmosdb.Scaler
{
    public class ConsoleLogger
    {
        const string logMessageFormat = "{0} {1}";
        public void Log(string logMessage)
        {
            Console.WriteLine(string.Format(logMessageFormat, DateTime.UtcNow, logMessage));
        }
    }
}
