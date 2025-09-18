using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class EventSubscriber
    {
        private SensorService sensorService;

        public EventSubscriber(SensorService sensorService)
        {
            this.sensorService = sensorService;
            sensorService.LoadData();

            // Pretplata na događaje
            sensorService.OnTransferStarted += HandleTransferStarted;
            sensorService.OnSampleReceived += HandleSampleReceived;
            sensorService.OnWarningRaised += HandleWarningRaised;
            sensorService.OnTransferCompleted += HandleTransferCompleted;
            /*sensorService.OnPressureSpike += HandlePressureSpike;
            sensorService.OnAQSpike += HandleAQSpike;
            sensorService.OnRHSpike += HandleRHSpike;
            sensorService.OnOutOfBoundWarning += HandleOnOutOfBoundWarning;*/
        }

        // Handler metode
        private void HandleTransferStarted(object sender, TransferEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[START] {e.Message}");
            Console.ResetColor();
            Console.WriteLine();
        }


        private void HandleSampleReceived(object sender, SampleEventArgs e)
        {
            Console.ForegroundColor= ConsoleColor.Green;
            Console.WriteLine($"[SAMPLE] Volume={e.Volume}, RH={e.RelativeHumidity}, AQ={e.AirQuality}, L={e.LightLevel}");
            Console.ResetColor();
            Console.WriteLine();
        }

        private void HandleWarningRaised(object sender, WarningEventArgs e)
        {
            Console.ForegroundColor= ConsoleColor.Red;
            Console.WriteLine($"⚠️ WARNING:\n {e.Warning}");
            Console.ResetColor();
            Console.WriteLine();
        }

        private void HandleTransferCompleted(object sender, TransferEventArgs e)
        {
            Console.ForegroundColor= ConsoleColor.Magenta;
            Console.WriteLine($"[END] {e.Message}");
            Console.ResetColor();
            Console.WriteLine();
        }
       /* private void HandleAQSpike(object sender, SpikeEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"[AQ SPIKE]: {e.Warning}");
            Console.ResetColor();
            Console.WriteLine();
        }
        private void HandlePressureSpike(object sender, SpikeEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"[PRESSURE SPIKE]: {e.Warning}");
            Console.ResetColor();
            Console.WriteLine();
        }
        private void HandleRHSpike(object sender, SpikeEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"[RH SPIKE]: {e.Warning}");
            Console.ResetColor();
            Console.WriteLine();
        }
        private void HandleOnOutOfBoundWarning(object sender, OutOfBoundWarningEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"[OUT OF BOUND WARNING]: {e.Warning}");
            Console.ResetColor();
            Console.WriteLine();
        }
       */
        public void CloseEvents(SensorService sensorService)
        {
            if (sensorService != null)
            {
                // Otkaži pretplate na događaje
                sensorService.OnTransferStarted -= HandleTransferStarted;
                sensorService.OnSampleReceived -= HandleSampleReceived;
                sensorService.OnWarningRaised -= HandleWarningRaised;
                sensorService.OnTransferCompleted -= HandleTransferCompleted;
                sensorService.Dispose();
                sensorService = null;
            }
        }
    }
}
