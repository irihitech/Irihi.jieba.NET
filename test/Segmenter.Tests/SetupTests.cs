using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace JiebaNet.Segmenter.Tests
{
    [SetUpFixture]
    public class SetUpClass
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Directory.SetCurrentDirectory(dir);

            // No ConfigFileBaseDir is set on purpose: the test suite exercises the
            // default embedded-resource loading path of the library.
        }

        [OneTimeTearDown]
        public void RunAfterAnyTests()
        {
            Console.WriteLine("Job Done");
        }
    }
}