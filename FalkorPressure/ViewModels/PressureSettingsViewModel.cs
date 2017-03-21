using System.Reactive;
using Microsoft.WindowsAPICodePack.Dialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using NationalInstruments.DAQmx;

namespace FalkorPressure.ViewModels
{
    [DataContract]
    public class PressureSettingsViewModel : ReactiveObject
    {
        public PressureSettingsViewModel()
        {
            this.AcquisitionWindow = 1;
            this.DataRate = 1;
            this.SamplesToAverage = 1;
            this.FilterRate = 1;
            this.SelectDirectory = ReactiveCommand.Create(() =>
            {
                CommonOpenFileDialog fileDialog = new CommonOpenFileDialog() { IsFolderPicker = true };
                if (fileDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    Directory = fileDialog.FileName;
                }
            });

            this.Add = ReactiveCommand.Create(() =>
                {
                    this.AiPressureChannels.Add(this.SelectedAiChannel);
                    this.AiChannels.Remove(this.SelectedAiChannel);
                },
                this.WhenAnyValue(x => x.SelectedAiChannel).Select(x => x != null));

            this.Remove = ReactiveCommand.Create(() =>
            {
                this.AiChannels.Add(this.SelectedAiPressureChannel);
                this.AiPressureChannels.Remove(this.SelectedAiPressureChannel);
            },
                this.WhenAnyValue(x => x.SelectedAiPressureChannel).Select(x => x != null));

            this.FileName = string.Empty;
            
            this.Devices = new ReactiveList<Device>(DaqSystem.Local.Devices.Select(x => DaqSystem.Local.LoadDevice(x)));
            this.AiChannels = new ReactiveList<ChannelViewModel>(this.Devices.SelectMany(x => x.AIPhysicalChannels).Select(y => new ChannelViewModel(y))){ChangeTrackingEnabled = true};
            this.AiPressureChannels = new ReactiveList<ChannelViewModel>(){ChangeTrackingEnabled = true};

            if (File.Exists(Properties.Settings.Default.ChannelsFile))
            {
                using (var reader = File.OpenText(Properties.Settings.Default.ChannelsFile))
                {
                    using (var sr = new StreamReader(reader.BaseStream))
                    {
                        while (sr.EndOfStream == false)
                        {
                            var line = sr.ReadLine();
                            var splitLine = line?.Split(',');
                            if (splitLine != null && splitLine.Length == 2)
                            {
                                var channel = this.AiChannels.FirstOrDefault(x => x.Address == splitLine[0]);
                                if (channel != null)
                                {
                                    channel.MultiplierFactor = int.Parse(splitLine[1]);
                                }
                                this.AiPressureChannels.Add(channel);
                                this.AiChannels.Remove(channel);
                            }
                        }
                        
                        
                    }
                }
            }
        }

        [Reactive]
        public string Directory { get; set; }

        [Reactive]
        public int AcquisitionWindow { get; set; }

        [Reactive]
        public double DataRate { get; set; }

        [Reactive]
        public string FileName { get; set; }

        [Reactive]
        public double FilterRate { get; set; }

        [Reactive]
        public double SamplesToAverage { get; set; }

        public ReactiveCommand<Unit, Unit> SelectDirectory { get; }

        public ReactiveCommand<Unit, Unit> Add { get; }

        public ReactiveCommand<Unit, Unit> Remove { get; }

        public ReactiveList<ChannelViewModel> AiChannels { get; }

        public ReactiveList<ChannelViewModel> AiPressureChannels { get; }

        [Reactive]
        public ChannelViewModel SelectedAiChannel { get; set; }

        [Reactive]
        public ChannelViewModel SelectedAiPressureChannel { get; set; }

        public ReactiveList<Device> Devices { get; }
    }
}