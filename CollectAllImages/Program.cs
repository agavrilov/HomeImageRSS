using System;
using System.IO;
using System.Data.SQLite;
using System.Data;

namespace CollectAllImages
{
    class Program
    {
        static void Main(string[] args)
        {
            string directoryPath = args[0];
            string dbPath = args[1];
            using (IDbConnection connection = PrepareDB(dbPath))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(directoryPath);
                DirectoryVisitor visitor = new DirectoryVisitor(dirInfo,connection);
                visitor.StartWalking();
                WalkDirectoryTree(dirInfo, "*.jpg", visitor);
                visitor.FinishWalking();
            }
        }

        private static IDbConnection PrepareDB(string dbPath)
        {
            IDbConnection connection = new SQLiteConnection("Data Source="+dbPath);
            connection.Open();
            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = "CREATE TABLE content (fileIndex INTEGER PRIMARY KEY, url VARCHAR(255), width INTEGER, height INTEGER)";
                command.ExecuteNonQuery();
            }
            return connection;
        }

        private static void WalkDirectoryTree(System.IO.DirectoryInfo root, string mask, IDirectoryVisitor visitor)
        {
            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo[] subDirs = null;

            // First, process all the files directly under this folder
            try
            {
                files = root.GetFiles(mask);
            }
            // This is thrown if even one of the files requires permissions greater
            // than the application provides.
            catch (UnauthorizedAccessException e)
            {
                // This code just writes out the message and continues to recurse.
                // You may decide to do something different here. For example, you
                // can try to elevate your privileges and access the file again.
                Console.WriteLine(e.Message);
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }

            if (files != null)
            {
                foreach (System.IO.FileInfo fi in files)
                {
                    // In this example, we only access the existing FileInfo object. If we
                    // want to open, delete or modify the file, then
                    // a try-catch block is required here to handle the case
                    // where the file has been deleted since the call to TraverseTree().
                    visitor.VisitFile(fi);
                }

                // Now find all the subdirectories under this directory.
                subDirs = root.GetDirectories();

                foreach (System.IO.DirectoryInfo dirInfo in subDirs)
                {
                    // Resursive call for each subdirectory.
                    WalkDirectoryTree(dirInfo,mask,visitor);
                }
            }
        }
    }
}

