// Note from Jeroen Haegebaert:
//  The code from LumenWorks was used as an inspiration for this project. I rewrote the entire CSV parsing engine
//  and left the -adjusted- implementation of CsvReader here so the unit tests could be left in place.

//	LumenWorks.Framework.IO.CSV.CsvReader
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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Globalization;
using System.IO;
using Net.Code.Csv.Exceptions;
using Net.Code.Csv.Resources;

namespace Net.Code.Csv
{
    /// <summary>
    /// Represents a reader that provides fast, non-cached, forward-only access to CSV data.  
    /// </summary>
    public partial class CsvReader
        : IDataReader, IEnumerable<string[]>
    {
        /// <summary>
        /// Defines the default buffer size.
        /// </summary>
        public const int DefaultBufferSize = 0x1000;

        /// <summary>
        /// Defines the default delimiter character separating each field.
        /// </summary>
        public const char DefaultDelimiter = ',';

        /// <summary>
        /// Defines the default quote character wrapping every field.
        /// </summary>
        public const char DefaultQuote = '"';

        /// <summary>
        /// Defines the default escape character letting insert quotation characters inside a quoted field.
        /// </summary>
        public const char DefaultEscape = '"';

        /// <summary>
        /// Defines the default comment character indicating that a line is commented out.
        /// </summary>
        public const char DefaultComment = '#';

        private CsvLine _line;
        private readonly IEnumerator<CsvLine> _enumerator;
        private readonly CsvLayout _csvLayout;

        /// <summary>
        /// Contains the <see cref="TextReader"/> pointing to the CSV file.
        /// </summary>
        private CsvParser _parser;

        /// <summary>
        /// Contains the buffer size.
        /// </summary>
        private int _bufferSize;

        /// <summary>
        /// Indicates if the class is initialized.
        /// </summary>
        private bool _initialized;

        /// <summary>
        /// Contains the current record index in the CSV file.
        /// A value of <see cref="Int32.MinValue"/> means that the reader has not been initialized yet.
        /// Otherwise, a negative value means that no record has been read yet.
        /// </summary>
        private long _currentRecordIndex;

        /// <summary>
        /// Contains the array of the field values for the current record.
        /// A null value indicates that the field have not been parsed.
        /// </summary>
        private string[] _fields;

        /// <summary>
        /// Contains the maximum number of fields to retrieve for each record.
        /// </summary>
        private int _fieldCount;

        /// <summary>
        /// Indicates if the end of the reader has been reached.
        /// </summary>
        private bool _eof;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the CsvReader class.
        /// </summary>
        /// <param name="reader">A <see cref="TextReader"/> pointing to the CSV file.</param>
        /// <param name="hasHeaders"><see langword="true"/> if field names are located on the first non commented line, otherwise, <see langword="false"/>.</param>
        /// <exception cref="ArgumentNullException">
        ///		<paramref name="reader"/> is a <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///		Cannot read from <paramref name="reader"/>.
        /// </exception>
        [Obsolete]
        public CsvReader(TextReader reader, bool hasHeaders)
            : this(reader, CsvReader.DefaultBufferSize, new CsvLayout(hasHeaders: hasHeaders), CsvBehaviour.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the CsvReader class.
        /// </summary>
        /// <param name="reader">A <see cref="TextReader"/> pointing to the CSV file.</param>
        /// <param name="hasHeaders"><see langword="true"/> if field names are located on the first non commented line, otherwise, <see langword="false"/>.</param>
        /// <param name="bufferSize">The buffer size in bytes.</param>
        /// <exception cref="ArgumentNullException">
        ///		<paramref name="reader"/> is a <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///		Cannot read from <paramref name="reader"/>.
        /// </exception>
        [Obsolete]
        public CsvReader(TextReader reader, bool hasHeaders, int bufferSize)
            : this(reader, bufferSize, new CsvLayout(hasHeaders: hasHeaders), CsvBehaviour.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the CsvReader class.
        /// </summary>
        /// <param name="reader">A <see cref="TextReader"/> pointing to the CSV file.</param>
        /// <param name="hasHeaders"><see langword="true"/> if field names are located on the first non commented line, otherwise, <see langword="false"/>.</param>
        /// <param name="delimiter">The delimiter character separating each field (default is ',').</param>
        /// <exception cref="ArgumentNullException">
        ///		<paramref name="reader"/> is a <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///		Cannot read from <paramref name="reader"/>.
        /// </exception>
        [Obsolete]
        public CsvReader(TextReader reader, bool hasHeaders, char delimiter)
            : this(reader, DefaultBufferSize, new CsvLayout(hasHeaders: hasHeaders, delimiter: delimiter), CsvBehaviour.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the CsvReader class.
        /// </summary>
        /// <param name="reader">A <see cref="TextReader"/> pointing to the CSV file.</param>
        /// <param name="hasHeaders"><see langword="true"/> if field names are located on the first non commented line, otherwise, <see langword="false"/>.</param>
        /// <param name="delimiter">The delimiter character separating each field (default is ',').</param>
        /// <param name="bufferSize">The buffer size in bytes.</param>
        /// <exception cref="ArgumentNullException">
        ///		<paramref name="reader"/> is a <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///		Cannot read from <paramref name="reader"/>.
        /// </exception>
        [Obsolete]
        public CsvReader(TextReader reader, bool hasHeaders, char delimiter, int bufferSize)
            : this(reader, bufferSize, new CsvLayout(hasHeaders: hasHeaders, delimiter: delimiter), CsvBehaviour.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the CsvReader class.
        /// </summary>
        /// <param name="reader">A <see cref="TextReader"/> pointing to the CSV file.</param>
        /// <param name="hasHeaders"><see langword="true"/> if field names are located on the first non commented line, otherwise, <see langword="false"/>.</param>
        /// <param name="delimiter">The delimiter character separating each field (default is ',').</param>
        /// <param name="quote">The quotation character wrapping every field (default is ''').</param>
        /// <param name="escape">
        /// The escape character letting insert quotation characters inside a quoted field (default is '\').
        /// If no escape character, set to '\0' to gain some performance.
        /// </param>
        /// <param name="comment">The comment character indicating that a line is commented out (default is '#').</param>
        /// <param name="trimmingOptions">Determines which values should be trimmed.</param>
        /// <exception cref="ArgumentNullException">
        ///		<paramref name="reader"/> is a <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///		Cannot read from <paramref name="reader"/>.
        /// </exception>
        [Obsolete]
        public CsvReader(TextReader reader, bool hasHeaders, char delimiter, char quote, char escape, char comment, ValueTrimmingOptions trimmingOptions)
            : this(reader, DefaultBufferSize,
            new CsvLayout(hasHeaders: hasHeaders, delimiter: delimiter, quote: quote, escape: escape, comment: comment), new CsvBehaviour(trimmingOptions: trimmingOptions))
        {
        }

        public CsvReader(TextReader reader,
            int bufferSize = DefaultBufferSize,
            CsvLayout layout = null,
            CsvBehaviour behaviour = null)
        {
            if (layout == null) layout = CsvLayout.Default;
            if (behaviour == null) behaviour = CsvBehaviour.Default;
            _fields = new string[0];

            if (reader == null)
                throw new ArgumentNullException("reader");

            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException("bufferSize", bufferSize, ExceptionMessage.BufferSizeTooSmall);

            _bufferSize = bufferSize;

            if (reader is StreamReader)
            {
                Stream stream = ((StreamReader)reader).BaseStream;

                if (stream.CanSeek)
                {
                    // Handle bad implementations returning 0 or less
                    if (stream.Length > 0)
                        _bufferSize = (int)Math.Min(bufferSize, stream.Length);
                }
            }

            _currentRecordIndex = -1;

            _csvLayout = layout;
            _behaviour = behaviour;
            _parser = new CsvParser(reader, bufferSize, _csvLayout, _behaviour);
            _enumerator = _parser.GetEnumerator();
        }

        #endregion

        #region Properties

        #region Settings

        /// <summary>
        /// Gets the comment character indicating that a line is commented out.
        /// </summary>
        /// <value>The comment character indicating that a line is commented out.</value>
        public char Comment
        {
            get
            {
                return _csvLayout.Comment;
            }
        }

        /// <summary>
        /// Gets the escape character letting insert quotation characters inside a quoted field.
        /// </summary>
        /// <value>The escape character letting insert quotation characters inside a quoted field.</value>
        public char Escape
        {
            get
            {
                return _csvLayout.Escape;
            }
        }

        /// <summary>
        /// Gets the delimiter character separating each field.
        /// </summary>
        /// <value>The delimiter character separating each field.</value>
        public char Delimiter
        {
            get
            {
                return _csvLayout.Delimiter;
            }
        }

        /// <summary>
        /// Gets the quotation character wrapping every field.
        /// </summary>
        /// <value>The quotation character wrapping every field.</value>
        public char Quote
        {
            get
            {
                return _csvLayout.Quote;
            }
        }

        /// <summary>
        /// Indicates if field names are located on the first non commented line.
        /// </summary>
        /// <value><see langword="true"/> if field names are located on the first non commented line, otherwise, <see langword="false"/>.</value>
        public bool HasHeaders
        {
            get
            {
                return _csvLayout.HasHeaders;
            }
        }

        /// <summary>
        /// Indicates if spaces at the start and end of a field are trimmed.
        /// </summary>
        /// <value><see langword="true"/> if spaces at the start and end of a field are trimmed, otherwise, <see langword="false"/>.</value>
        public ValueTrimmingOptions TrimmingOption
        {
            get
            {
                return _behaviour.TrimmingOptions;
            }
        }

        /// <summary>
        /// Gets the buffer size.
        /// </summary>
        public int BufferSize
        {
            get
            {
                return _bufferSize;
            }
        }

        /// <summary>
        /// Gets or sets the default action to take when a parsing error has occured.
        /// </summary>
        /// <value>The default action to take when a parsing error has occured.</value>
        public QuotesInsideQuotedFieldAction DefaultQuotesInsideQuotedFieldAction
        {
            get
            {
                return _behaviour.QuotesInsideQuotedFieldAction;
            }
            set
            {
                _behaviour = new CsvBehaviour(_behaviour.TrimmingOptions, _behaviour.MissingFieldAction, _behaviour.SkipEmptyLines, value);
            }
        }

        /// <summary>
        /// Gets or sets the default header name when it is an empty string or only whitespaces.
        /// The header index will be appended to the specified name.
        /// </summary>
        /// <value>The default header name when it is an empty string or only whitespaces.</value>
        public string DefaultHeaderName { set { _parser.DefaultHeaderName = value; } }

        #endregion

        #region State

        /// <summary>
        /// Gets the maximum number of fields to retrieve for each record.
        /// </summary>
        /// <value>The maximum number of fields to retrieve for each record.</value>
        /// <exception cref="ObjectDisposedException">
        ///	The instance has been disposed of.
        /// </exception>
        public int FieldCount
        {
            get
            {
                EnsureInitialize();
                return _fieldCount;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the current stream position is at the end of the stream.
        /// </summary>
        /// <value><see langword="true"/> if the current stream position is at the end of the stream; otherwise <see langword="false"/>.</value>
        public virtual bool EndOfStream
        {
            get
            {
                return _eof;
            }
        }

        /// <summary>
        /// Gets the field headers.
        /// </summary>
        /// <returns>The field headers or an empty array if headers are not supported.</returns>
        /// <exception cref="System.ObjectDisposedException">
        ///	The instance has been disposed of.
        /// </exception>
        public string[] GetFieldHeaders()
        {
            EnsureInitialize();
            if (_parser.Header == null) return new string[0];
            return _parser.Header.Fields.ToArray();
        }

        /// <summary>
        /// Gets the current record index in the CSV file.
        /// </summary>
        /// <value>The current record index in the CSV file.</value>
        public virtual long CurrentRecordIndex
        {
            get
            {
                return _currentRecordIndex;
            }
        }

        #endregion

        #endregion

        #region Indexers

        /// <summary>
        /// Gets the field with the specified name and record position. <see cref="HasHeaders"/> must be <see langword="true"/>.
        /// </summary>
        /// <value>
        /// The field with the specified name and record position.
        /// </value>
        /// <exception cref="ArgumentNullException">
        ///		<paramref name="field"/> is <see langword="null"/> or an empty string.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///	The CSV does not have headers (<see cref="HasHeaders"/> property is <see langword="false"/>).
        /// </exception>
        /// <exception cref="ArgumentException">
        ///		<paramref name="field"/> not found.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///		Record index must be > 0.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///		Cannot move to a previous record in forward-only mode.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        ///		Cannot read record at <paramref name="record"/>.
        ///	</exception>
        ///	<exception cref="MalformedCsvException">
        ///		The CSV appears to be corrupt at the current position.
        /// </exception>
        /// <exception cref="System.ObjectDisposedException">
        ///	The instance has been disposed of.
        /// </exception>
        public string this[int record, string field]
        {
            get
            {
                if (!MoveTo(record))
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, ExceptionMessage.CannotReadRecordAtIndex, record));

                return this[field];
            }
        }

        /// <summary>
        /// Gets the field at the specified index and record position.
        /// </summary>
        /// <value>
        /// The field at the specified index and record position.
        /// A <see langword="null"/> is returned if the field cannot be found for the record.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">
        ///		<paramref name="field"/> must be included in [0, <see cref="FieldCount"/>[.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///		Record index must be > 0.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///		Cannot move to a previous record in forward-only mode.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        ///		Cannot read record at <paramref name="record"/>.
        /// </exception>
        /// <exception cref="MalformedCsvException">
        ///		The CSV appears to be corrupt at the current position.
        /// </exception>
        /// <exception cref="System.ObjectDisposedException">
        ///	The instance has been disposed of.
        /// </exception>
        public string this[int record, int field]
        {
            get
            {
                if (!MoveTo(record))
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, ExceptionMessage.CannotReadRecordAtIndex, record));

                return this[field];
            }
        }

        /// <summary>
        /// Gets the field with the specified name. <see cref="HasHeaders"/> must be <see langword="true"/>.
        /// </summary>
        /// <value>
        /// The field with the specified name.
        /// </value>
        /// <exception cref="ArgumentNullException">
        ///		<paramref name="field"/> is <see langword="null"/> or an empty string.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///	The CSV does not have headers (<see cref="HasHeaders"/> property is <see langword="false"/>).
        /// </exception>
        /// <exception cref="ArgumentException">
        ///		<paramref name="field"/> not found.
        /// </exception>
        /// <exception cref="MalformedCsvException">
        ///		The CSV appears to be corrupt at the current position.
        /// </exception>
        /// <exception cref="System.ObjectDisposedException">
        ///	The instance has been disposed of.
        /// </exception>
        public string this[string field]
        {
            get
            {
                if (string.IsNullOrEmpty(field))
                    throw new ArgumentNullException("field");

                if (!_csvLayout.HasHeaders)
                    throw new InvalidOperationException(ExceptionMessage.NoHeaders);

                int index = GetFieldIndex(field);

                if (index < 0)
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, ExceptionMessage.FieldHeaderNotFound, field), "field");

                return this[index];
            }
        }

        /// <summary>
        /// Gets the field at the specified index.
        /// </summary>
        /// <value>The field at the specified index.</value>
        /// <exception cref="ArgumentOutOfRangeException">
        ///		<paramref name="field"/> must be included in [0, <see cref="FieldCount"/>[.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///		No record read yet. Call ReadLine() first.
        /// </exception>
        /// <exception cref="MalformedCsvException">
        ///		The CSV appears to be corrupt at the current position.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///	The instance has been disposed of.
        /// </exception>
        public virtual string this[int field]
        {
            get
            {
                if (field < 0 || field >= _fieldCount)
                    throw new ArgumentOutOfRangeException("field", field, string.Format(CultureInfo.InvariantCulture, ExceptionMessage.FieldIndexOutOfRange, field));
                return _fields[field];
            }
        }

        #endregion

        #region Methods

        #region EnsureInitialize

        /// <summary>
        /// Ensures that the reader is initialized.
        /// </summary>
        private void EnsureInitialize()
        {
            if (_initialized)
                return;

            _currentRecordIndex = -1;

            _parser.Initialize();

            _fieldCount = _parser.FieldCount;

            _initialized = true;

        }

        #endregion

        #region GetFieldIndex

        /// <summary>
        /// Gets the field index for the provided header.
        /// </summary>
        /// <param name="header">The header to look for.</param>
        /// <returns>The field index for the provided header. -1 if not found.</returns>
        /// <exception cref="ObjectDisposedException">
        ///	The instance has been disposed of.
        /// </exception>
        public int GetFieldIndex(string header)
        {
            EnsureInitialize();

            int index;

            if (_parser.Header != null && _parser.Header.TryGetIndex(header, out index))
                return index;
            return -1;
        }

        #endregion

        #region CopyCurrentRecordTo

        /// <summary>
        /// Copies the field array of the current record to a one-dimensional array, starting at the beginning of the target array.
        /// </summary>
        /// <param name="array"> The one-dimensional <see cref="Array"/> that is the destination of the fields of the current record.</param>
        /// <exception cref="ArgumentNullException">
        ///		<paramref name="array"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///		The number of fields in the record is greater than the available space from <paramref name="index"/> to the end of <paramref name="array"/>.
        /// </exception>
        public void CopyCurrentRecordTo(string[] array)
        {
            CopyCurrentRecordTo(array, 0);
        }

        /// <summary>
        /// Copies the field array of the current record to a one-dimensional array, starting at the beginning of the target array.
        /// </summary>
        /// <param name="array"> The one-dimensional <see cref="Array"/> that is the destination of the fields of the current record.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="ArgumentNullException">
        ///		<paramref name="array"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///		<paramref name="index"/> is les than zero or is equal to or greater than the length <paramref name="array"/>. 
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///	No current record.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///		The number of fields in the record is greater than the available space from <paramref name="index"/> to the end of <paramref name="array"/>.
        /// </exception>
        public void CopyCurrentRecordTo(string[] array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException("index", index, string.Empty);

            if (_currentRecordIndex < 0 || !_initialized)
                throw new InvalidOperationException(ExceptionMessage.NoCurrentRecord);

            if (array.Length - index < _fieldCount)
                throw new ArgumentException(ExceptionMessage.NotEnoughSpaceInArray, "array");

            for (int i = 0; i < _fieldCount; i++)
            {
                array[index + i] = this[i];
            }
        }

        #endregion

        #region IsWhiteSpace

        /// <summary>
        /// Indicates whether the specified Unicode character is categorized as white space.
        /// </summary>
        /// <param name="c">A Unicode character.</param>
        /// <returns><see langword="true"/> if <paramref name="c"/> is white space; otherwise, <see langword="false"/>.</returns>
        private bool IsWhiteSpace(char c)
        {
            // Handle cases where the delimiter is a whitespace (e.g. tab)
            if (c == _csvLayout.Delimiter)
                return false;
            else
            {
                // See char.IsLatin1(char c) in Reflector
                if (c <= '\x00ff')
                    return (c == ' ' || c == '\t');
                else
                    return (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.SpaceSeparator);
            }
        }

        #endregion

        #region MoveTo

        /// <summary>
        /// Moves to the specified record index.
        /// </summary>
        /// <param name="record">The record index.</param>
        /// <returns><c>true</c> if the operation was successful; otherwise, <c>false</c>.</returns>
        /// <exception cref="ObjectDisposedException">
        ///	The instance has been disposed of.
        /// </exception>
        public virtual bool MoveTo(long record)
        {
            if (record < _currentRecordIndex)
                return false;

            // Get number of record to read

            long offset = record - _currentRecordIndex;

            while (offset > 0)
            {
                if (!ReadNextRecord())
                    return false;

                offset--;
            }

            return true;
        }

        #endregion


        #endregion

        #region ReadLine

        /// <summary>
        /// Fills the buffer with data from the reader.
        /// </summary>
        /// <returns><see langword="true"/> if data was successfully read; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ObjectDisposedException">
        ///	The instance has been disposed of.
        /// </exception>
        private bool ReadLine()
        {
            if (_eof || !_enumerator.MoveNext())
            {
                _eof = true;
                return false;
            }

            _line = _enumerator.Current;
            return true;
        }

        #endregion




        #region ReadNextRecord
        /// <summary>
        /// Reads the next record.
        /// </summary>
        /// <returns><see langword="true"/> if a record has been successfully reads; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ObjectDisposedException">
        ///	The instance has been disposed of.
        /// </exception>
        public bool ReadNextRecord()
        {
            return ReadNextRecord(false);
        }

        /// <summary>
        /// Reads the next record.
        /// </summary>
        /// <param name="skipToNextLine">
        /// Indicates if the reader will skip directly to the next line without parsing the current one. 
        /// To be used when an error occurs.
        /// </param>
        /// <returns><see langword="true"/> if a record has been successfully reads; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ObjectDisposedException">
        ///	The instance has been disposed of.
        /// </exception>
        protected virtual bool ReadNextRecord(bool skipToNextLine)
        {
            EnsureInitialize();

            _line = null;

            if (!ReadLine()) return false;

            _fields = _line.Fields.ToArray();
            _currentRecordIndex++;

            return true;
        }

        #endregion




        #region IDataReader support methods

        /// <summary>
        /// Validates the state of the data reader.
        /// </summary>
        /// <param name="validations">The validations to accomplish.</param>
        /// <exception cref="InvalidOperationException">
        ///	No current record.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///	This operation is invalid when the reader is closed.
        /// </exception>
        private void ValidateDataReader(DataReaderValidations validations)
        {
            if ((validations & DataReaderValidations.IsInitialized) != 0 && !_initialized)
                throw new InvalidOperationException(ExceptionMessage.NoCurrentRecord);

            if ((validations & DataReaderValidations.IsNotClosed) != 0 && _isDisposed)
                throw new InvalidOperationException(ExceptionMessage.ReaderClosed);
        }

        /// <summary>
        /// Copy the value of the specified field to an array.
        /// </summary>
        /// <param name="field">The index of the field.</param>
        /// <param name="fieldOffset">The offset in the field value.</param>
        /// <param name="destinationArray">The destination array where the field value will be copied.</param>
        /// <param name="destinationOffset">The destination array offset.</param>
        /// <param name="length">The number of characters to copy from the field value.</param>
        /// <returns></returns>
        private long CopyFieldToArray(int field, long fieldOffset, Array destinationArray, int destinationOffset, int length)
        {
            EnsureInitialize();

            if (field < 0 || field >= _fieldCount)
                throw new ArgumentOutOfRangeException("field", field, string.Format(CultureInfo.InvariantCulture, ExceptionMessage.FieldIndexOutOfRange, field));

            if (fieldOffset < 0 || fieldOffset >= int.MaxValue)
                throw new ArgumentOutOfRangeException("fieldOffset");

            // Array.Copy(...) will do the remaining argument checks

            if (length == 0)
                return 0;

            string value = this[field];

            if (value == null)
                value = string.Empty;

            Debug.Assert(fieldOffset < int.MaxValue);

            Debug.Assert(destinationArray.GetType() == typeof(char[]) || destinationArray.GetType() == typeof(byte[]));

            if (destinationArray.GetType() == typeof(char[]))
                Array.Copy(value.ToCharArray((int)fieldOffset, length), 0, destinationArray, destinationOffset, length);
            else
            {
                char[] chars = value.ToCharArray((int)fieldOffset, length);
                byte[] source = new byte[chars.Length];

                for (int i = 0; i < chars.Length; i++)
                    source[i] = Convert.ToByte(chars[i]);

                Array.Copy(source, 0, destinationArray, destinationOffset, length);
            }

            return length;
        }

        #endregion

        #region IDataReader Members

        int IDataReader.RecordsAffected
        {
            get
            {
                // For SELECT statements, -1 must be returned.
                return -1;
            }
        }

        bool IDataReader.IsClosed
        {
            get
            {
                return _eof;
            }
        }

        bool IDataReader.NextResult()
        {
            ValidateDataReader(DataReaderValidations.IsNotClosed);

            return false;
        }

        void IDataReader.Close()
        {
            Dispose();
        }

        bool IDataReader.Read()
        {
            ValidateDataReader(DataReaderValidations.IsNotClosed);

            return ReadNextRecord();
        }

        int IDataReader.Depth
        {
            get
            {
                ValidateDataReader(DataReaderValidations.IsNotClosed);

                return 0;
            }
        }

        DataTable IDataReader.GetSchemaTable()
        {
            EnsureInitialize();
            ValidateDataReader(DataReaderValidations.IsNotClosed);

            DataTable schema = new DataTable("SchemaTable");
            schema.Locale = CultureInfo.InvariantCulture;
            schema.MinimumCapacity = _fieldCount;

            schema.Columns.Add(SchemaTableColumn.AllowDBNull, typeof(bool)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.BaseColumnName, typeof(string)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.BaseSchemaName, typeof(string)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.BaseTableName, typeof(string)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.ColumnName, typeof(string)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.ColumnOrdinal, typeof(int)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.ColumnSize, typeof(int)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.DataType, typeof(object)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.IsAliased, typeof(bool)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.IsExpression, typeof(bool)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.IsKey, typeof(bool)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.IsLong, typeof(bool)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.IsUnique, typeof(bool)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.NumericPrecision, typeof(short)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.NumericScale, typeof(short)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.ProviderType, typeof(int)).ReadOnly = true;

            schema.Columns.Add(SchemaTableOptionalColumn.BaseCatalogName, typeof(string)).ReadOnly = true;
            schema.Columns.Add(SchemaTableOptionalColumn.BaseServerName, typeof(string)).ReadOnly = true;
            schema.Columns.Add(SchemaTableOptionalColumn.IsAutoIncrement, typeof(bool)).ReadOnly = true;
            schema.Columns.Add(SchemaTableOptionalColumn.IsHidden, typeof(bool)).ReadOnly = true;
            schema.Columns.Add(SchemaTableOptionalColumn.IsReadOnly, typeof(bool)).ReadOnly = true;
            schema.Columns.Add(SchemaTableOptionalColumn.IsRowVersion, typeof(bool)).ReadOnly = true;

            string[] columnNames;

            if (_csvLayout.HasHeaders)
                columnNames = _parser.Header.Fields;
            else
            {
                columnNames = new string[_fieldCount];

                for (int i = 0; i < _fieldCount; i++)
                    columnNames[i] = "Column" + i.ToString(CultureInfo.InvariantCulture);
            }

            // null marks columns that will change for each row
            object[] schemaRow = new object[] { 
					true,					// 00- AllowDBNull
					null,					// 01- BaseColumnName
					string.Empty,			// 02- BaseSchemaName
					string.Empty,			// 03- BaseTableName
					null,					// 04- ColumnName
					null,					// 05- ColumnOrdinal
					int.MaxValue,			// 06- ColumnSize
					typeof(string),			// 07- DataType
					false,					// 08- IsAliased
					false,					// 09- IsExpression
					false,					// 10- IsKey
					false,					// 11- IsLong
					false,					// 12- IsUnique
					DBNull.Value,			// 13- NumericPrecision
					DBNull.Value,			// 14- NumericScale
					(int) DbType.String,	// 15- ProviderType

					string.Empty,			// 16- BaseCatalogName
					string.Empty,			// 17- BaseServerName
					false,					// 18- IsAutoIncrement
					false,					// 19- IsHidden
					true,					// 20- IsReadOnly
					false					// 21- IsRowVersion
			  };

            for (int i = 0; i < columnNames.Length; i++)
            {
                schemaRow[1] = columnNames[i]; // Base column name
                schemaRow[4] = columnNames[i]; // Column name
                schemaRow[5] = i; // Column ordinal

                schema.Rows.Add(schemaRow);
            }

            return schema;
        }

        #endregion

        #region IDataRecord Members

        int IDataRecord.GetInt32(int i)
        {
            ValidateDataReader(DataReaderValidations.IsInitialized | DataReaderValidations.IsNotClosed);

            string value = this[i];

            return Int32.Parse(value ?? string.Empty, CultureInfo.CurrentCulture);
        }

        object IDataRecord.this[string name]
        {
            get
            {
                ValidateDataReader(DataReaderValidations.IsInitialized | DataReaderValidations.IsNotClosed);
                return this[name];
            }
        }

        object IDataRecord.this[int i]
        {
            get
            {
                ValidateDataReader(DataReaderValidations.IsInitialized | DataReaderValidations.IsNotClosed);
                return this[i];
            }
        }

        object IDataRecord.GetValue(int i)
        {
            ValidateDataReader(DataReaderValidations.IsInitialized | DataReaderValidations.IsNotClosed);

            if (((IDataRecord)this).IsDBNull(i))
                return DBNull.Value;
            else
                return this[i];
        }

        bool IDataRecord.IsDBNull(int i)
        {
            ValidateDataReader(DataReaderValidations.IsInitialized | DataReaderValidations.IsNotClosed);
            return (string.IsNullOrEmpty(this[i]));
        }

        long IDataRecord.GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            ValidateDataReader(DataReaderValidations.IsInitialized | DataReaderValidations.IsNotClosed);

            return CopyFieldToArray(i, fieldOffset, buffer, bufferoffset, length);
        }

