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
    /// Init.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Init : Window
    {
        public static MainWindow mainApplication;
        public static ConsoleDebugger consoleDebugger;
        public static GanttChartPrototype ganttChartPrototype;

        public Init()
        {
            InitializeComponent();
            this.Opacity = 0;
            BorderMain.Width = 0;
            BorderMain.Height = 0;
        }

        private async void LoadedEventHandler(object sender, RoutedEventArgs e)
        {
            await Task.Delay(100);
            AnimationController.BeginAnimation(this, OpacityProperty, duration: 0.5, easingFunction: new CubicEase());
            AnimationController.BeginAnimation(BorderMain, WidthProperty, 0, 1280, 0.7, easingFunction: new CubicEase());
            AnimationController.BeginAnimation(BorderMain, HeightProperty, 0, 720, 0.7, easingFunction: new CubicEase());
        }

        private async void AppCloseClickedEventHandler(object sender, RoutedEventArgs e)
        {
            AnimationController.BeginAnimation(this, OpacityProperty, from:1, to:0, duration: 0.5, easingFunction: new CubicEase());
            AnimationController.BeginAnimation(BorderMain, WidthProperty, 1280, 0, 0.7, easingFunction: new CubicEase());
            AnimationController.BeginAnimation(BorderMain, HeightProperty, 720, 0, 0.7, easingFunction: new CubicEase());
            await Task.Delay(500);
            this.Close();
        }

        private void MainApplicationInstanceClickedEventHandler(object sender, RoutedEventArgs e)
        {
            mainApplication = new MainWindow();
            mainApplication.Show();
        }

        private void GanttChartPrototypeInstanceClickedEventHandler(object sender, RoutedEventArgs e)
        {
            ganttChartPrototype = new GanttChartPrototype();
            ganttChartPrototype.Show();
        }
        private void ConsoleDebuggerInstanceClickedEventHandler(object sender, RoutedEventArgs e)
        {
            consoleDebugger = new ConsoleDebugger();
        }



        private void Topbar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }


    }
}
