using Android.Content;
using System.IO;

namespace ChatBotZ.Utilities
{
    internal class MyFileUtils
    {
        private readonly Context _context;

        public MyFileUtils(Context context)
        {
            _context = context;
        }

        public string ReadTextFile(string filePath)
        {
            return File.ReadAllText(filePath);
        }

        public static void WriteTextFile(string filePath, string text)
        {
            File.WriteAllText(filePath, text);
        }

        public static bool SearchTextInFile(string filePath, string searchString)
        {
            string fileText = File.ReadAllText(filePath);
            return fileText.Contains(searchString);
        }

        public static byte[] ReadBinaryFile(string filePath)
        {
            return File.ReadAllBytes(filePath);
        }

        public static void WriteBinaryFile(string filePath, byte[] data)
        {
            File.WriteAllBytes(filePath, data);
        }

        public static void AppendTextToFile(string filePath, string text)
        {
            File.AppendAllText(filePath, text);
        }

        public static void AppendBinaryToFile(string filePath, byte[] data)
        {
            using (FileStream fs = File.Open(filePath, FileMode.Append))
            {
                fs.Write(data, 0, data.Length);
            }
        }

        private string GetFilePath(string fileName)
        {
            string documentsPath = _context.GetExternalFilesDir(null).AbsolutePath;
            return Path.Combine(documentsPath, fileName);
        }

    }
}