        byte IDataRecord.GetByte(int i)
        {
            ValidateDataReader(DataReaderValidations.IsInitialized | DataReaderValidations.IsNotClosed);
            return Byte.Parse(this[i], CultureInfo.CurrentCulture);
        }

        Type IDataRecord.GetFieldType(int i)
        {
            EnsureInitialize();
            ValidateDataReader(DataReaderValidations.IsInitialized | DataReaderValidations.IsNotClosed);

            if (i < 0 || i >= _fieldCount)
                throw new ArgumentOutOfRangeException("i", i, string.Format(CultureInfo.InvariantCulture, ExceptionMessage.FieldIndexOutOfRange, i));

            return typeof(string);
        }

        decimal IDataRecord.GetDecimal(int i)
        {
            ValidateDataReader(DataReaderValidations.IsInitialized | DataReaderValidations.IsNotClosed);
            return Decimal.Parse(this[i], CultureInfo.CurrentCulture);
        }

        int IDataRecord.GetValues(object[] values)
        {
            ValidateDataReader(DataReaderValidations.IsInitialized | DataReaderValidations.IsNotClosed);

            IDataRecord record = this;

            for (int i = 0; i < _fieldCount; i++)
                values[i] = record.GetValue(i);

            return _fieldCount;
        }

        string IDataRecord.GetName(int i)
        {
            EnsureInitialize();
            ValidateDataReader(DataReaderValidations.IsNotClosed);

            if (i < 0 || i >= _fieldCount)
                throw new ArgumentOutOfRangeException("i", i, string.Format(CultureInfo.InvariantCulture, ExceptionMessage.FieldIndexOutOfRange, i));

            if (_csvLayout.HasHeaders)
                return _parser.Header.Fields[i];
            else
                return "Column" + i.ToString(CultureInfo.InvariantCulture);
        }

