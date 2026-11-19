using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine;


public static partial class GFUtility
{
    public static class Zip
    {
        /// <summary>
        /// 压缩
        /// </summary>
        /// <param name="zipDir"></param>
        /// <param name="outputZipFile"></param>
        /// <returns></returns>
        public static bool Compress(string zipDir, string outputZipFile)
        {
            if (!Directory.Exists(zipDir))
            {
                return false;
            }
            FileStream zipFile = File.Create(outputZipFile);
            ZipOutputStream zipStream = new(zipFile);
            string[] files = Directory.GetFiles(zipDir, "*", SearchOption.AllDirectories);
            foreach (string fileName in files)
            {

                string relativePath = fileName.Substring(zipDir.Length + 1);
                ZipEntry entry = new(relativePath);
                entry.DateTime = DateTime.UtcNow;
                zipStream.PutNextEntry(entry);
                using (FileStream reader = File.OpenRead(fileName))
                {
                    int size;
                    byte[] data = new byte[reader.Length];
                    while ((size = reader.Read(data, 0, data.Length)) > 0)
                    {
                        zipStream.Write(data, 0, size);
                    }
                    reader.Close();
                    reader.Dispose();
                }
            }
            zipStream.Flush();
            zipStream.Close();
            zipStream.Dispose();
            zipFile.Close();
            zipFile.Dispose();
            return true;
        }
        /// <summary>
        /// 解压缩
        /// </summary>
        /// <param name="zipFile"></param>
        /// <param name="unzipDir"></param>
        /// <returns></returns>
        public static bool Decompress(string zipFile, string unzipDir)
        {
            if (!File.Exists(zipFile))
            {
                Debug.LogErrorFormat("zip file is not exist:{0}", zipFile);
                return false;
            }
            FileStream fileStream = File.OpenRead(zipFile);
            using (ZipInputStream unzipStream = new(fileStream))
            {
                ZipEntry zipEntry = null;
                while ((zipEntry = unzipStream.GetNextEntry()) != null)
                {
                    string directoryName = Path.GetDirectoryName(zipEntry.Name);
                    string fileName = Path.GetFileName(zipEntry.Name);
                    if (!string.IsNullOrEmpty(directoryName))
                    {
                        Directory.CreateDirectory(AssetsPath.GetCombinePath(unzipDir, directoryName));
                    }
                    if (fileName != String.Empty)
                    {
                        using (FileStream streamWriter = File.Create(AssetsPath.GetCombinePath(unzipDir, zipEntry.Name)))
                        {
                            int size;
                            byte[] data = new byte[zipEntry.Size];
                            while ((size = unzipStream.Read(data, 0, data.Length)) > 0)
                            {
                                streamWriter.Write(data, 0, size);
                            }
                        }
                    }
                }
                return true;
            }
        }
    }
}