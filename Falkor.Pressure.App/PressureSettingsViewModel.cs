using System.Reactive;
using Microsoft.WindowsAPICodePack.Dialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using FalkorPressure.Properties;
using MsgPack.Serialization;
using NationalInstruments.DAQmx;
using StackExchange.Redis;

namespace FalkorPressure.ViewModels
{
    [DataContract]
    public class PressureSettingsViewModel : ReactiveObject
    {
        private IDatabase database;

        public PressureSettingsViewModel()
        {
            this.AcquisitionWindow = 1;
            this.DataRate = 1;
            this.SamplesToAverage = 1;
            this.FilterRate = 1;
            this.Directory = Settings.Default.LogPath;
            this.SelectDirectory = ReactiveCommand.Create(() =>
            {
                CommonOpenFileDialog fileDialog =
                    new CommonOpenFileDialog() {IsFolderPicker = true, DefaultDirectory = Settings.Default.LogPath};
                if (fileDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    Directory = fileDialog.FileName;
                    Settings.Default.LogPath = Directory;
                    Settings.Default.Save();
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
            this.AiChannels = new ReactiveList<ChannelViewModel>(this.Devices.SelectMany(x => x.AIPhysicalChannels)
                .Select(y => new ChannelViewModel(y))) {ChangeTrackingEnabled = true};
            this.AiPressureChannels = new ReactiveList<ChannelViewModel>() {ChangeTrackingEnabled = true};

            //var writer = SerializationContext.Default.GetSerializer<PressureSettingsViewModel>();
            //this.database = RedisPersist.Redis.Value.GetDatabase();

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
                                    this.AiPressureChannels.Add(channel);
                                    this.AiChannels.Remove(channel);
                                }

                            }
                        }


                    }
                }
            }
        }

        [Reactive]
        [DataMember]
        public string Directory { get; set; }

        [Reactive]
        [DataMember]
        public int AcquisitionWindow { get; set; }

        [Reactive]
        [DataMember]
        public double DataRate { get; set; }

        [Reactive]
        [DataMember]
        public string FileName { get; set; }

        [Reactive]
        [DataMember]
        public double FilterRate { get; set; }

        [Reactive]
        [DataMember]
        public double SamplesToAverage { get; set; }

        public ReactiveCommand<Unit, Unit> SelectDirectory { get; }

        public ReactiveCommand<Unit, Unit> Add { get; }

        public ReactiveCommand<Unit, Unit> Remove { get; }

        public ReactiveList<ChannelViewModel> AiChannels { get; set; }

        [DataMember]
        public ReactiveList<ChannelViewModel> AiPressureChannels { get; set; }

        [Reactive]
        public ChannelViewModel SelectedAiChannel { get; set; }

        [Reactive]
        public ChannelViewModel SelectedAiPressureChannel { get; set; }

        public ReactiveList<Device> Devices { get; }
    }
}