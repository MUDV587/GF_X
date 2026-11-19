using System.Text;


public static partial class GFUtility
{
    public class MD5_32
    {
        public static string Encrypt(string strText)
        {
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] fromData = Encoding.Default.GetBytes(strText);
            byte[] targetData = md5.ComputeHash(fromData);
            StringBuilder byte2String = new();
            for (int i = 0; i < targetData.Length; i++)
            {
                byte2String.AppendFormat("{0:x2}", targetData[i]);
            }
            return byte2String.ToString().ToUpper();
        }
    }
}