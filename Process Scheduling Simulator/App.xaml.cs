using System.Configuration;
using System.Data;
using System.Windows;

namespace Process_Scheduling_Simulator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Boolean isDebugMode = false;
        private void Applicaiton_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Contains("--test-mode-enable"))
            {
                isDebugMode = true;
            }
        }
    }

}
