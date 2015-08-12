//	LumenWorks.Framework.IO.CSV.CsvReader.RecordEnumerator
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
using Net.Code.Csv.Resources;

namespace Net.Code.Csv
{
	public sealed partial class CsvReader
	{
		/// <summary>
		/// Supports a simple iteration over the records of a <see cref="T:CsvReader"/>.
		/// </summary>
		public struct RecordEnumerator
			: IEnumerator<string[]>, IEnumerator
		{
			private CsvReader _reader;
			private long _currentRecordIndex;

			/// <summary>
			/// Initializes a new instance of the <see cref="T:RecordEnumerator"/> class.
			/// </summary>
			/// <param name="reader">The <see cref="T:CsvReader"/> to iterate over.</param>
			/// <exception cref="T:ArgumentNullException">
			///		<paramref name="reader"/> is a <see langword="null"/>.
			/// </exception>
			public RecordEnumerator(CsvReader reader)
			{
				if (reader == null)
					throw new ArgumentNullException(nameof(reader));

				_reader = reader;
				Current = null;
				_currentRecordIndex = reader.CurrentRecordIndex;
			}


			/// <summary>
			/// Gets the current record.
			/// </summary>
			public string[] Current { get; private set; }

		    /// <summary>
			/// Advances the enumerator to the next record of the CSV.
			/// </summary>
			/// <returns><see langword="true"/> if the enumerator was successfully advanced to the next record, <see langword="false"/> if the enumerator has passed the end of the CSV.</returns>
			public bool MoveNext()
			{
				if (_reader.CurrentRecordIndex != _currentRecordIndex)
					throw new InvalidOperationException(ExceptionMessage.EnumerationVersionCheckFailed);

				if (_reader.ReadNextRecord())
				{
					Current = new string[_reader._fieldCount];

					_reader.CopyCurrentRecordTo(Current);
					_currentRecordIndex = _reader.CurrentRecordIndex;

					return true;
				}
				else
				{
					Current = null;
					_currentRecordIndex = _reader.CurrentRecordIndex;

					return false;
				}
			}

			/// <summary>
			/// Sets the enumerator to its initial position, which is before the first record in the CSV.
			/// </summary>
			public void Reset()
			{
				if (_reader.CurrentRecordIndex != _currentRecordIndex)
					throw new InvalidOperationException(ExceptionMessage.EnumerationVersionCheckFailed);

				_reader.MoveTo(-1);

				Current = null;
				_currentRecordIndex = _reader.CurrentRecordIndex;
			}

			/// <summary>
			/// Gets the current record.
			/// </summary>
			object IEnumerator.Current
			{
				get
				{
					if (_reader.CurrentRecordIndex != _currentRecordIndex)
						throw new InvalidOperationException(ExceptionMessage.EnumerationVersionCheckFailed);

					return this.Current;
				}
			}

			/// <summary>
			/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
			/// </summary>
			public void Dispose()
			{
				_reader = null;
				Current = null;
			}
		}
	}
}