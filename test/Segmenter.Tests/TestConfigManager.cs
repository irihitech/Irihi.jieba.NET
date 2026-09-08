using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace JiebaNet.Segmenter.Tests;

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
        var tempDir = Path.Combine(Path.GetTempPath(), "jieba-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "dict.txt"), "测试 100\n", Encoding.UTF8);

            ConfigManager.ConfigFileBaseDir = tempDir;
            Assert.That(ConfigManager.HasCustomConfigFileDir, Is.True);
            Assert.That(ConfigManager.MainDictFile, Is.EqualTo(Path.Combine(tempDir, "dict.txt")));

            using (var stream = ConfigManager.OpenResource("dict.txt"))
            {
                Assert.That(stream, Is.InstanceOf<FileStream>());
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    Assert.That(reader.ReadLine(), Is.EqualTo("测试 100"));
                }
            }
        }
        finally
        {
            ConfigManager.ConfigFileBaseDir = null;
            Directory.Delete(tempDir, true);
        }
    }
}