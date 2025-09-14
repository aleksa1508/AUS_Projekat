using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class TransferEventArgs : EventArgs
    {
        [DataMember]
        public string Message { get; }
        public TransferEventArgs(string message)
        {
            Message = message;
        }
    }
    [DataContract]
    public class SampleEventArgs : EventArgs
    {
        [DataMember]
        public double Volume { get; }
        [DataMember]
        public double RelativeHumidity { get; }
        [DataMember]
        public double AirQuality { get; }
        [DataMember]
        public double LightLevel { get; }

        public SampleEventArgs(double volume, double rh, double aq, double light)
        {
            Volume = volume;
            RelativeHumidity = rh;
            AirQuality = aq;
            LightLevel = light;
        }
    }
    [DataContract]
    public class WarningEventArgs : EventArgs
    {
        [DataMember]
        public string Warning { get; }
        public WarningEventArgs(string warning)
        {
            Warning = warning;
        }
    }
    [DataContract]
    public class OutOfBoundWarningEventArgs : EventArgs
    {
        [DataMember]
        public string Warning { get; }
        public OutOfBoundWarningEventArgs(string warning)
        {
            Warning = warning;
        }
    }
    [DataContract]
    public class SpikeEventArgs : EventArgs
    {
        [DataMember]
        public string Warning { get; }
        public SpikeEventArgs(string warning)
        {
            Warning = warning;
        }
    }
}
