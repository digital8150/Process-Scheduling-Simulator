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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Process_Scheduling_Simulator.Classes;

namespace Process_Scheduling_Simulator.View
{
    /// <summary>
    /// License.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class License : Window
    {
        public License()
        {
            this.Opacity = 0;
            InitializeComponent();
        }

        private async void AppCloseClickedEventHandler(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Topbar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private async void LoadedHandler(object sender, RoutedEventArgs e)
        {
            await Task.Delay(100);
            AnimationController.BeginAnimation(this, OpacityProperty, from: 0, to: 1, duration: 0.5, easingFunction: new CubicEase());
        }
    }
}
