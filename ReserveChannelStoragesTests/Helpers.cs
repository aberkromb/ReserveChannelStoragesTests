using System.Globalization;

namespace ReserveChannelStoragesTests
{
    public class Helpers
    {
        public static string DateTime => System.DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
    }
}