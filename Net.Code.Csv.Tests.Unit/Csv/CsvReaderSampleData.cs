//	LumenWorks.Framework.Tests.Unit.IO.CSV.CsvReaderSampleData
//	Copyright (c) 2005 Sébastien Lorion
//
//	MIT license (http://en.wikipedia.org/wiki/MIT_License)
//
//	Permission is hereby granted, free of charge, to any person obtaining a copy
//	of this software and associated documentation files (the "Software"), to deal
//	in the Software without restriction, including without limitation the rights 
//	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
//	of the Software, and to permit persons to whom the Software is furnished to do so, 
//	subject to the following conditions:
//
//	The above copyright notice and this permission notice shall be included in all 
//	copies or substantial portions of the Software.
//
//	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
//	INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
//	PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE 
//	FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
//	ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Data;

namespace Net.Code.Csv.Tests.Unit.IO.Csv;

public class CsvReaderSampleData
{
    #region Sample data

    public const int SampleData1RecordCount = 6;
    public const int SampleData1FieldCount = 6;
    public readonly static string[] SampleData1Headers =
    {
            SampleData1Header0,
            SampleData1Header1,
            SampleData1Header2,
            SampleData1Header3,
            SampleData1Header4,
            SampleData1Header5,
        };

    public const string SampleData1Header0 = "First Name";
    public const string SampleData1Header1 = "Last Name";
    public const string SampleData1Header2 = "Address";
    public const string SampleData1Header3 = "City";
    public const string SampleData1Header4 = "State";
    public const string SampleData1Header5 = "Zip Code";

    public const string SampleData1 = """"
        # This is a comment
        "First Name", "Last Name", Address, City, State, "Zip Code"	
        John,Doe,120 jefferson st.,Riverside, NJ, 08075
        Jack,McGinnis,220 hobo Av.,Phila	, PA,09119
        "John ""Da Man""",Repici,120 Jefferson St.,Riverside, NJ,08075

