using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using Falkor.Pressure.App.ViewModels;
using Falkor.Pressure.App.Views;
using Microsoft.WindowsAPICodePack.Dialogs;
using NationalInstruments.DAQmx;
using ReactiveUI;
using Task = System.Threading.Tasks.Task;

namespace Falkor.Pressure.App
{
    public class MainWindowViewModel : ReactiveObject, IDisposable
    {
        private CancellationTokenSource tokenSource;

        public MainWindowViewModel()
        {
            this.Alpha = new PlotModel(){ Title = "Pressure Readback (MKS)"};
            DateTimeAxis xAxis = new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "HH:mm:ss:fff",

                Title = "Pressure",
                MinorIntervalType = DateTimeIntervalType.Seconds,
                IntervalType = DateTimeIntervalType.Seconds,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.None,
            };
            this.Alpha.Axes.Add(xAxis);
            this.Alpha.Axes.Add(new LinearAxis() { AbsoluteMinimum = 0, Position = AxisPosition.Left, Title = "milli-torr"});
            var lineSeries = new LineSeries(){ Color = OxyColors.DeepSkyBlue, Title = "SLIM"};
            this.Alpha.Series.Add(lineSeries);

            var betaLineSeries = new LineSeries() {Color = OxyColors.ForestGreen, Title = "IFT"};
            this.Alpha.Series.Add(betaLineSeries);

            this.PressureSettings = new PressureSettingsViewModel();

            this.WhenAnyValue(x => x.SaveToFile).Where(x => x).Select(async b =>
            {
                dataToSave = new BlockingCollection<ChannelViewModel>();

                this.StartingDateTime = DateTime.Now;

                await Task.Run(async () =>
                {

                    foreach (var analogWaveform in dataToSave.GetConsumingEnumerable())
                    {
                        var filePath = Path.Combine(PressureSettings.Directory, $"{this.PressureSettings.FileName}{analogWaveform.Address.Replace('/', '_')}.csv");
                        using (var filestream = File.Open(filePath, FileMode.Append))
                        {
                            using (var streamWriter = new StreamWriter(filestream))
                            {
                                foreach (var analogWaveformSample in analogWaveform.Samples)
                                {
                                    await streamWriter.WriteLineAsync(
                                         $"{analogWaveformSample.PrecisionTimeStamp.ToString("HH:mm:ss:fff")}\t{analogWaveform.ConvertPressure(analogWaveformSample.Value)}");
                                }
                            }
                        }
                    }
                    this.SaveToFile = false;
                });
                
                return b;
            }).Subscribe();
           
            this.Stop = ReactiveCommand.Create(() =>
            {
                this.tokenSource.Cancel();
            });
            this.Start = ReactiveCommand.CreateFromTask(async () =>
            {
                tokenSource = new CancellationTokenSource();
                await Acquire(tokenSource.Token);

            }, this.WhenAnyValue(x => x.PressureSettings.Directory).Select(x => x != null));

            this.Start.ThrownExceptions.Subscribe(exception =>
            {
                TaskDialog dialog = new TaskDialog();
                dialog.Icon = TaskDialogStandardIcon.Error;
                dialog.Text = exception.Message;
                dialog.StandardButtons = TaskDialogStandardButtons.Ok;
                dialog.Show();
            });

            this.OpenSettings = ReactiveCommand.CreateFromTask(async () =>
            {
               await Application.Current.MainWindow.ShowChildWindowAsync(new ChildWindow()
                    {
                        Content = new PressureSettingsView() {ViewModel = this.PressureSettings},
                        IsModal = true,
                        CloseOnOverlay = true,
                        ShowCloseButton = true
                    });
            });

            this.PressureSettings.AiPressureChannels.ItemsAdded.Subscribe(model =>
            {
                using (var streamWriter = new StreamWriter("channels.txt"))
                {
                    foreach (var pressureSettingsAiPressureChannel in PressureSettings.AiPressureChannels)
                    {
                        streamWriter.WriteLine($"{pressureSettingsAiPressureChannel.Address},{pressureSettingsAiPressureChannel.MultiplierFactor}");
                    }

                }
            });

            this.PressureSettings.AiPressureChannels.ItemsRemoved.Subscribe(model =>
            {
                using (var streamWriter = new StreamWriter("channels.txt"))
                {
                    foreach (var pressureSettingsAiPressureChannel in PressureSettings.AiPressureChannels)
                    {
                        streamWriter.WriteLine($"{pressureSettingsAiPressureChannel.Address},{pressureSettingsAiPressureChannel.MultiplierFactor}");
                    }
                }
            });

            this.PressureSettings.AiPressureChannels.ItemChanged.Subscribe(args =>
            {
                using (var streamWriter = new StreamWriter("channels.txt"))
                {
                    foreach (var pressureSettingsAiPressureChannel in PressureSettings.AiPressureChannels)
                    {
                        streamWriter.WriteLine($"{pressureSettingsAiPressureChannel.Address},{pressureSettingsAiPressureChannel.MultiplierFactor}");
                    }
                }
            });

        }

