using System.IO;

namespace GitStoreDotnet
{
    internal static class DirectoryHelper
    {
        public static void EnsureDirectoryExists(string path)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public static void Delete(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                return;
            }

            NormalizeAttributes(directoryPath);
            Directory.Delete(directoryPath, true);
        }

        private static void NormalizeAttributes(string directoryPath)
        {
            string[] filePaths = Directory.GetFiles(directoryPath);
            string[] subdirectoryPaths = Directory.GetDirectories(directoryPath);

            foreach (string filePath in filePaths)
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
            }

            foreach (string subdirectoryPath in subdirectoryPaths)
            {
                NormalizeAttributes(subdirectoryPath);
            }

            File.SetAttributes(directoryPath, FileAttributes.Normal);
        }
    }
}