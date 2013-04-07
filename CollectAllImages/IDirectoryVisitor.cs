using System.IO;

namespace CollectAllImages
{
    interface IDirectoryVisitor
    {
        void VisitFile(FileInfo f);
    }
}
