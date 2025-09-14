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
            sensorService.OnPressureSpike += HandlePressureSpike;
            sensorService.OnAQSpike += HandleAQSpike;
            sensorService.OnRHSpike += HandleRHSpike;
            sensorService.OnOutOfBoundWarning += HandleOnOutOfBoundWarning;
        }

        // Handler metode
        private void HandleTransferStarted(object sender, TransferEventArgs e)
        {
            Console.WriteLine($"[START] {e.Message}");
        }

        private void HandleSampleReceived(object sender, SampleEventArgs e)
        {
            Console.WriteLine($"[SAMPLE] Volume={e.Volume}, RH={e.RelativeHumidity}, AQ={e.AirQuality}, L={e.LightLevel}");
        }

        private void HandleWarningRaised(object sender, WarningEventArgs e)
        {
            Console.WriteLine($"⚠️ WARNING: {e.Warning}");
        }

        private void HandleTransferCompleted(object sender, TransferEventArgs e)
        {
            Console.WriteLine($"[END] {e.Message}");
        }
        private void HandleAQSpike(object sender, SpikeEventArgs e)
        {
            Console.WriteLine($"[AQ SPIKE]: {e.Warning}");
        }
        private void HandlePressureSpike(object sender, SpikeEventArgs e)
        {
            Console.WriteLine($"[PRESSURE SPIKE]: {e.Warning}");
        }
        private void HandleRHSpike(object sender, SpikeEventArgs e)
        {
            Console.WriteLine($"[RH SPIKE]: {e.Warning}");
        }
        private void HandleOnOutOfBoundWarning(object sender, OutOfBoundWarningEventArgs e)
        {
            Console.WriteLine($"[OUT OF BOUND WARNING]: {e.Warning}");
        }

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
