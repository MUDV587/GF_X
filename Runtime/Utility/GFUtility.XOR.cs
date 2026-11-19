using GameFramework;


public static partial class GFUtility
{
    public class XOR
    {
        public static byte[] QuickXor(byte[] data_bytes, string key)
        {
            var key_bytes = Utility.Converter.GetBytes(key);

            return Utility.Encryption.GetQuickXorBytes(data_bytes, key_bytes);
        }
    }
}