        long IDataRecord.GetInt64(int i)
        {
            ValidateDataReader(DataReaderValidations.IsInitialized | DataReaderValidations.IsNotClosed);
            return Int64.Parse(this[i], CultureInfo.CurrentCulture);
        }

        double IDataRecord.GetDouble(int i)
        {
            ValidateDataReader(DataReaderValidations.IsInitialized | DataReaderValidations.IsNotClosed);
            return Double.Parse(this[i], CultureInfo.CurrentCulture);
        }

        bool IDataRecord.GetBoolean(int i)
        {
            ValidateDataReader(DataReaderValidations.IsInitialized | DataReaderValidations.IsNotClosed);

            string value = this[i];

            int result;

            if (Int32.TryParse(value, out result))
                return (result != 0);
            else
                return Boolean.Parse(value);
        }

        Guid IDataRecord.GetGuid(int i)
        {
            ValidateDataReader(DataReaderValidations.IsInitialized | DataReaderValidations.IsNotClosed);
            return new Guid(this[i]);
        }

        DateTime IDataRecord.GetDateTime(int i)
        {
            ValidateDataReader(DataReaderValidations.IsInitialized | DataReaderValidations.IsNotClosed);
            return DateTime.Parse(this[i], CultureInfo.CurrentCulture);
        }

