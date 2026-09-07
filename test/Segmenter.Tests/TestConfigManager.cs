using System;
using System.IO;
using NUnit.Framework;

namespace JiebaNet.Segmenter.Tests
{
    [TestFixture]
    public class TestConfigManager
    {
        [TestCase]
        public void TestEmbeddedResourcesByDefault()
        {
            Assert.That(ConfigManager.HasCustomConfigFileDir, Is.False);

            using (var stream = ConfigManager.OpenResource("dict.txt"))
            {
                Assert.That(stream, Is.Not.Null);
                Assert.That(stream, Is.Not.InstanceOf<FileStream>());
                using (var reader = new StreamReader(stream))
                {
                    var firstLine = reader.ReadLine();
                    Assert.That(firstLine, Is.Not.Empty);
                }
            }
        }

        [TestCase]
        public void TestCustomDirPrefersFiles()
        {
            var resourcesDir = Path.Combine(AppContext.BaseDirectory, "Resources");
            try
            {
                ConfigManager.ConfigFileBaseDir = resourcesDir;
                Assert.That(ConfigManager.HasCustomConfigFileDir, Is.True);

                using (var stream = ConfigManager.OpenResource("dict.txt"))
                {
                    Assert.That(stream, Is.InstanceOf<FileStream>());
                }

                var text = ConfigManager.ReadResourceText("idf.txt");
                Assert.That(text, Is.Not.Empty);
            }
            finally
            {
                ConfigManager.ConfigFileBaseDir = null;
            }
        }
    }
}
