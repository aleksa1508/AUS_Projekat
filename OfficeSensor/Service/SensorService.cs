using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class SensorService : ISensor, IDisposable
    {
        public static bool sessionActive = false;

        private bool disposed = false;
        StreamWriter measurementsWriter;
        StreamWriter rejestSampleWriter;
        public ServiceResponse EndSession()
        {
            try
            {
                sessionActive = false;
                measurementsWriter?.Close();
                rejestSampleWriter?.Close();

                

                return new ServiceResponse
                {
                    ServiceType = ServiceType.ACK,
                    ServiceStatus = ServiceStatus.COMPLETED,
                    Message = "Sesija je zavrsena"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    ServiceType = ServiceType.ACK,
                    ServiceStatus = ServiceStatus.COMPLETED,
                    Message = ex.Message,
                };
            }
        }


        public ServiceResponse PushSample(SensorSample sample)
        {
            try
            {
                if (!sessionActive)
                    throw new FaultException<DataFormatFault>(
                        new DataFormatFault { Message = "Sesija nije aktivna", Field = "Session" },
                        new FaultReason("Sesija jos nije aktivna"));

                ValidateSensorSampleData(sample);

                measurementsWriter.WriteLine($"{sample.Volume},{sample.RelativeHumidity},{sample.AirQuality},{sample.LightLevel},{sample.DateTime}");
                measurementsWriter.Flush();

                return new ServiceResponse
                {
                    ServiceType = ServiceType.ACK,
                    ServiceStatus = ServiceStatus.IN_PROGRESS,
                    Message = "Uzorak uspesno primljen"
                };
            }
            catch (FaultException<ValidationFault> ex)
            {
                //WriteRejactSample(sample, ex.Detail.Message); // belezi u reject CSV
                return new ServiceResponse
                {
                    ServiceType = ServiceType.NACK,
                    ServiceStatus = ServiceStatus.IN_PROGRESS,
                    Message = ex.Detail.Message
                };
            }
            catch (FaultException<DataFormatFault> ex)
            {
               // WriteRejactSample(sample, ex.Detail.Message);
                return new ServiceResponse
                {
                    ServiceType = ServiceType.NACK,
                    ServiceStatus = ServiceStatus.IN_PROGRESS,
                    Message = ex.Detail.Message
                };
            }
            catch (Exception ex)
            {
               // WriteRejactSample(sample, ex.Message);
                return new ServiceResponse
                {
                    ServiceType = ServiceType.NACK,
                    ServiceStatus = ServiceStatus.IN_PROGRESS,
                    Message = $"Neocekivana greska: {ex.Message}"
                };
            }
        }

        public ServiceResponse StartStession(SessionMetaData metaData)
        {
            Console.WriteLine($"STAAAAAAAAAAAAART");
            try
            {
                Console.WriteLine($"Sesija je započeta");
                sessionActive = true;

                string measurementsPath = "measurements_session.csv";
                string rejectsPath = "rejects.csv";
                measurementsWriter = new StreamWriter(measurementsPath);
                rejestSampleWriter = new StreamWriter(rejectsPath);

                measurementsWriter.WriteLine("Volume,RelativeHumidity,AirQuality,LightLevel,DateTime");
                rejestSampleWriter.WriteLine("Volume,RelativeHumidity,AirQuality,LightLevel,DateTime,Reason rejact");

               /* measurementsWriter = new StreamWriter(new FileStream(measurementsPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    AutoFlush = true
                };
                measurementsWriter.WriteLine("Volume,RelativeHumidity,AirQuality,LightLevel,DateTime");

                rejestSampleWriter = new StreamWriter(new FileStream(rejectsPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    AutoFlush = true
                };
                rejestSampleWriter.WriteLine("Volume,RelativeHumidity,AirQuality,LightLevel,DateTime,Reason");*/

                return new ServiceResponse
                {
                    ServiceType = ServiceType.ACK,
                    ServiceStatus = ServiceStatus.IN_PROGRESS,
                };
            }
            catch (Exception ex)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault
                    {
                        Message = "Greška pri pokretanju sesije",
                        Details = ex.Message,
                        Field = "Session"
                    },
                    new FaultReason("Greška se desila pri pokretanju sesije")
                );
            }
        }

        
        public void WriteRejactSample(SensorSample sample, string reason = "")
        {
            if (rejestSampleWriter == null)
            {
                Console.WriteLine("rejestSampleWriter nije inicijalizovan, uzorak nije upisan u rejects.csv");
                return;
            }

            if (sample == null)
            {
                Console.WriteLine("Sample je null, nije moguce zapisati reject");
                return;
            }

           
                rejestSampleWriter?.WriteLine($"{sample?.Volume.ToString(CultureInfo.InvariantCulture)},{sample?.RelativeHumidity.ToString(CultureInfo.InvariantCulture)},{sample?.AirQuality.ToString(CultureInfo.InvariantCulture)},{sample?.LightLevel.ToString(CultureInfo.InvariantCulture)},{sample?.DateTime.ToString("dd-MM-yyyy HH:mm:ss")},{reason}");
                rejestSampleWriter?.Flush();
           
            

            /*rejestSampleWriter.WriteLine(line);
            rejestSampleWriter.Flush();*/
        }
        private void ValidateSensorSampleData(SensorSample sample)
        {
            if (sample == null)
            {
                WriteRejactSample(sample, "SensorSample objekat je null");
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault
                    {
                        Message = "SensorSample objekat je null",
                        Field = "Sample",
                        Details = "Sample je null"
                    },
                    new FaultReason("Nevalidan sample")
                );
            }

            // Provjera da li su vrijednosti numerički validne
            CheckFinity("Volume", sample.Volume, sample);
            CheckFinity("RelativeHumidity", sample.RelativeHumidity, sample);
            CheckFinity("AirQuality", sample.AirQuality, sample);
            CheckFinity("LightLevel", sample.LightLevel, sample);

            // Provjera opsega
            if (sample.Volume < 0 || sample.Volume > 1000)
                throwValidationAndLog(sample, "Volume van dozvoljenog opsega (0-1000)");

            if (sample.RelativeHumidity <= 0 || sample.RelativeHumidity > 100)
                throwValidationAndLog(sample, "RelativeHumidity van dozvoljenog opsega (0-100)");

            if (sample.AirQuality < 0 || sample.AirQuality > 1000000)
                throwValidationAndLog(sample, "AirQuality van dozvoljenog opsega (0-100000)");

            if (sample.LightLevel < 0 || sample.LightLevel > 10000000)
                throwValidationAndLog(sample, "LightLevel van dozvoljenog opsega (0-1000000)");

            if (sample.DateTime == default)
                throwValidationAndLog(sample, "DateTime nije postavljen");
        }

        private void throwValidationAndLog(SensorSample sample, string message)
        {
            WriteRejactSample(sample, message);
            throw new FaultException<ValidationFault>(
                new ValidationFault
                {
                    Message = message,
                    Field = "Sample",
                    Value = sample
                },
                new FaultReason($"Nevalidan sample: {message}")
            );
        }

        private void CheckFinity(string fieldName, double value, SensorSample sample)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                WriteRejactSample(sample, $"{fieldName} nije validna numericka vrednost");
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = $"{fieldName} nije validna numericka vrednost",
                        Field = fieldName,
                        Value = sample
                    },
                    new FaultReason($"Nevalidan sample: {fieldName} nije validan")
                );
            }
        }
        public SensorService()
        {
            // creates the unmanaged resource...
        }
        ~SensorService()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                // Free the unmanaged resource anytime.
                if (disposing)
                {
                    // Free any other managed objects here.
                    measurementsWriter.Close();
                    rejestSampleWriter.Close();
                    measurementsWriter.Dispose();
                    rejestSampleWriter.Dispose();
                }
                disposed = true;
            }
        }

    }
}