        int IDataRecord.GetOrdinal(string name)
        {
            EnsureInitialize();
            ValidateDataReader(DataReaderValidations.IsNotClosed);

            int index;

            if (!_parser.Header.TryGetIndex(name, out index))
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, ExceptionMessage.FieldHeaderNotFound, name), "name");

            return index;
        }

        string IDataRecord.GetDataTypeName(int i)
        {
            ValidateDataReader(DataReaderValidations.IsInitialized | DataReaderValidations.IsNotClosed);
            return typeof(string).FullName;
        }

        float IDataRecord.GetFloat(int i)
        {
            ValidateDataReader(DataReaderValidations.IsInitialized | DataReaderValidations.IsNotClosed);
            return Single.Parse(this[i], CultureInfo.CurrentCulture);
        }

        IDataReader IDataRecord.GetData(int i)
        {
            ValidateDataReader(DataReaderValidations.IsInitialized | DataReaderValidations.IsNotClosed);

            if (i == 0)
                return this;
            else
                return null;
        }

        long IDataRecord.GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            ValidateDataReader(DataReaderValidations.IsInitialized | DataReaderValidations.IsNotClosed);

            return CopyFieldToArray(i, fieldoffset, buffer, bufferoffset, length);
        }

        string IDataRecord.GetString(int i)
        {
            ValidateDataReader(DataReaderValidations.IsInitialized | DataReaderValidations.IsNotClosed);
            return this[i];
        }

        char IDataRecord.GetChar(int i)
        {
            ValidateDataReader(DataReaderValidations.IsInitialized | DataReaderValidations.IsNotClosed);
            return Char.Parse(this[i]);
        }

        short IDataRecord.GetInt16(int i)
        {
            ValidateDataReader(DataReaderValidations.IsInitialized | DataReaderValidations.IsNotClosed);
            return Int16.Parse(this[i], CultureInfo.CurrentCulture);
        }

        #endregion

        #region IEnumerable<string[]> Members

        /// <summary>
        /// Returns an <see cref="RecordEnumerator"/>  that can iterate through CSV records.
        /// </summary>
        /// <returns>An <see cref="RecordEnumerator"/>  that can iterate through CSV records.</returns>
        /// <exception cref="ObjectDisposedException">
        ///	The instance has been disposed of.
        /// </exception>
        public RecordEnumerator GetEnumerator()
        {
            return new RecordEnumerator(this);
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator"/>  that can iterate through CSV records.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/>  that can iterate through CSV records.</returns>
        /// <exception cref="ObjectDisposedException">
        ///	The instance has been disposed of.
        /// </exception>
        IEnumerator<string[]> IEnumerable<string[]>.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an <see cref="System.Collections.IEnumerator"/>  that can iterate through CSV records.
        /// </summary>
        /// <returns>An <see cref="System.Collections.IEnumerator"/>  that can iterate through CSV records.</returns>
        /// <exception cref="ObjectDisposedException">
        ///	The instance has been disposed of.
        /// </exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region IDisposable members

        private bool _isDisposed = false;
        private CsvBehaviour _behaviour;

        public void Dispose()
        {
            if (_isDisposed) return;
            _parser.Dispose();
            _parser = null;
            _eof = true;
            _isDisposed = true;
        }

        #endregion
    }
}