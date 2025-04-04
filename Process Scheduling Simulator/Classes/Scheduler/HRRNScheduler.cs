using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Process_Scheduling_Simulator.View;

namespace Process_Scheduling_Simulator.Classes.Scheduler
{
    public class HRRNScheduler : Scheduler
    {
        public HRRNScheduler(List<Process> processes, List<Processor> processors)
            : base(processes, processors) { }

        public async override Task Schedule()
        {
            Reset();
            var incomingProcesses = new Queue<Process>(Processes.OrderBy(p => p.ArrivalTime));
            var readyQueue = new List<Process>();

            Console.WriteLine($"--- HRRN Simulation Start ---");
            // ... (시작 로그)

            while (CompletedProcesses.Count < Processes.Count)
            {
                int delay = 100;
                int.TryParse(Init.mainApplication.VisDelayTextBox.Text, out delay); // 지연 시간 조절
                await Task.Delay(delay); // 지연 시간 조절
                // Console.WriteLine($"\n--- Time: {CurrentTime} ---");

                // 1. Process Arrival
                while (incomingProcesses.Count > 0 && incomingProcesses.Peek().ArrivalTime <= CurrentTime)
                {
                    var arrivedProcess = incomingProcesses.Dequeue();
                    readyQueue.Add(arrivedProcess);
                    // Console.WriteLine($"  Arrival: Process {arrivedProcess.Name} arrived.");
                }

                // 3. Process Assignment (HRRN)
                var assignedInThisTick = new HashSet<Process>();
                foreach (var processor in Processors.OrderBy(p => p.Type))
                {
                    if (processor.IsIdle && readyQueue.Count > 0) // 유휴 프로세서 & 대기 프로세스 존재
                    {
                        Process bestProcess = FindBestProcessHRRN(readyQueue, assignedInThisTick, CurrentTime);

                        if (bestProcess != null)
                        {
                            readyQueue.Remove(bestProcess);
                            // AssignProcess에서 시동 전력 처리!
                            processor.AssignProcess(bestProcess, CurrentTime);
                            assignedInThisTick.Add(bestProcess);
                            // Console.WriteLine($"  Assignment: {processor.Name} assigned {bestProcess.Name}");
                        }
                    }
                }

                // 4. Processor Tick & Completion Handling
                foreach (var processor in Processors)
                {
                    // Tick 메서드 내에서 활성 전력 누적!
                    Process completedProcess = processor.Tick(CurrentTime);

                    if (completedProcess != null)
                    {
                        CalculateCompletionMetrics(completedProcess, processor, CurrentTime); // 완료 통계 계산
                        CompletedProcesses.Add(completedProcess);
                        // Console.WriteLine($"  Completion: {completedProcess.Name} finished on {processor.Name}.");
                    }
                }
                CurrentTime++;

                // ... (무한 루프 방지 및 데드락 감지 로직)
                if (CurrentTime > 20000) { Console.WriteLine("Error: Simulation time limit exceeded."); break; }
                if (incomingProcesses.Count == 0 && readyQueue.Count == 0 && Processors.All(p => p.IsIdle) && CompletedProcesses.Count < Processes.Count)
                { Console.WriteLine("Error: Deadlock detected or no more progress possible."); break; }

            } // End Main Simulation Loop

            Console.WriteLine($"\n--- HRRN Simulation End ---");
            Console.WriteLine($"Total Simulation Time: {CurrentTime}");
            CalculateAverageMetrics(); // 최종 통계 및 전력 출력
        }


        private Process FindBestProcessHRRN(List<Process> readyQueue, HashSet<Process> assignedInThisTick, int currentTime)
        {
            Process bestProcess = null;
            double highestRatio = -1.0;

            foreach (var readyProcess in readyQueue.Where(rp => !assignedInThisTick.Contains(rp)).ToList())
            {
                if (readyProcess.BurstTime <= 0) continue;
                int waitingTime = currentTime - readyProcess.ArrivalTime;
                // 응답률 계산 시 waitingTime이 음수가 되지 않도록 보장 (ArrivalTime > currentTime 인 경우는 없어야 함)
                waitingTime = Math.Max(0, waitingTime);
                double responseRatio = (double)(waitingTime + readyProcess.BurstTime) / readyProcess.BurstTime;

                if (responseRatio > highestRatio)
                {
                    highestRatio = responseRatio;
                    bestProcess = readyProcess;
                }
                else if (responseRatio == highestRatio && (bestProcess == null || readyProcess.ArrivalTime < bestProcess.ArrivalTime))
                {
                    bestProcess = readyProcess; // Tie-breaking: FCFS
                }
            }
            return bestProcess;
        }

        private void CalculateCompletionMetrics(Process completedProcess, Processor processor, int currentTime)
        {
            int completionTime = currentTime + 1;
            completedProcess.TurnaroundTime = completionTime - completedProcess.ArrivalTime;

            int actualExecutionTicks;
            double originalBurstTime = completedProcess.BurstTime;

            // 실제 실행 시간 계산 (완료된 코어 기준 - 이 부분은 시나리오에 따라 더 정교화될 수 있음)
            if (processor.Type == CoreType.P)
            {
                actualExecutionTicks = (originalBurstTime <= 0) ? 0 : (int)Math.Ceiling(originalBurstTime / processor.PerformanceFactor);
            }
            else
            {
                actualExecutionTicks = (int)Math.Max(0, originalBurstTime);
            }

            completedProcess.WaitingTime = Math.Max(0, completedProcess.TurnaroundTime - actualExecutionTicks); // 음수 방지
            completedProcess.NormalizedTTime = (originalBurstTime > 0) ? (double)completedProcess.TurnaroundTime / originalBurstTime : 0;

            if (completedProcess.WaitingTime < 0) // 혹시 모를 음수 WT 재확인
            {
                Console.WriteLine($"!!! WARNING: Negative WT calculated for {completedProcess.Name}. Clamped to 0.");
                completedProcess.WaitingTime = 0;
            }
        }
    }
}