        [Reactive]
        public PlotModel Alpha { get; set; }

        public ReactiveCommand<Unit, Unit> Start { get; }

        public ReactiveCommand<Unit, Unit> Stop { get; }

        [Reactive]
        public bool SaveToFile { get; set; }

        public PressureSettingsViewModel PressureSettings { get; }

        public BlockingCollection<ChannelViewModel> dataToSave;

        public ReactiveCommand<Unit, Unit> OpenSettings { get; }

        [Reactive]
        public DateTime StartingDateTime { get; set; }

        [Reactive]
        public double Mean { get; set; }

        [Reactive]
        public double StdDev { get; set; }

        private async Task Acquire(CancellationToken token)
        {
            await Task.Run(() =>
            {
               
                if (PressureSettings.AiPressureChannels.IsEmpty)
                {
                    return;
                }
                using (var niTask = new NationalInstruments.DAQmx.Task())
                {
                    foreach (var pressureSettingsAiPressureChannel in PressureSettings.AiPressureChannels)
                    {
                        niTask.AIChannels.CreateVoltageChannel(pressureSettingsAiPressureChannel.Address, "", AITerminalConfiguration.Differential, 0, 10,
                                AIVoltageUnits.Volts);
                    }
                   

                    niTask.Timing.ConfigureSampleClock("", PressureSettings.DataRate, SampleClockActiveEdge.Rising,
                    SampleQuantityMode.ContinuousSamples);
                    var reader = new AnalogMultiChannelReader(niTask.Stream);

                    niTask.Control(TaskAction.Verify);

                    int maxSamples = 30000;
                    
                    while (!token.IsCancellationRequested)
                    {
                        var amountToRead = (int)PressureSettings.DataRate;
                        var data = reader.ReadWaveform(amountToRead);

                        lock (this.Alpha.SyncRoot)
                        {
                            var waveform = data[0];
                            var channel = this.PressureSettings.AiPressureChannels.First(x => x.Address == waveform.ChannelName);
                            channel.Samples = waveform.Samples;
                           
                            var alphaLineSeries = this.Alpha.Series[0] as LineSeries;
                            if (alphaLineSeries == null)
                            {
                                continue;
                            }
                            for (int i = 0; i < channel.Samples.Count; i++)
                            {
                                var sample = channel.Samples[i];
                                alphaLineSeries.Points.Add(
                                    new DataPoint(DateTimeAxis.ToDouble(sample.PrecisionTimeStamp.ToDateTime()),
                                        channel.ConvertPressure(sample.Value)));
                               
                                if (SaveToFile)
                                {
                                    var result = sample.TimeStamp - StartingDateTime;
                                    if (result.Seconds >= PressureSettings.AcquisitionWindow)
                                    {
                                        dataToSave.CompleteAdding();
                                    }
                                }
                               var meanStdDeviation = alphaLineSeries.Points.Select(x => x.Y).MeanStandardDeviation();
                                Mean = meanStdDeviation.Item1;
                                StdDev = meanStdDeviation.Item2;

                                if (alphaLineSeries.Points.Count > maxSamples)
                                {
                                    alphaLineSeries.Points.RemoveAt(
                                        alphaLineSeries.Points.IndexOf(alphaLineSeries.Points.First()));
                                }
                            }

                            if (SaveToFile && !dataToSave.IsAddingCompleted)
                            {
                                dataToSave.TryAdd(channel);
                            }

                            waveform = data[1];
                            channel = this.PressureSettings.AiPressureChannels.First(x => x.Address == waveform.ChannelName);
                            channel.Samples = waveform.Samples;
                            var betaLineSeries = this.Alpha.Series[1] as LineSeries;
                            if (betaLineSeries == null)
                            {
                                continue;
                            }
                            for (int i = 0; i < channel.Samples.Count; i++)
                            {
                                var sample = channel.Samples[i];
                                betaLineSeries.Points.Add(
                                    new DataPoint(DateTimeAxis.ToDouble(sample.PrecisionTimeStamp.ToDateTime()),
                                       channel.ConvertPressure(sample.Value)));

                                if (betaLineSeries.Points.Count > maxSamples)
                                {
                                    betaLineSeries.Points.RemoveAt(
                                        betaLineSeries.Points.IndexOf(betaLineSeries.Points.First()));
                                }
                            }
                            if (SaveToFile && !dataToSave.IsAddingCompleted)
                            {
                                dataToSave.TryAdd(channel);
                            }
                            this.Alpha.InvalidatePlot(true);
                        }

                    }
                }
            });
           
           
        }

        public void Dispose()
        {
            tokenSource?.Dispose();
            dataToSave?.Dispose();
            Start?.Dispose();
            Stop?.Dispose();
            OpenSettings?.Dispose();
        }
    }
}