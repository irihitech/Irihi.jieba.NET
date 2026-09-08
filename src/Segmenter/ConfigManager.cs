using System;
using System.IO;
using System.Text;

namespace JiebaNet.Segmenter;

public class ConfigManager
{
    public const string ConfigFileDirEnvVar = "JIEBA_CONFIG_FILE_DIR";
    public const string LegacyConfigFileDirEnvVar = "JiebaConfigFileDir";

    private const string EmbeddedResourcePrefix = "JiebaNet.Segmenter.Resources.";

    private static string _customConfigFileBaseDir = null;

    /// <summary>
    /// Custom directory of the dictionary and model files. When it is set (or the
    /// JIEBA_CONFIG_FILE_DIR environment variable is present), data files are loaded
    /// from that directory; otherwise the embedded resources of the assembly are used.
    /// Set it before using any segmentation feature.
    /// </summary>
    public static string ConfigFileBaseDir
    {
        get
        {
            var dir = _customConfigFileBaseDir
                      ?? Environment.GetEnvironmentVariable(ConfigFileDirEnvVar)
                      ?? Environment.GetEnvironmentVariable(LegacyConfigFileDirEnvVar)
                      ?? "Resources";
            if (!Path.IsPathRooted(dir))
            {
                dir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, dir));
            }

            return dir;
        }
        set { _customConfigFileBaseDir = value; }
    }

    /// <summary>
    /// Indicates whether a custom config directory is in effect (via <see cref="ConfigFileBaseDir"/>
    /// or the environment variable), in which case data files are read from disk
    /// instead of the embedded resources.
    /// </summary>
    public static bool HasCustomConfigFileDir
    {
        get
        {
            return _customConfigFileBaseDir != null
                   || Environment.GetEnvironmentVariable(ConfigFileDirEnvVar) != null
                   || Environment.GetEnvironmentVariable(LegacyConfigFileDirEnvVar) != null;
        }
    }

    /// <summary>
    /// Opens the named data file. By default it is read from the embedded resources;
    /// when a custom config directory is in effect, the file is read from that directory.
    /// </summary>
    public static Stream OpenResource(string fileName)
    {
        if (!HasCustomConfigFileDir)
        {
            var stream = typeof(ConfigManager).Assembly.GetManifestResourceStream(EmbeddedResourcePrefix + fileName);
            if (stream != null)
            {
                return stream;
            }
        }

        return File.OpenRead(Path.Combine(ConfigFileBaseDir, fileName));
    }

    /// <summary>
    /// Reads the named data file as UTF-8 text (embedded resources by default, see <see cref="OpenResource"/>).
    /// </summary>
    public static string ReadResourceText(string fileName)
    {
        using (var reader = new StreamReader(OpenResource(fileName), Encoding.UTF8))
        {
            return reader.ReadToEnd();
        }
    }

    public static string MainDictFile
    {
        get { return Path.Combine(ConfigFileBaseDir, "dict.txt"); }
    }

    public static string ProbTransFile
    {
        get { return Path.Combine(ConfigFileBaseDir, "prob_trans.json"); }
    }

    public static string ProbEmitFile
    {
        get { return Path.Combine(ConfigFileBaseDir, "prob_emit.json"); }
    }

    public static string PosProbStartFile
    {
        get { return Path.Combine(ConfigFileBaseDir, "pos_prob_start.json"); }
    }

    public static string PosProbTransFile
    {
        get { return Path.Combine(ConfigFileBaseDir, "pos_prob_trans.json"); }
    }

    public static string PosProbEmitFile
    {
        get { return Path.Combine(ConfigFileBaseDir, "pos_prob_emit.json"); }
    }

    public static string CharStateTabFile
    {
        get { return Path.Combine(ConfigFileBaseDir, "char_state_tab.json"); }
    }

    public static string IdfFile => Path.Combine(ConfigFileBaseDir, "idf.txt");

    public static string StopWordsFile => Path.Combine(ConfigFileBaseDir, "stopwords.txt");
}