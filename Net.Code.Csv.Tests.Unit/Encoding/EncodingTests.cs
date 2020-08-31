using System;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace Net.Code.Csv.Tests.Unit
{
    [TestFixture]
    public class EncodingTests
    {
        [Test]
        public void Utf8()
        {
            Test("utf8", Encoding.UTF8);
        }
        [Test]
        public void Unicode()
        {
            Test("unicode", Encoding.Unicode);
        }
        [Test]
        public void UnicodeBigEndian()
        {
            Test("unicodebigendian", Encoding.BigEndianUnicode);
        }
        [Test]
        public void Ansi()
        {
            Test("ansi", Encoding.GetEncoding(1252));
        }
        [Test]
        public void Utf7()
        {
            Test("utf7", Encoding.UTF7);
        }

        private static void Test(string encodingName, Encoding encoding)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Net.Code.Csv.Tests.Unit.Encoding." + encodingName + ".txt";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream, encoding))
            {
                string result = reader.ReadToEnd();
                Assert.AreEqual("H1;H2\r\nä û;België", result);
                Console.WriteLine(result);
            }
        }
    }
}
