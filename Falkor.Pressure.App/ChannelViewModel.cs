using System.Runtime.Serialization;
using NationalInstruments;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FalkorPressure.ViewModels
{
    [DataContract]
    public class ChannelViewModel : ReactiveObject
    {

        public ChannelViewModel(string address)
        {
            this.Address = address;
            this.MultiplierFactor = 1;
        }
        [Reactive]
        [DataMember]
        public string Address { get; set; }
        [Reactive]
        [DataMember]
        public int MultiplierFactor { get; set; }

        public double ConvertPressure(double voltage)
        {
            return MultiplierFactor * voltage * 1000;
        }

        public AnalogWaveformSampleCollection<double> Samples { get; set; }
    }
}