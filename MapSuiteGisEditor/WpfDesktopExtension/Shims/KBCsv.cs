using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// Minimal drop-in shim for the Kent.Boogaart.KBCsv types used by CsvFeatureSource.
//
// The original GIS Editor relied on Kent.Boogaart.KBCsv.dll, shipped as a binary in the old
// MapSuite v10 repository. To keep this v14 upgrade self-contained, we provide a lightweight
// replacement that supports the specific API surface used by the extension.
namespace Kent.Boogaart.KBCsv
{
    public sealed class CsvReader : IDisposable
    {
        private readonly string path;
        private readonly Encoding encoding;
        private HeaderRecord headerRecord;

        public CsvReader(string path)
            : this(path, Encoding.UTF8)
        {
        }

        public CsvReader(string path, Encoding encoding)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("CSV path is required.", nameof(path));

            this.path = path;
            this.encoding = encoding ?? Encoding.UTF8;
            ValueSeparator = ',';
        }

        public char ValueSeparator { get; set; }

        // Included for compatibility with external code that may set it.
        public char ValueDelimiter { get; set; }

        public bool PreserveLeadingWhiteSpace { get; set; }

        public bool PreserveTrailingWhiteSpace { get; set; }

        public long RecordNumber { get; private set; }

        public bool HasMoreRecords { get; private set; }

        public HeaderRecord HeaderRecord => headerRecord;

        public HeaderRecord ReadHeaderRecord()
        {
            using (var reader = new StreamReader(path, encoding, detectEncodingFromByteOrderMarks: true))
            {
                var headerLine = reader.ReadLine();
                headerRecord = new HeaderRecord(ParseCsvLine(headerLine, ValueSeparator));
            }

            return headerRecord;
        }

        public IEnumerable<DataRecord> DataRecords
        {
            get
            {
                EnsureHeader();

                using (var reader = new StreamReader(path, encoding, detectEncodingFromByteOrderMarks: true))
                {
                    reader.ReadLine();

                    RecordNumber = 0;
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        RecordNumber++;
                        var values = ParseCsvLine(line, ValueSeparator);
                        yield return new DataRecord(headerRecord, values);
                    }
                }
            }
        }

        public IEnumerable<string[]> DataRecordsAsStrings
        {
            get { return DataRecords.Select(r => r.Values.ToArray()); }
        }

        public ICollection<DataRecord> ReadDataRecords()
        {
            return DataRecords.ToList();
        }

        public ICollection<string[]> ReadDataRecordsAsStrings()
        {
            return DataRecordsAsStrings.ToList();
        }

        public DataRecord ReadDataRecord()
        {
            return DataRecords.FirstOrDefault();
        }

        public string[] ReadDataRecordAsStrings()
        {
            return DataRecordsAsStrings.FirstOrDefault();
        }

        public void Close()
        {
            // No-op for compatibility; enumerators own their StreamReader.
        }

        private void EnsureHeader()
        {
            if (headerRecord == null)
            {
                ReadHeaderRecord();
            }
        }

        private static List<string> ParseCsvLine(string line, char separator)
        {
            var results = new List<string>();
            if (line == null)
            {
                results.Add(string.Empty);
                return results;
            }

            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == separator)
                    {
                        results.Add(sb.ToString());
                        sb.Clear();
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }

            results.Add(sb.ToString());
            return results;
        }

        public void Dispose()
        {
            // Nothing to dispose. The enumerator owns its StreamReader.
        }
    }

    public sealed class HeaderRecord
    {
        public HeaderRecord(IEnumerable<string> values)
        {
            Values = values != null ? new List<string>(values) : new List<string>();
        }

        public List<string> Values { get; }

        public int IndexOf(string columnName)
        {
            return Values.IndexOf(columnName);
        }

        public string this[int index]
        {
            get
            {
                return index >= 0 && index < Values.Count ? Values[index] : string.Empty;
            }
        }
    }

    public sealed class DataRecord
    {
        public DataRecord(HeaderRecord headerRecord, IList<string> values)
        {
            HeaderRecord = headerRecord ?? new HeaderRecord(Array.Empty<string>());
            Values = values != null ? new List<string>(values) : new List<string>();
        }

        public HeaderRecord HeaderRecord { get; }

        public List<string> Values { get; }

        public string this[string columnName]
        {
            get
            {
                if (HeaderRecord == null || string.IsNullOrEmpty(columnName)) return string.Empty;

                int index = HeaderRecord.IndexOf(columnName);
                return index >= 0 && index < Values.Count ? Values[index] : string.Empty;
            }
        }

        public string this[int index]
        {
            get
            {
                return index >= 0 && index < Values.Count ? Values[index] : string.Empty;
            }
        }
    }
}
