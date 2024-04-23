using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WiimoteLib;
using WpfApp7.ViewModels;

namespace WpfApp7
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WiimoteCollection mWC;
        private MainWinViewModel winWM;

        public MainWindow()
        {
            InitializeComponent();

            winWM = new MainWinViewModel();

            // find all wiimotes connected to the system
            //mWC = new WiimoteCollection();
            //int index = 1;

            try
            {
                winWM.FindWiimotes();
            }
            catch (WiimoteNotFoundException ex)
            {
                MessageBox.Show(ex.Message, "Wiimote not found error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (WiimoteException ex)
            {
                MessageBox.Show(ex.Message, "Wiimote error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Unknown error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            //Trace.WriteLine(calibCanvas.Width);
            Width = winWM.CanvasWidth;
            Height = winWM.CanvasHeight;

            //winWM.SetCanvasDimensions((int)Width, (int)Height);
            winWM.CheckStartProcess();
            SetupEvents();

            DataContext = winWM;
        }

        private void SetupEvents()
        {
            //winWM.WiimoteStatePreprocess += WinWM_WiimoteStatePreprocess;
            winWM.StateDataChanged += WinWM_StateDataChanged;
        }

        private void WinWM_StateDataChanged(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                // Tell UI to update bindings
                //DataContext = null;
                //DataContext = winWM;
                // Tell UI to update lightgun point position
                winWM.UpdateLightGunPoint();
            });
        }

        private void WinWM_WiimoteStatePreprocess(object sender, EventArgs e)
        {
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            DataContext = null;

            winWM.TearDown();
            winWM = null;
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.R)
            {
                winWM.Reset();
                winWM.StartCalibration();
                e.Handled = true;
            }
            else if (e.Key == Key.Q)
            {
                e.Handled = true;
                Application.Current.Shutdown(0);
            }
        }
    }
}