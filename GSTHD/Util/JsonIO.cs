using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSTHD.Util
{
    public static class JsonIO
    {
        public static T Read<T>(string filePath)
            where T : new()
        {
            if (File.Exists(filePath))
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
            else
            {
                var obj = new T();
                Write(obj, filePath);
                return obj;
            }
        }

        //public static string ReadAsString<T>(string filePath)
        //    where T : new()
        //{
        //    if (!File.Exists(filePath))
        //    {
        //        var obj = new T();
        //        Write(obj, filePath);
        //    }
        //    return File.ReadAllText(filePath);
        //}
        
        //public static T StringToObject<T>(string text)
        //    where T : new()
        //{
        //    return JsonConvert.DeserializeObject<T>(text);
        //}

        public static void Write<T>(T obj, string filePath)
        {
            var str = JsonConvert.SerializeObject(obj, Formatting.Indented);
            var fullPath = Path.GetFullPath(filePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempFilePath = Path.Combine(directory ?? AppContext.BaseDirectory, Path.GetRandomFileName());

            try
            {
                File.WriteAllText(tempFilePath, str);

                if (File.Exists(fullPath))
                {
                    File.Replace(tempFilePath, fullPath, null);
                }
                else
                {
                    File.Move(tempFilePath, fullPath);
                }
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }
    }
}
