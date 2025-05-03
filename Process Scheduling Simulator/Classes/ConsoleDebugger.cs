using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using Process_Scheduling_Simulator.View;

namespace Process_Scheduling_Simulator.Classes
{
    public class ConsoleDebugger
    {

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FreeConsole();


        private string refid = System.Guid.NewGuid().ToString();

        public ConsoleDebugger()
        {
            AllocConsole(); // 콘솔 창을 띄움
            Console.WriteLine("Debugger Initialized");
        }

        public void CloseConsole()
        {
            FreeConsole(); // 콘솔 창을 닫음
        }
    }
}