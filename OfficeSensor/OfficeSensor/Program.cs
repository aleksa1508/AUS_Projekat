using Common;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OfficeSensor
{
    public class Program
    {
        static void Main(string[] args)
        {
           /* NetTcpBinding binding = new NetTcpBinding
            {
                TransferMode = TransferMode.Streamed,
                MaxReceivedMessageSize = 65536
            };

            EndpointAddress address = new EndpointAddress("net.tcp://localhost:4000/SensorService");*/

            ChannelFactory<ISensor> factory = new ChannelFactory<ISensor>("SensorService");
            ISensor proxy = factory.CreateChannel();

            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var fileDirectoryPath = ConfigurationManager.AppSettings["path"];
            var rows = Int32.Parse(ConfigurationManager.AppSettings["maxRows"]);
            /*// Relativna putanja do CSV fajla unutar projekta
            string csvFilePath = Path.Combine(exeDir, "Data.csv");
            Console.WriteLine(fileDirectoryPath);
            // Primer upotrebe sa tvojom funkcijom Reader
            string logFilePath = Path.Combine(exeDir, "log.txt");*/
            if (!File.Exists(fileDirectoryPath))
                throw new FileNotFoundException($" {fileDirectoryPath} fajl nije pronađen:");
           var readData = new ReadData(fileDirectoryPath);
           var lista=readData.Reader(rows);
            Console.WriteLine($"Učitano je {lista.Count} redova iz csv fajla.");

            var meta=new SessionMetaData
            {
                Volume = lista[0].Volume,
                RelativeHumidity = lista[0].RelativeHumidity,
                AirQuality = lista[0].AirQuality,
                LightLevel = lista[0].LightLevel,
                DateTime = lista[0].DateTime,
            };
            Console.WriteLine("Pokrenuta sesija!");
            try
            {
            
                var sensorService = proxy.StartStession(meta);

                if (sensorService.ServiceType == ServiceType.NACK)
                {
                    Console.WriteLine($"{sensorService.ServiceType} ");
                    return;
                }

            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine($"Validacijska greska pri pokretanju: {ex.Detail.Message}");
                return;
            }
            catch (FaultException<DataFormatFault> ex)
            {
                Console.WriteLine($"Format greška pri pokretanju: {ex.Detail.Message}");
                Console.WriteLine($"Detalji: {ex.Detail.Details}");
                return;
            }
            int i = 0;
            foreach (var sensor in lista)
            {
                try
                {
                   
                    var response = proxy.PushSample(sensor);

                    if (response.ServiceType == ServiceType.ACK) 
                        Console.WriteLine($"{++i} -> Uzorak uspesno obradjen: {response.Message}");
                    else
                        Console.WriteLine($"{++i}-> Uzorak odbijen zbog ne validnih podataka: {response.Message}");
                }
                
                catch (FaultException ex)
                {
                    Console.WriteLine($"[NEOCEKIVANA WCF GRESKA] {i}: {ex.Message}");
                }

                Thread.Sleep(100);
            }
           
            var finallyResponse = proxy.EndSession();

            if (finallyResponse.ServiceType == ServiceType.ACK)
            {
                Console.WriteLine($"[KRAJ] Sesija je zatvorena");
            }
            else
            {
                Console.WriteLine($"[GRESKA END SESSION]{finallyResponse.Message}");
            }
        }
    }
}
