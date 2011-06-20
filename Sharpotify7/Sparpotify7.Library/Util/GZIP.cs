﻿using System;
using System.IO;
using ICSharpCode.SharpZipLib.GZip;

namespace Sharpotify.Util
{
    internal static class GZIP
    {
        private const int BUFFER_SIZE = 4096;

        public static byte[] Inflate(byte[] bytes)
        {
            MemoryStream ms = new MemoryStream(bytes);
            MemoryStream output = new MemoryStream();
            GZipInputStream gzip = new GZipInputStream(ms);
            byte[] buffer = new byte[BUFFER_SIZE];
            try
            {
                while (gzip.CanRead)
                {
                    int bytesRead = gzip.Read(buffer, 0, buffer.Length);
                    if (bytesRead <= 0)
                        break;
                    output.Write(buffer, 0, bytesRead);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                gzip.Close();
                ms = null;
            }

            return output.ToArray();
        }
    }
}
