using System.Windows.Controls;
using Falkor.Pressure.App.ViewModels;
using ReactiveUI;

namespace Falkor.Pressure.App.Views
{
    /// <summary>
    /// Interaction logic for PressureSettingsView.xaml
    /// </summary>
    public partial class PressureSettingsView : UserControl, IViewFor<PressureSettingsViewModel>
    {
        public PressureSettingsView()
        {
            InitializeComponent();
            this.WhenAnyValue(x => x.ViewModel).BindTo(this, x => x.DataContext);

            this.BindCommand(this.ViewModel, model => model.SelectDirectory, view => view.SelectDirectoryButton);
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = value as PressureSettingsViewModel; }
        }

        public PressureSettingsViewModel ViewModel { get; set; }
    }
}
