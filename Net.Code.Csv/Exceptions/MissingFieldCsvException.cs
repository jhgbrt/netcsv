//	LumenWorks.Framework.IO.Csv.MissingFieldCsvException
//	Copyright (c) 2005 S�bastien Lorion
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
/// Represents the exception that is thrown when a there is a missing field in a record of the CSV file.
/// </summary>
/// <remarks>
/// MissingFieldException would have been a better name, but there is already a <see cref="T:System.MissingFieldException"/>.
/// </remarks>
[Serializable]
public class MissingFieldCsvException
    : MalformedCsvException
{
    /// <summary>
    /// Initializes a new instance of the MissingFieldCsvException class.
    /// </summary>
    /// <param name="rawData">The raw data when the error occured.</param>
    /// <param name="columnNumber">The current position in the raw data.</param>
    /// <param name="lineNumber">The current record index.</param>
    /// <param name="fieldNumber">The current field index.</param>
    internal MissingFieldCsvException(string rawData, Location location, int fieldNumber)
        : base(rawData, location, fieldNumber)
    {
    }

    /// <summary>
    /// Initializes a new instance of the MissingFieldCsvException class with serialized data.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
    protected MissingFieldCsvException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
