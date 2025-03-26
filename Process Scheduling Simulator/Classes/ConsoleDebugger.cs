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

            Console.WriteLine("Testing Gantt!");

            GanttChartPrototype ganttChartWindow = Init.ganttChartPrototype;

            // 프로세서 추가
            int cpu1Index = ganttChartWindow.AddProcessor("CPU 1"); // 반환값: 0
            int cpu2Index = ganttChartWindow.AddProcessor("CPU 2"); // 반환값: 1
            int ioIndex = ganttChartWindow.AddProcessor("I/O Device"); // 반환값: 2
            ganttChartWindow.AddProcessor("Network");            // 반환값: 3

            // 간트 바 그리기 (예시 데이터)
            // DrawGanttBar(double startTime, double endTime, int processorIndex, string processName, Brush barColor)

            // CPU 1 작업
            ganttChartWindow.DrawGanttBar(0, 5, cpu1Index, "P1", Brushes.LightCoral);
            ganttChartWindow.DrawGanttBar(8, 12, cpu1Index, "P3", Brushes.LightCoral);
            ganttChartWindow.DrawGanttBar(15, 18, cpu1Index, "P1", Brushes.LightCoral); // P1이 다시 실행

            // CPU 2 작업
            ganttChartWindow.DrawGanttBar(2, 7, cpu2Index, "P2", Brushes.LightSkyBlue);
            ganttChartWindow.DrawGanttBar(12, 16, cpu2Index, "P4", Brushes.LightSkyBlue);

            // I/O 작업
            ganttChartWindow.DrawGanttBar(5, 8, ioIndex, "P1 (I/O)", Brushes.LightGreen); // P1의 I/O 대기
            ganttChartWindow.DrawGanttBar(7, 11, ioIndex, "P2 (I/O)", Brushes.LightGreen); // P2의 I/O 대기

            // Network 작업
            ganttChartWindow.DrawGanttBar(11, 15, 3, "P2 (Net)", Brushes.LightYellow); // P2의 Network 작업 (인덱스 3)


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