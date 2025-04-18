using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Process_Scheduling_Simulator.Classes;
using Process_Scheduling_Simulator.View;

namespace Process_Scheduling_Simulator.Classes.Scheduler
{
    public class FCFSScheduler : Scheduler
    {
        //생성자
        public FCFSScheduler(List<Process> processes, List<Processor> processors)
            : base(processes, processors) { }

        public async override Task Schedule()
        {
            //초기화
            Reset();

            var incomingProcesses = new Queue<Process>(Processes.OrderBy(p => p.ArrivalTime));
            var readyQueue = new Queue<Process>();

            Console.WriteLine("--- FCFS Simulation Start ---");

            while (CompletedProcesses.Count < Processes.Count)
            {
                int delay = 100;
                int.TryParse(Init.mainApplication?.VisDelayTextBox.Text, out delay);
                await Task.Delay(Math.Max(1, delay));

                // ---프로세스 도착---
                while (incomingProcesses.Count > 0 && incomingProcesses.Peek().ArrivalTime <= CurrentTime)
                {
                    var arrivedProcess = incomingProcesses.Dequeue();
                    readyQueue.Enqueue(arrivedProcess);
                }

                // ---프로세서에 프로세스 할당---
                foreach (var processor in Processors)
                {
                    if (processor.IsIdle && readyQueue.Count > 0)
                    {
                        var processToRun = readyQueue.Dequeue();
                        processor.AssignProcess(processToRun, CurrentTime);
                    }
                }

                // ---Tick 및 완료 처리---
                List<Process> justCompletedProcesses = new List<Process>();
                foreach (var processor in Processors)
                {
                    if (!processor.IsIdle)
                    {
                        Process completedProcess = processor.Tick(CurrentTime);

                        if (completedProcess != null)
                        {
                            // --- 수정된 부분 ---
                            // CompletionTime 속성에 값을 할당하는 대신,
                            // 완료 시간 (CurrentTime + 1)을 직접 사용하여 TurnaroundTime 계산
                            int completionTime = CurrentTime + 1; // 완료 시간 계산

                            // TurnaroundTime 계산
                            completedProcess.TurnaroundTime = completionTime - completedProcess.ArrivalTime;

                            // 대기 시간 = 반환 시간 - CPU Tick 시간
                            completedProcess.WaitingTime = Math.Max(0, completedProcess.TurnaroundTime - completedProcess.CPUTicks);

                            // NTT 계산
                            completedProcess.NormalizedTTime = (completedProcess.BurstTime > 0)
                                ? (double)completedProcess.TurnaroundTime / completedProcess.BurstTime
                                : 0;

                            justCompletedProcesses.Add(completedProcess);
                            // --- 수정 완료 ---
                        }
                    }
                }

                foreach (var p in justCompletedProcesses)
                {
                    CompletedProcesses.Add(p);
                }

                //이하 RR과 로직 동일
                CurrentTime++;

                //--- 종료 조건 확인---
                if (CompletedProcesses.Count == Processes.Count && incomingProcesses.Count == 0 && readyQueue.Count == 0 && Processors.All(p => p.IsIdle))
                {
                    Console.WriteLine($"--- FCFS Simulation Complete at Time {CurrentTime} ---");
                    break;
                }

                if (CurrentTime > 20000)
                {
                    Console.WriteLine("Warning: Simulation exceeded maximum time limit.");
                    break;
                }
            }
            CalculateAverageMetrics();
        }
    }
}