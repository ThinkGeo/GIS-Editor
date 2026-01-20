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
        private List<string> headerRecord;

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

        /// <summary>
        /// Gets or sets the delimiter used between fields.
        /// </summary>
        public char ValueSeparator { get; set; }

        /// <summary>
        /// 1-based record number for the current data record during enumeration.
        /// </summary>
        public long RecordNumber { get; private set; }

        public void ReadHeaderRecord()
        {
            using (var reader = new StreamReader(path, encoding, detectEncodingFromByteOrderMarks: true))
            {
                var headerLine = reader.ReadLine();
                headerRecord = ParseCsvLine(headerLine, ValueSeparator);
            }
        }

        public IEnumerable<DataRecord> DataRecords
        {
            get
            {
                EnsureHeader();

                using (var reader = new StreamReader(path, encoding, detectEncodingFromByteOrderMarks: true))
                {
                    // Skip header
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
                        // Escaped quote
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

    public sealed class DataRecord
    {
        public DataRecord(List<string> headerRecord, List<string> values)
        {
            HeaderRecord = headerRecord ?? new List<string>();
            Values = values ?? new List<string>();
        }

        public List<string> HeaderRecord { get; }

        public List<string> Values { get; }
    }
}
