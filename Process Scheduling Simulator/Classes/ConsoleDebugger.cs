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

        /*
        public async void postMessage(string message)

        {

            Console.WriteLine($"[+] {message}");

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string apiEndpoint = $"https://gcp.codingbot.kr/api/v2/postlog?channel=1273085983742754878&title=DebugInfo&message={message}&t_f1=ticketId&m_f1={refid}&t_f2=username&m_f2={Logon.mainwindow.username}";

                    HttpResponseMessage response = await client.GetAsync(apiEndpoint);
                }
            }
            catch (Exception ex2)
            {
                Console.WriteLine($"Error occured while sending error logs : {ex2.Message}");
            }

        }
        */

        public void writeMessage(string message)
        {

            Console.WriteLine($"[-] {message}");

        }
    }
}