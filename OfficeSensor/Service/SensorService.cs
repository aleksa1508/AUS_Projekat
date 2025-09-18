using Common;
using Service;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class SensorService : ISensor, IDisposable
    {

        public delegate void TransferEventHandler(object sender, TransferEventArgs e);
        public delegate void SampleEventHandler(object sender, SampleEventArgs e);
        public delegate void WarningEventHandler(object sender, WarningEventArgs e);
       /* public delegate void OutOfBoundWarningEventHandler(object sender, OutOfBoundWarningEventArgs e);
        public delegate void SpikeEventHandler(object sender, SpikeEventArgs e);*/

        // Događaji
        public event TransferEventHandler OnTransferStarted;
        public event SampleEventHandler OnSampleReceived;
        public event TransferEventHandler OnTransferCompleted;
        public event WarningEventHandler OnWarningRaised;
        /*public event OutOfBoundWarningEventHandler OnOutOfBoundWarning;
        public event SpikeEventHandler OnPressureSpike;
        public event SpikeEventHandler OnAQSpike;
        public event SpikeEventHandler OnRHSpike;*/

        private List<double> humiditySamples = new List<double>();
        private List<double> lightSamples = new List<double>();
        private double AQ_threshold;
        private double RH_threshold;
        private double L_threshold;
        private double TresholdPrecentage;
        private SensorSample previousSample=null;
        public static bool sessionActive = false;

        private bool disposed = false;
        StreamWriter measurementsWriter;
        StreamWriter rejestSampleWriter;
        public void LoadData()
        {
            // Učitavanje pragova iz konfiguracije
            AQ_threshold = double.Parse(ConfigurationManager.AppSettings["AQ_threshold"]);
            RH_threshold = double.Parse(ConfigurationManager.AppSettings["RH_threshold"]);
            L_threshold = double.Parse(ConfigurationManager.AppSettings["L_threshold"]);
            TresholdPrecentage = double.Parse(ConfigurationManager.AppSettings["TresholdPrecentage"]);
        }


        public void StartTransfer()
        {
            OnTransferStarted?.Invoke(this, new TransferEventArgs("Transfer started."));
        }

        public void WarningRaise()
        {
            OnWarningRaised?.Invoke(this, new WarningEventArgs($"WARNING RAISED"));
        }

        public void ReceiveSample(double volume, double rh, double aq, double light)
        {
            // Okidamo događaj da je sample primljen
            OnSampleReceived?.Invoke(this, new SampleEventArgs(volume, rh, aq, light));

            // Logika provere pragova
            /*if (aq > AQ_threshold)
                OnWarningRaised?.Invoke(this, new WarningEventArgs($"AQ over threshold: {aq} > {AQ_threshold}"));

            if (rh > RH_threshold)
                OnWarningRaised?.Invoke(this, new WarningEventArgs($"RH over threshold: {rh} > {RH_threshold}"));

            if (light > L_threshold)
                OnWarningRaised?.Invoke(this, new WarningEventArgs($"Light over threshold: {light} > {L_threshold}"));
            */
            // Prosek RH i odstupanje ±25%
            /*humiditySamples.Add(rh);
            double avgRh = humiditySamples.Average();
            double lowerBound = avgRh * 0.75;
            double upperBound = avgRh * 1.25;

            if (rh < lowerBound || rh > upperBound)
                OnWarningRaised?.Invoke(this, new WarningEventArgs($"RH deviates ±25% from avg {avgRh:F2}: {rh}"));
            */

            // --- ΔL za prethodni sample ---
            if (previousSample != null)
            {
                double deltaL = light - previousSample.LightLevel;
//                Console.WriteLine($"LIGHT NOW {light} AND PREVIOUS LIGHT LEVEL {previousSample.LightLevel}  ->{deltaL}  {L_threshold}");
                if (Math.Abs(deltaL) > L_threshold)
                {
                    string direction = deltaL > 0 ? "iznad očekivanog" : "ispod očekivanog";
                    OnWarningRaised?.Invoke(this, new WarningEventArgs($"[PRESSURE SPIKE]:\t ΔL={deltaL:F2}, smer: {direction}"));
                   // OnPressureSpike?.Invoke(this, new SpikeEventArgs($" ΔL={deltaL:F2}, smer: {direction}"));
                }

                double deltaAQ = aq - previousSample.AirQuality;
                //                Console.WriteLine($"LIGHT NOW {light} AND PREVIOUS LIGHT LEVEL {previousSample.LightLevel}  ->{deltaL}  {L_threshold}");
                if (Math.Abs(deltaAQ) > AQ_threshold)
                {
                    string direction = deltaAQ > 0 ? "iznad očekivanog" : "ispod očekivanog";
                    OnWarningRaised?.Invoke(this, new WarningEventArgs($"[AQ SPIKE]:\t ΔAQ={deltaAQ:F2}, smer: {direction}"));
                  //  OnAQSpike?.Invoke(this, new SpikeEventArgs($"[AQ Spike]: ΔAQ={deltaAQ:F2}, smer: {direction}"));
                }
                double deltaRH = rh - previousSample.RelativeHumidity;
                //                Console.WriteLine($"LIGHT NOW {light} AND PREVIOUS LIGHT LEVEL {previousSample.LightLevel}  ->{deltaL}  {L_threshold}");
                if (Math.Abs(deltaRH) > RH_threshold)
                {
                    string direction = deltaRH > 0 ? "iznad očekivanog" : "ispod očekivanog";
                    OnWarningRaised?.Invoke(this, new WarningEventArgs($"[RH SPIKE]:\t ΔRH={deltaRH:F2}, smer: {direction}"));
                   // OnRHSpike?.Invoke(this, new SpikeEventArgs($"[RH Spike]: ΔRH={deltaRH:F2}, smer: {direction}"));
                }
            }

            // --- Running mean za LightLevel i ±25% provera ---
            lightSamples.Add(light);
            double lMean = lightSamples.Average();
            double lowerBoundL = lMean * (1 - TresholdPrecentage / 100.0);
            double upperBoundL = lMean * (1 + TresholdPrecentage / 100.0);

            if (light < lowerBoundL || light > upperBoundL)
            {
                string direction = light < lowerBoundL ? "ispod očekivane vrednosti" : "iznad očekivane vrednosti";
                OnWarningRaised?.Invoke(this, new WarningEventArgs($"[OUT OF BOUND WARNING]:\t L={light:F2}, Lmean={lMean:F2}, smer: {direction}"));
                //OnOutOfBoundWarning?.Invoke(this, new OutOfBoundWarningEventArgs($"OutOfBandWarning: L={light:F2}, Lmean={lMean:F2}, smer: {direction}"));
            }

            // --- Zapamti trenutni sample kao prethodni ---
            previousSample = new SensorSample
            {
                Volume = volume,
                RelativeHumidity = rh,
                AirQuality = aq,
                LightLevel = light,
                DateTime = DateTime.Now
            };
        }

        public void CompleteTransfer()
        {
            OnTransferCompleted?.Invoke(this, new TransferEventArgs("Transfer completed."));
        }

        public ServiceResponse EndSession()
        {
            try
            {
                sessionActive = false;
                measurementsWriter?.Close();
                rejestSampleWriter?.Close();
                CompleteTransfer();

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

                Console.WriteLine("[INFO] Prenos je u toku...");

                measurementsWriter.WriteLine($"{sample.Volume},{sample.RelativeHumidity},{sample.AirQuality},{sample.LightLevel},{sample.DateTime}");
                measurementsWriter.Flush();

                ReceiveSample(sample.Volume, sample.RelativeHumidity, sample.AirQuality, sample.LightLevel);
                Console.WriteLine("[INFO] Zavrsen prenos...\n");

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
                //WarningRaise();
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
                //WarningRaise();
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
                //WarningRaise();
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


                StartTransfer();

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

            // Provjera opsega mi smo svoje neke opsege stavili kako bi izbacili odredjene redove
            if (sample.Volume < 0 || sample.Volume > 100)
                throwValidationAndLog(sample, "Volume van dozvoljenog opsega (0-100)");

            if (sample.RelativeHumidity <=28.2 || sample.RelativeHumidity > 100)
                throwValidationAndLog(sample, "RelativeHumidity van dozvoljenog opsega (28.2-100)");

            if (sample.AirQuality < 25000 || sample.AirQuality > 50000)
                throwValidationAndLog(sample, "AirQuality van dozvoljenog opsega (25000-50000)");

            if (sample.LightLevel < 200 || sample.LightLevel > 10300000)
                throwValidationAndLog(sample, "LightLevel van dozvoljenog opsega (200-1030000)");

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
