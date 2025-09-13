using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace OfficeSensor
{
    public class ReadData : IDisposable
    {
        private StreamReader _reader;
        private bool _disposed = false; // praćenje da Dispose ne bude pozvan više puta

        public ReadData(string csvFilePath)
        {
            // Otvaramo StreamReader i čuvamo ga kao polje
            _reader = new StreamReader(csvFilePath);
        }

        public List<SensorSample> Reader(string logFilePath, int maxRows = 100)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ReadData));

            var invalidRows = new List<string>();
            var sensorSampleList = new List<SensorSample>();

            int rowCount = 0;
            string headerLine = _reader.ReadLine();
            if (headerLine == null)
            {
                Console.WriteLine("CSV je prazan!");
                return sensorSampleList;
            }

            string[] headers = headerLine.Split(',');

            while (!_reader.EndOfStream)
            {
                string line = _reader.ReadLine();
                rowCount++;

                if (rowCount > maxRows)
                {
                    invalidRows.Add(line);
                    continue;
                }

                string[] values = line.Split(',');

                try
                {
                    var sample = new SensorSample();

                    for (int i = 0; i < headers.Length; i++)
                    {
                        string header = headers[i].Trim();
                        string value = values[i].Trim();

                        switch (header.ToLower())
                        {
                            case "volume [mv]":
                                sample.Volume = double.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "relative_humidity [%]":
                                sample.RelativeHumidity = double.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "air_quality [ohms]":
                                sample.AirQuality = double.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "light_level [ohms]":
                                sample.LightLevel = double.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "date time":
                                sample.DateTime = DateTime.Parse(value, CultureInfo.InvariantCulture);
                                break;
                        }
                    }

                    sensorSampleList.Add(sample);
                }
                catch
                {
                    invalidRows.Add(line);
                }
            }

            // Upis nevalidnih redova u log fajl
            /*if (invalidRows.Count > 0)
                File.WriteAllLines(logFilePath, invalidRows);*/
            Console.WriteLine($"{invalidRows.Count}");
            return sensorSampleList;
        }

        // IDisposable implementacija
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // sprečava pozivanje finalizera
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Oslobađanje managed resursa
                    _reader?.Dispose();
                }

                // Oslobađanje unmanaged resursa, ako ih ima (nema u ovom slučaju)

                _disposed = true;
            }
        }

        ~ReadData()
        {
            // Finalizer, samo u slučaju da Dispose nije pozvan
            Dispose(false);
        }
    }
}
