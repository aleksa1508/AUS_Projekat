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
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Učitano je {lista.Count} redova iz csv fajla.");
            Console.ResetColor();
            var meta = new SessionMetaData();
            Console.WriteLine("[START] Pokrenuta sesija!");
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
                Console.ForegroundColor= ConsoleColor.Red;
                Console.WriteLine($"Validacijska greska pri pokretanju: {ex.Detail.Message}");
                Console.ResetColor();
                return;
            }
            catch (FaultException<DataFormatFault> ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Format greška pri pokretanju: {ex.Detail.Message}");
                Console.WriteLine($"Detalji: {ex.Detail.Details}");
                Console.ResetColor();
                return;
            }
            int i = 0;
            foreach (var sensor in lista)
            {
                try
                {

                    var response = proxy.PushSample(sensor);

                    if (response.ServiceType == ServiceType.ACK)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{++i} -> Uzorak uspesno obradjen: {response.Message}");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{++i}-> Uzorak odbijen zbog ne validnih podataka: {response.Message}");
                        Console.ResetColor();
                    }
                }

                catch (FaultException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[NEOCEKIVANA WCF GRESKA] {i}: {ex.Message}");
                    Console.ResetColor();
                }

                Thread.Sleep(100);
            }
           
            var finallyResponse = proxy.EndSession();

            if (finallyResponse.ServiceType == ServiceType.ACK)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[KRAJ] Sesija je zatvorena");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[GRESKA END SESSION]{finallyResponse.Message}");
                Console.ResetColor();
            }
        }
    }
}
