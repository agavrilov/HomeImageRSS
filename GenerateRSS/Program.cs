using System;
using System.Reflection;
using System.IO;
using System.Xml;
using System.Data;
using System.Data.SQLite;
using System.Text;

namespace GenerateRSS
{
    class Program
    {
        static void Main(string[] args)
        {
            string dbFilePath = args[0];
            string targetRSSFilePath = args[1];
            int numberOfItems = Convert.ToInt32(args[2]);
            int durationInSeconds = Convert.ToInt32(args[3]);
            string rootUri = null;
            if (args.Length > 4)
                rootUri = args[4];

            // time to live (in minutes)
            int ttl = (numberOfItems * durationInSeconds) / 60;

            string rssTemplate = GenerateRSS.Resource.RSSTemplate;

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(rssTemplate);

            if (rootUri == null)
            {
                XmlNode linkNode = doc.SelectSingleNode("//link");
                rootUri = linkNode.InnerText;
            }
            if (!rootUri.EndsWith("/"))
                rootUri += "/";

            XmlNode channelNode = doc.SelectSingleNode("//channel");
            XmlElement ttlElem = doc.CreateElement("ttl");
            ttlElem.AppendChild(doc.CreateTextNode(ttl.ToString()));
            channelNode.AppendChild(ttlElem);

            using (IDbConnection connection = new SQLiteConnection("Read Only=True;FailIfMissing=True;Data Source="+dbFilePath))
            {
                connection.Open();
                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM content";
                    int count = Convert.ToInt32(command.ExecuteScalar());

                    StringBuilder indices = new StringBuilder();
                    Random random = new Random();
                    for (int i = 0; i < numberOfItems; i++)
                    {
                        if (i > 0)
                            indices.Append(",");
                        int index = random.Next(0, count);
                        indices.Append(index);
                    }

                    string selectedIndices = indices.ToString();
                    Console.WriteLine("Selected Indices="+selectedIndices);

                    command.CommandText = "SELECT * FROM content WHERE fileIndex IN ("+selectedIndices+")";
                    IDataReader dataReader = command.ExecuteReader();
                    while (dataReader.Read())
                    {
                        string url = Convert.ToString(dataReader["url"]);
                        int width = Convert.ToInt32(dataReader["width"]);
                        int height = Convert.ToInt32(dataReader["height"]);
                        AddItemElement(doc, rootUri, channelNode, url, width, height, durationInSeconds);
                    }
                }

            }

            doc.Save(targetRSSFilePath);
        }

        private static void AddItemElement(XmlDocument doc, string rootUri, XmlNode channelNode, string relativeUrl, int width, int height, int durationInSeconds)
        {
            Uri imageUri = new Uri(rootUri + relativeUrl);
            string absoluteUri = imageUri.AbsoluteUri;

            XmlElement itemElem = doc.CreateElement("item");

            XmlElement titleElem = doc.CreateElement("title");
            XmlText text = doc.CreateTextNode(GetImageTitle(relativeUrl));
            titleElem.AppendChild(text);
            itemElem.AppendChild(titleElem);

            XmlElement guidElem = doc.CreateElement("guid");
            text = doc.CreateTextNode(absoluteUri);
            guidElem.AppendChild(text);
            itemElem.AppendChild(guidElem);

            XmlElement mediaThumbnailElem = doc.CreateElement("media:thumbnail", "http://search.yahoo.com/mrss/");
            mediaThumbnailElem.SetAttribute("url", absoluteUri);
            mediaThumbnailElem.SetAttribute("type", "image/jpeg");
            mediaThumbnailElem.SetAttribute("width", width.ToString());
            mediaThumbnailElem.SetAttribute("height", height.ToString());
            mediaThumbnailElem.SetAttribute("duration", durationInSeconds.ToString());
            itemElem.AppendChild(mediaThumbnailElem);

            XmlElement mediaContentElem = doc.CreateElement("media:content", "http://search.yahoo.com/mrss/");
            mediaContentElem.SetAttribute("url", absoluteUri);
            mediaContentElem.SetAttribute("type", "image/jpeg");
            mediaContentElem.SetAttribute("width", width.ToString());
            mediaContentElem.SetAttribute("height", height.ToString());
            mediaContentElem.SetAttribute("duration", durationInSeconds.ToString());
            itemElem.AppendChild(mediaContentElem);

            channelNode.AppendChild(itemElem);
        }

        private static string GetImageTitle(string relativeUrl)
        {
            // find the second slash from the end
            int index = relativeUrl.LastIndexOf('/');
            if (index > 0)
            {
                // find previous slash
                index = relativeUrl.LastIndexOf('/', index-1);
                if (index >= 0)
                    relativeUrl = relativeUrl.Substring(index + 1);
            }

            // decode URL
            return Uri.UnescapeDataString(relativeUrl);
        }
    }
}
