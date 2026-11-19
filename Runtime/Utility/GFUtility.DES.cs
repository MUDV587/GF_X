using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using GameFramework;
using Logger = UnityGameFramework.Runtime.Log;


public static partial class GFUtility
{
    public class DES
    {
        public static string Encrypt(string stringToEncrypt, string sKey)
        {
            try
            {
                DESCryptoServiceProvider des = new();
                byte[] inputByteArray = Utility.Converter.GetBytes(stringToEncrypt);
                des.Key = Encoding.UTF8.GetBytes(sKey);
                des.IV = Encoding.UTF8.GetBytes(sKey);
                MemoryStream ms = new();
                CryptoStream cs = new(ms, des.CreateEncryptor(), CryptoStreamMode.Write);
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();
                StringBuilder ret = new();
                foreach (byte b in ms.ToArray())
                {
                    ret.AppendFormat("{0:X2}", b);
                }
                return ret.ToString();
            }
            catch (Exception e)
            {
                Logger.Error("加密失败:{0}", e.Message);
                return string.Empty;
            }

        }


        public static string Decrypt(string stringToDecrypt, string sKey)
        {
            try
            {
                DESCryptoServiceProvider des = new();
                byte[] inputByteArray = new byte[stringToDecrypt.Length / 2];
                for (int x = 0; x < stringToDecrypt.Length / 2; x++)
                {
                    string strx = stringToDecrypt.Substring(x * 2, 2);
                    inputByteArray[x] = Convert.ToByte(strx, 16);
                }
                des.Key = Utility.Converter.GetBytes(sKey);
                des.IV = Utility.Converter.GetBytes(sKey);
                MemoryStream ms = new();

                CryptoStream cs = new(ms, des.CreateDecryptor(), CryptoStreamMode.Write);
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();
                return Utility.Converter.GetString(ms.ToArray());
            }
            catch (Exception e)
            {
                Logger.Error("解密失败:{0}", e.Message);
                return string.Empty;
            }
        }
    }
}