        # This is a comment
        Stephen,Tyler,"7452 Terrace ""At the Plaza"" road",SomeTown,SD, 91234
        ,Blankman,,SomeTown, SD, 00298
        "Joan ""the bone"", Anne",Jet,"9th, at Terrace plc",Desert City,CO,00123
        """";

    public static readonly CsvSchema SampleData1Schema = new CsvSchemaBuilder()
        .AddString("First Name")
        .AddString("Last Name")
        .AddString("Address")
        .AddString("City")
        .AddString("State")
        .AddString("Zip Code")
        .Schema;

    public static readonly CsvSchema SampleTypedData1Schema = new CsvSchemaBuilder(CultureInfo.InvariantCulture)
        .AddBoolean(typeof(bool).FullName)
        .AddDateTime(typeof(DateTime).FullName, "yyyy-MM-dd")
        .AddSingle(typeof(float).FullName)
        .AddDouble(typeof(double).FullName)
        .AddDecimal(typeof(decimal).FullName)
        .AddSByte(typeof(sbyte).FullName)
        .AddInt16(typeof(short).FullName)
        .AddInt32(typeof(int).FullName)
        .AddInt64(typeof(long).FullName)
        .AddByte(typeof(byte).FullName)
        .AddUInt16(typeof(ushort).FullName)
        .AddUInt32(typeof(uint).FullName)
        .AddUInt64(typeof(ulong).FullName)
        .AddChar(typeof(char).FullName)
        .AddString(typeof(string).FullName)
        .AddGuid(typeof(Guid).FullName)
        .Add(typeof(System.DBNull).FullName, s => DBNull.Value, true)
        .Add(typeof(System.Byte[]).FullName, s => Convert.FromBase64String(s), true)
        .Schema;

    public const string SampleTypedData1 = """
        System.Boolean,System.DateTime,System.Single,System.Double,System.Decimal,System.SByte,System.Int16,System.Int32,System.Int64,System.Byte,System.UInt16,System.UInt32,System.UInt64,System.Char,System.String,System.Guid,System.DBNull,System.Byte[]
        "true","2001-11-15","1","1.1","1.10","1","1","1","1","1","1","1","1","a","abc","{11111111-1111-1111-1111-111111111111}","","AQIDBAUGBwgJ"
        """;

    #endregion

    #region Sample data utility methods


    public static void CheckSampleData1(IDataReader csv, bool hasHeaders, bool nullIsEmpty)
    {
        Assert.AreEqual(CsvReaderSampleData.SampleData1FieldCount, csv.FieldCount);

        if (hasHeaders)
        {
            Assert.AreEqual(0, csv.GetOrdinal(SampleData1Header0));
            Assert.AreEqual(1, csv.GetOrdinal(SampleData1Header1));
            Assert.AreEqual(2, csv.GetOrdinal(SampleData1Header2));
            Assert.AreEqual(3, csv.GetOrdinal(SampleData1Header3));
            Assert.AreEqual(4, csv.GetOrdinal(SampleData1Header4));
            Assert.AreEqual(5, csv.GetOrdinal(SampleData1Header5));
        }

        long recordCount = 0;
        while (csv.Read())
        {
            CheckSampleData1(recordCount, csv, nullIsEmpty, hasHeaders);
            recordCount++;
        }

        if (hasHeaders)
            Assert.AreEqual(CsvReaderSampleData.SampleData1RecordCount, recordCount);
        else
            Assert.AreEqual(CsvReaderSampleData.SampleData1RecordCount + 1, recordCount);
    }

    public static void CheckSampleData1(long recordIndex, IDataReader csv, bool nullIsEmpty, bool hasHeaders)
    {
        string[] fields = new string[6];
        csv.GetValues(fields);

        CheckSampleData1(hasHeaders, recordIndex, fields, 0, nullIsEmpty);
    }
    public static void CheckSampleData1(bool hasHeaders, long recordIndex, string[] fields, bool nullIsEmpty = false)
    {
        CheckSampleData1(hasHeaders, recordIndex, fields, 0, nullIsEmpty);
    }

    public static void CheckSampleData1(bool hasHeaders, long recordIndex, string[] fields, int startIndex, bool nullIsEmpty = false)
    {
        Assert.IsTrue(fields.Length - startIndex >= 6);

        long index = recordIndex;

        if (hasHeaders)
            index++;

        switch (index)
        {
            case 0:
                Assert.AreEqual(SampleData1Header0, fields[startIndex]);
                Assert.AreEqual(SampleData1Header1, fields[startIndex + 1]);
                Assert.AreEqual(SampleData1Header2, fields[startIndex + 2]);
                Assert.AreEqual(SampleData1Header3, fields[startIndex + 3]);
                Assert.AreEqual(SampleData1Header4, fields[startIndex + 4]);
                Assert.AreEqual(SampleData1Header5, fields[startIndex + 5]);
                break;

            case 1:
                Assert.AreEqual("John", fields[startIndex]);
                Assert.AreEqual("Doe", fields[startIndex + 1]);
                Assert.AreEqual("120 jefferson st.", fields[startIndex + 2]);
                Assert.AreEqual("Riverside", fields[startIndex + 3]);
                Assert.AreEqual("NJ", fields[startIndex + 4]);
                Assert.AreEqual("08075", fields[startIndex + 5]);
                break;

            case 2:
                Assert.AreEqual("Jack", fields[startIndex]);
                Assert.AreEqual("McGinnis", fields[startIndex + 1]);
                Assert.AreEqual("220 hobo Av.", fields[startIndex + 2]);
                Assert.AreEqual("Phila", fields[startIndex + 3]);
                Assert.AreEqual("PA", fields[startIndex + 4]);
                Assert.AreEqual("09119", fields[startIndex + 5]);
                break;

            case 3:
                Assert.AreEqual(@"John ""Da Man""", fields[startIndex]);
                Assert.AreEqual("Repici", fields[startIndex + 1]);
                Assert.AreEqual("120 Jefferson St.", fields[startIndex + 2]);
                Assert.AreEqual("Riverside", fields[startIndex + 3]);
                Assert.AreEqual("NJ", fields[startIndex + 4]);
                Assert.AreEqual("08075", fields[startIndex + 5]);
                break;

            case 4:
                Assert.AreEqual("Stephen", fields[startIndex]);
                Assert.AreEqual("Tyler", fields[startIndex + 1]);
                Assert.AreEqual(@"7452 Terrace ""At the Plaza"" road", fields[startIndex + 2]);
                Assert.AreEqual("SomeTown", fields[startIndex + 3]);
                Assert.AreEqual("SD", fields[startIndex + 4]);
                Assert.AreEqual("91234", fields[startIndex + 5]);
                break;

            case 5:
                if (nullIsEmpty) Assert.AreEqual(string.Empty, fields[startIndex]);
                else Assert.IsNull(fields[startIndex]);
                Assert.AreEqual("Blankman", fields[startIndex + 1]);
                if (nullIsEmpty) Assert.AreEqual(string.Empty, fields[startIndex + 2]);
                else Assert.IsNull(fields[startIndex + 2]);
                Assert.AreEqual("SomeTown", fields[startIndex + 3]);
                Assert.AreEqual("SD", fields[startIndex + 4]);
                Assert.AreEqual("00298", fields[startIndex + 5]);
                break;

            case 6:
                Assert.AreEqual(@"Joan ""the bone"", Anne", fields[startIndex]);
                Assert.AreEqual("Jet", fields[startIndex + 1]);
                Assert.AreEqual("9th, at Terrace plc", fields[startIndex + 2]);
                Assert.AreEqual("Desert City", fields[startIndex + 3]);
                Assert.AreEqual("CO", fields[startIndex + 4]);
                Assert.AreEqual("00123", fields[startIndex + 5]);
                break;

            default:
                throw new IndexOutOfRangeException(string.Format("Specified recordIndex is '{0}'. Possible range is [0, 5].", recordIndex));
        }
    }

    #endregion
}
