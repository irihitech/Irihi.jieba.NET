using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JiebaNet.Segmenter;

namespace JiebaNet.Analyser
{
    public class IdfLoader
    {
        internal string IdfFilePath { get; set; }
        internal IDictionary<string, double> IdfFreq { get; set; }
        internal double MedianIdf { get; set; }

        /// <summary>
        /// Creates a loader and loads the built-in idf file from the embedded resources.
        /// </summary>
        public IdfLoader()
        {
            IdfFilePath = string.Empty;
            IdfFreq = new Dictionary<string, double>();
            MedianIdf = 0.0;
            LoadFrom(ConfigManager.OpenResource("idf.txt"));
        }

        /// <summary>
        /// Creates a loader and loads idf values from the given file.
        /// </summary>
        public IdfLoader(string idfPath)
        {
            IdfFilePath = string.Empty;
            IdfFreq = new Dictionary<string, double>();
            MedianIdf = 0.0;
            if (!string.IsNullOrWhiteSpace(idfPath))
            {
                SetNewPath(idfPath);
            }
        }

        public void SetNewPath(string newIdfPath)
        {
            var idfPath = Path.GetFullPath(newIdfPath);
            if (IdfFilePath != idfPath)
            {
                IdfFilePath = idfPath;
                LoadFrom(File.OpenRead(idfPath));
            }
        }

        private void LoadFrom(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                IdfFreq = new Dictionary<string, double>();
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var parts = line.Trim().Split(' ');
                    var word = parts[0];
                    var freq = double.Parse(parts[1]);
                    IdfFreq[word] = freq;
                }

                MedianIdf = IdfFreq.Values.OrderBy(v => v).ToList()[IdfFreq.Count / 2];
            }
        }
    }
}
