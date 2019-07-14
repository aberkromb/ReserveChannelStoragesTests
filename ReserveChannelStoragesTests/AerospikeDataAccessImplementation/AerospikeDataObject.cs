namespace ReserveChannelStoragesTests.AerospikeDataAccessImplementation
{
    public class AerospikeDataObject
    {
        /// <summary>
        ///     Имя наймспейса
        /// </summary>
        public string Namespace { get; set; } = "test";

        /// <summary>
        ///     Имя множества в неймспейсе - аналог таблицы
        /// </summary>
        public string SetName { get; set; }

        /// <summary>
        ///      Ключ в множестве
        /// </summary>
        public int Key { get; set; }
        
        /// <summary>
        ///     Непосредственно сохраняемое значение 
        /// </summary>
        public byte[] Data { get; set; }
    }
}