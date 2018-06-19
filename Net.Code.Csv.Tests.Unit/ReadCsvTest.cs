using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace Net.Code.Csv.Tests.Unit
{ 
    [TestFixture]
    public class ReadCsvTest
    {
        [Test]
        public void FromStream()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Net.Code.Csv.Tests.Unit.test.csv"))
            using (var reader = ReadCsv.FromStream(stream, encoding: Encoding.GetEncoding(1253)))
            {
                reader.Read();
                Assert.AreEqual("Fieldα", reader[0]);
            }
        }
    }
}
