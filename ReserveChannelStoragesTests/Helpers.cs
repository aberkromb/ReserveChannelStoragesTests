using System;
using System.Globalization;
using System.Threading.Tasks;

namespace ReserveChannelStoragesTests
{
    public class Helpers
    {
        public static string DateTimeFormatedString => DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
        
        public static async Task Try(Func<Task> func)
        {
            try
            {
                await func();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        
        public static Unit Try(Func<Unit> func)
        {
            try
            {
                return func();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new Unit();
            }
        }
    }
}