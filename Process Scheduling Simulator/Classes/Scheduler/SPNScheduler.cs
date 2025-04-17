using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Process_Scheduling_Simulator.View;

namespace Process_Scheduling_Simulator.Classes.Scheduler
{
    public class SPNScheduler : Scheduler
    {
        public SPNScheduler(List<Process> processes, List<Processor> processors)
            : base(processes, processors) { }

        public async override Task Schedule()
        {
            Reset();
            var incomingProcesses = new Queue<Process>(Processes.OrderBy(p => p.ArrivalTime));
            var readyQueue = new List<Process>();

            while (CompletedProcesses.Count < Processes.Count) //전체 프로세스 갯수보다 완료한 프로세스가 적은 동안 루프
            {
                int delay = 100;
                int.TryParse(Init.mainApplication.VisDelayTextBox.Text, out delay);
                await Task.Delay(delay); // 시각화 지연시간 적용 - 이 코드는 공통으로 수정하지 말아주세요

                // 도착한 프로세스를 readyQueue에 추가
                while (incomingProcesses.Count > 0 && incomingProcesses.Peek().ArrivalTime <= CurrentTime)
                {
                    var arrivedProcess = incomingProcesses.Dequeue();
                    readyQueue.Add(arrivedProcess);
                }

                // 프로세서가 비어있는 경우에만 작업 할당
                foreach (var processor in Processors.OrderBy(p => p.Type))
                {
                    if (processor.IsIdle && readyQueue.Count > 0)
                    {
                        // SPN: 가장 짧은 서비스 시간을 가진 프로세스 선택
                        var nextProcess = readyQueue.OrderBy(p => p.BurstTime).First();

                        readyQueue.Remove(nextProcess);
                        processor.AssignProcess(nextProcess, CurrentTime); // 시작 시간 설정됨
                    }
                }

                // 실행 중인 프로세스들 처리
                foreach (var processor in Processors)
                {
                    Process completedProcess = processor.Tick(CurrentTime);

                    if (completedProcess != null)
                    {
                        CalculateCompletionMetrics(completedProcess, processor, CurrentTime); // 완료 통계 계산
                        CompletedProcesses.Add(completedProcess);
                        // Console.WriteLine($"  Completion: {completedProcess.Name} finished on {processor.Name}.");
                    }
                }

                CurrentTime++;
            }
            CalculateAverageMetrics();
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
