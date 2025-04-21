using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Process_Scheduling_Simulator.View;

namespace Process_Scheduling_Simulator.Classes.Scheduler
{
    public class SRTNScheduler : Scheduler
    {
        public SRTNScheduler(List<Process> processes, List<Processor> processors)
            : base(processes, processors) { }

        public async override Task Schedule()
        {
            Reset();
            var incomingProcesses = new Queue<Process>(Processes.OrderBy(p => p.ArrivalTime));
            var readyQueue = new List<Process>();

            while (CompletedProcesses.Count < Processes.Count)
            {
                int delay = 100;
                int.TryParse(Init.mainApplication.VisDelayTextBox.Text, out delay);
                await Task.Delay(delay); // 시각화용 딜레이

                // 도착한 프로세스를 레디 큐에 추가
                while (incomingProcesses.Count > 0 && incomingProcesses.Peek().ArrivalTime <= CurrentTime)
                {
                    var arrived = incomingProcesses.Dequeue();
                    readyQueue.Add(arrived);
                }

                //유휴 프로세서에 가장 짧은 남은 시간 프로세스 할당
                foreach(var processor in Processors.OrderBy(p => p.Type))
                {
                    if (processor.IsIdle && readyQueue.Count > 0)
                    {
                        var nextProcess = readyQueue.OrderBy(p => p.RemainingTime).First();
                        readyQueue.Remove(nextProcess);
                        processor.AssignProcess(nextProcess, CurrentTime);
                    }
                }

                //가장 짧은 프로세스가 레디 큐에 있다면 선점
                foreach (var processor in Processors)
                {
                    if (processor.CurrentProcess != null && readyQueue.Count > 0)
                    {
                        var nextProcess = readyQueue.OrderBy(p => p.RemainingBurstTime).First();
                        if (nextProcess.RemainingBurstTime < processor.CurrentProcess.RemainingBurstTime)
                        {
                            readyQueue.Add(processor.PreemptProcess(CurrentTime));
                            processor.AssignProcess(nextProcess, CurrentTime);
                            readyQueue.Remove(nextProcess);
                        }
                    }
                }

                // 한 틱 실행 및 종료 확인
                foreach (var processor in Processors)
                {
                    var completedProcess = processor.Tick(CurrentTime);
                    if (completedProcess != null)
                    {
                        completedProcess.CompletionTime = CurrentTime + 1; // ⬅️ Tick 후 종료 시점 기록
                        CalculateCompletionMetrics(completedProcess, processor, CurrentTime + 1);
                        CompletedProcesses.Add(completedProcess);
                    }
                }

                CurrentTime++;

                // 정지 조건 체크
                if (CurrentTime > 10000 || (
                    incomingProcesses.Count == 0 &&
                    readyQueue.Count == 0 &&
                    Processors.All(p => p.IsIdle)))
                {
                    Console.WriteLine("정지 조건 도달. 루프 종료.");
                    break;
                }

                
            }

            Console.WriteLine("--- SRTN Scheduling Complete ---");
            CalculateAverageMetrics();
        }

        private void CalculateCompletionMetrics(Process completedProcess, Processor processor, int currentTime)
        {
            completedProcess.TurnaroundTime = completedProcess.CompletionTime - completedProcess.ArrivalTime;
            completedProcess.WaitingTime = Math.Max(0, completedProcess.TurnaroundTime - completedProcess.CPUTicks);
            completedProcess.NormalizedTTime = (completedProcess.BurstTime > 0)
                ? (double)completedProcess.TurnaroundTime / completedProcess.BurstTime
                : 0;
        }
    }
}