using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class SessionMetaData
    {
        [DataMember]
        public string Volume { get; set; } = "Volume [mV]";

        [DataMember]
        public string RelativeHumidity { get; set; } = "RelativeHumidity [%]";

        [DataMember]
        public string AirQuality { get; set; } = "AirQuality [Ohms]";

        [DataMember]
        public string LightLevel { get; set; } = "LightLevel [Ohms]";

        [DataMember]
        public string DateTime { get; set; } = "Date Time";
    }
}
