using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class ServiceProgram
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            //Uri baseAddress = new Uri("net.tcp://localhost:4000/SensorService");
            SensorService sensorService = new SensorService();
            EventSubscriber eventSubscriber = new EventSubscriber(sensorService);
           
            ServiceHost host = new ServiceHost(sensorService);

           /* NetTcpBinding binding = new NetTcpBinding
            {
                TransferMode = TransferMode.Streamed,
                MaxReceivedMessageSize = 65536
            };

            */
            try
            {
               //host.AddServiceEndpoint(typeof(ISensor), binding, "");
                host.Open();
                Console.WriteLine("Servis pokrenut na net.tcp://localhost:4000/SensorService");
                Console.WriteLine("Pritisnite neki taster za zatvaranje");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska pri pokretanju servisa: {ex.Message}");
            }
            host.Close();
            eventSubscriber.CloseEvents(sensorService);

        }
    }
}
