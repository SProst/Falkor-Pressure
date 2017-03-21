using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FalkorPressure.ViewModels;
using ReactiveUI;

namespace FalkorPressure.Views
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
