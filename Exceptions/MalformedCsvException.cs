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

using System;
using System.Globalization;
using System.Runtime.Serialization;
using Net.Code.Csv.Resources;

namespace Net.Code.Csv.Exceptions
{
	/// <summary>
	/// Represents the exception that is thrown when a CSV file is malformed.
	/// </summary>
	[Serializable()]
	public class MalformedCsvException 
		: Exception
	{
		#region Fields

		/// <summary>
		/// Contains the message that describes the error.
		/// </summary>
		private string _message;

		/// <summary>
		/// Contains the raw data when the error occured.
		/// </summary>
		private string _rawData;

		/// <summary>
		/// Contains the current field index.
		/// </summary>
		private int _fieldNumber;

		/// <summary>
		/// Contains the current record index.
		/// </summary>
		private long _lineNumber;

		/// <summary>
		/// Contains the current position in the raw data.
		/// </summary>
		private int _columnNumber;

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the MalformedCsvException class.
		/// </summary>
		public MalformedCsvException()
			: this(null, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the MalformedCsvException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public MalformedCsvException(string message)
			: this(message, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the MalformedCsvException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="innerException">The exception that is the cause of the current exception.</param>
		public MalformedCsvException(string message, Exception innerException)
			: base(String.Empty, innerException)
		{
			_message = (message == null ? string.Empty : message);

			_rawData = string.Empty;
			_columnNumber = -1;
			_lineNumber = -1;
			_fieldNumber = -1;
		}

		/// <summary>
		/// Initializes a new instance of the MalformedCsvException class.
		/// </summary>
		/// <param name="rawData">The raw data when the error occured.</param>
		/// <param name="columnNumber">The current position in the raw data.</param>
		/// <param name="lineNumber">The current record index.</param>
		/// <param name="fieldNumber">The current field index.</param>
		public MalformedCsvException(string rawData, int columnNumber, long lineNumber, int fieldNumber)
			: this(rawData, columnNumber, lineNumber, fieldNumber, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the MalformedCsvException class.
		/// </summary>
		/// <param name="rawData">The raw data when the error occured.</param>
		/// <param name="columnNumber">The current position in the raw data.</param>
		/// <param name="lineNumber">The current record index.</param>
		/// <param name="fieldNumber">The current field index.</param>
		/// <param name="innerException">The exception that is the cause of the current exception.</param>
		public MalformedCsvException(string rawData, int columnNumber, long lineNumber, int fieldNumber, Exception innerException)
			: base(String.Empty, innerException)
		{
			_rawData = (rawData == null ? string.Empty : rawData);
			_columnNumber = columnNumber;
			_lineNumber = lineNumber;
			_fieldNumber = fieldNumber;

			_message = String.Format(CultureInfo.InvariantCulture, ExceptionMessage.MalformedCsvException, _lineNumber, _fieldNumber, _columnNumber, _rawData);
		}

		/// <summary>
		/// Initializes a new instance of the MalformedCsvException class with serialized data.
		/// </summary>
		/// <param name="info">The <see cref="T:SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
		/// <param name="context">The <see cref="T:StreamingContext"/> that contains contextual information about the source or destination.</param>
		protected MalformedCsvException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			_message = info.GetString("MyMessage");

			_rawData = info.GetString("RawData");
			_columnNumber = info.GetInt32("ColumnNumber");
			_lineNumber = info.GetInt64("LineNumber");
			_fieldNumber = info.GetInt32("FieldNumber");
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the raw data when the error occured.
		/// </summary>
		/// <value>The raw data when the error occured.</value>
		public string RawData
		{
			get { return _rawData; }
		}

		/// <summary>
		/// Gets the current position in the raw data.
		/// </summary>
		/// <value>The current position in the raw data.</value>
		public int ColumnNumber
		{
			get { return _columnNumber; }
		}

		/// <summary>
		/// Gets the current record index.
		/// </summary>
		/// <value>The current record index.</value>
		public long LineNumber
		{
			get { return _lineNumber; }
		}

		/// <summary>
		/// Gets the current field index.
		/// </summary>
		/// <value>The current record index.</value>
		public int FieldNumber
		{
			get { return _fieldNumber; }
		}

		#endregion

		#region Overrides

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		/// <value>A message that describes the current exception.</value>
		public override string Message
		{
			get { return _message; }
		}

		/// <summary>
		/// When overridden in a derived class, sets the <see cref="T:SerializationInfo"/> with information about the exception.
		/// </summary>
		/// <param name="info">The <see cref="T:SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
		/// <param name="context">The <see cref="T:StreamingContext"/> that contains contextual information about the source or destination.</param>
		public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
		{
			base.GetObjectData(info, context);

			info.AddValue("MyMessage", _message);

			info.AddValue("RawData", _rawData);
			info.AddValue("ColumnNumber", _columnNumber);
			info.AddValue("LineNumber", _lineNumber);
			info.AddValue("FieldNumber", _fieldNumber);
		}

		#endregion
	}
}