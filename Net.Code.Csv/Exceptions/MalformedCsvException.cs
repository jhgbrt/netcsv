//	LumenWorks.Framework.IO.Csv.MalformedCsvException
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

using System.Runtime.Serialization;

namespace Net.Code.Csv;

/// <summary>
/// Represents the exception that is thrown when a CSV file is malformed.
/// </summary>
[Serializable]
public class MalformedCsvException
    : Exception
{

    /// <summary>
    /// Initializes a new instance of the MalformedCsvException class.
    /// </summary>
    /// <param name="rawData">The raw data when the error occured.</param>
    /// <param name="columnNumber">The current position in the raw data.</param>
    /// <param name="lineNumber">The current record index.</param>
    /// <param name="fieldNumber">The current field index.</param>
    internal MalformedCsvException(string rawData, Location location, int fieldNumber)
        : base(String.Empty)
    {
        RawData = (rawData ?? string.Empty);
        ColumnNumber = location.Column;
        LineNumber = location.Line;
        FieldNumber = fieldNumber;
        Message = $"The CSV appears to be corrupt on line {LineNumber}, field '{FieldNumber}' at position {ColumnNumber}. Current raw data : '{RawData}'.";
    }

    /// <summary>
    /// Initializes a new instance of the MalformedCsvException class with serialized data.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
    protected MalformedCsvException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        Message = info.GetString("MyMessage");

        RawData = info.GetString("RawData");
        ColumnNumber = info.GetInt32("ColumnNumber");
        LineNumber = info.GetInt64("LineNumber");
        FieldNumber = info.GetInt32("FieldNumber");
    }

    /// <summary>
    /// Gets the raw data when the error occured.
    /// </summary>
    /// <value>The raw data when the error occured.</value>
    public string RawData { get; }

    /// <summary>
    /// Gets the current position in the raw data.
    /// </summary>
    /// <value>The current position in the raw data.</value>
    public int ColumnNumber { get; }

    /// <summary>
    /// Gets the current record index.
    /// </summary>
    /// <value>The current record index.</value>
    public long LineNumber { get; }

    /// <summary>
    /// Gets the current field index.
    /// </summary>
    /// <value>The current record index.</value>
    public int FieldNumber { get; }

    /// <summary>
    /// Gets a message that describes the current exception.
    /// </summary>
    /// <value>A message that describes the current exception.</value>
    public override string Message { get; }

    /// <summary>
    /// When overridden in a derived class, sets the <see cref="SerializationInfo"/> with information about the exception.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
    public override void GetObjectData(
        SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);

        info.AddValue("MyMessage", Message);
        info.AddValue("RawData", RawData);
        info.AddValue("ColumnNumber", ColumnNumber);
        info.AddValue("LineNumber", LineNumber);
        info.AddValue("FieldNumber", FieldNumber);
    }
}
