using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Process_Scheduling_Simulator.Classes.Scheduler
{
    public abstract class Scheduler
    {
        public List<Process> Processes { get; protected set; }
        public List<Processor> Processors { get; protected set; }
        public List<Process> CompletedProcesses { get; protected set; }
        public int CurrentTime { get; protected set; }

        public double TotalPCorePower { get; protected set; } // 최종 결과 저장용 (선택적)
        public double TotalECorePower { get; protected set; } // 최종 결과 저장용 (선택적)
        public double OverallTotalPower { get; protected set; } // 최종 결과 저장용 (선택적)


        protected Scheduler(List<Process> processes, List<Processor> processors)
        {
            Processes = processes.Select(p => new Process(p.Name, p.ArrivalTime, p.BurstTime, p.ProcessColor)).ToList();
            Processors = processors;
            CompletedProcesses = new List<Process>();
        }

        public abstract Task Schedule();

        public virtual void Reset()
        {
            CurrentTime = 0;
            CompletedProcesses.Clear();
            foreach (var p in Processes) { p.ResetState(); }
            foreach (var proc in Processors) { proc.ResetState(); } // 프로세서 상태 및 전력 리셋
            Console.WriteLine("Scheduler and Processors Reset.");
            TotalPCorePower = 0;
            TotalECorePower = 0;
            OverallTotalPower = 0;
        }

        public virtual void CalculateAverageMetrics()
        {
            if (CompletedProcesses.Count > 0)
            {
                double avgWT = CompletedProcesses.Average(p => p.WaitingTime);
                double avgTT = CompletedProcesses.Average(p => p.TurnaroundTime);
                double avgNTT = CompletedProcesses.Average(p => p.NormalizedTTime);
                Console.WriteLine($"Average Waiting Time (WT): {avgWT:F2}");
                Console.WriteLine($"Average Turnaround Time (TT): {avgTT:F2}");
                Console.WriteLine($"Average Normalized Turnaround Time (NTT): {avgNTT:F2}");
            }
            else
            {
                Console.WriteLine("No processes completed.");
            }

            TotalPCorePower = Processors.Where(p => p.Type == CoreType.P).Sum(p => p.TotalConsumedPower);
            TotalECorePower = Processors.Where(p => p.Type == CoreType.E).Sum(p => p.TotalConsumedPower);
            OverallTotalPower = TotalPCorePower + TotalECorePower;

            Console.WriteLine($"Total P-Core Power Consumed: {TotalPCorePower:F1}W");
            Console.WriteLine($"Total E-Core Power Consumed: {TotalECorePower:F1}W");
            Console.WriteLine($"Overall Total Power Consumed: {OverallTotalPower:F1}W");
        }
    }
}