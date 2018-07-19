using MahApps.Metro.Controls;
using ReactiveUI;

namespace Falkor.Pressure.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, IViewFor<MainWindowViewModel>
    {
        public MainWindow()
        {
            InitializeComponent();
            this.ViewModel = new MainWindowViewModel();
            this.WhenAnyValue(x => x.ViewModel).BindTo(this, window => window.DataContext);
            this.BindCommand(this.ViewModel, model => model.Start, window => window.StartButton);
            this.BindCommand(this.ViewModel, model => model.Stop, window => window.StopButton);
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = value as MainWindowViewModel; }
        }

        public MainWindowViewModel ViewModel { get; set; }
    }
}
