using System;
using System.IO;
using System.Windows.Media.Imaging;
using System.Data;

namespace CollectAllImages
{
    class DirectoryVisitor : IDirectoryVisitor
    {
        private Uri rootUri;
        private IDbConnection connection;
        private IDbTransaction transaction;
        private IDbCommand command;
        private int currentIndex;

        public DirectoryVisitor(DirectoryInfo rootDir, IDbConnection connection)
        {
            string dirFullPath = rootDir.FullName;
            if (!dirFullPath.EndsWith(@"\"))
                dirFullPath += @"\";
            this.rootUri = new Uri(dirFullPath);
            this.connection = connection;
        }

        public void StartWalking()
        {
            transaction = connection.BeginTransaction();
            command = connection.CreateCommand();
            command.CommandText = "INSERT INTO content (fileIndex, url, width, height) VALUES (?,?,?,?)";
            for (int i=0; i < 4; i++)
                command.Parameters.Add(command.CreateParameter());
            currentIndex = 0;
        }

        public void FinishWalking()
        {
            transaction.Commit();
            Console.WriteLine("Total Files={0}", currentIndex);
        }

        public void VisitFile(FileInfo f)
        {
            if (!f.Name.StartsWith("."))
                WriteInfo(f);
        }

        /*
        private static string GetDate(FileInfo f)
        {
            FileStream fs = new FileStream(f.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            BitmapSource img = BitmapFrame.Create(fs);
            BitmapMetadata md = (BitmapMetadata)img.Metadata;
            string date = md.DateTaken;
            Console.WriteLine(date);
            return date;
        }
        */

        private static bool GetDimensions(FileInfo f, out int width, out int height)
        {
            bool result = false;
            try
            {
                using (FileStream fs = new FileStream(f.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    BitmapSource img = BitmapFrame.Create(fs, BitmapCreateOptions.None, BitmapCacheOption.None);
                    width = img.PixelWidth;
                    height = img.PixelHeight;
                    result = true;
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Exception at file '"+f.FullName+"' :"+e.Message);
                width = height = -1;
            }
            return result;
        }

        private void WriteInfo(FileInfo f)
        {
            int width, height;

            if (GetDimensions(f, out width, out height))
            {
                string path = GetRelativePath(f);

                ((IDataParameter)command.Parameters[0]).Value = currentIndex;
                ((IDataParameter)command.Parameters[1]).Value = path;
                ((IDataParameter)command.Parameters[2]).Value = width;
                ((IDataParameter)command.Parameters[3]).Value = height;
                command.ExecuteNonQuery();

                if ((currentIndex % 10) == 0)
                    Console.WriteLine("index={0} name={1} width={2} height={3}", currentIndex, path, width, height);
                ++currentIndex;
            }
        }

        private string GetRelativePath(FileInfo f)
        {
            Uri uri = new Uri(f.FullName);
            Uri relativeUri = rootUri.MakeRelativeUri(uri);
            return relativeUri.ToString();
        }
    }
}
