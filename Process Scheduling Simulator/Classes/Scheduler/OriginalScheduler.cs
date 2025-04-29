using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Process_Scheduling_Simulator.View;

namespace Process_Scheduling_Simulator.Classes.Scheduler
{
    public class OriginalScheduler : Scheduler
    {
        private int customThreshold = 0; // 커스텀 임계값
        private int schedulerIndex = 0;
        private bool preferPCore = false;
        public OriginalScheduler(List<Process> processes, List<Processor> processors, int customThreshold, int schedulerIndex, bool preferPCore)
            : base(processes, processors) { 
            this.customThreshold = customThreshold;
            this.schedulerIndex = schedulerIndex;
            this.preferPCore = preferPCore;
        }


        public async override Task Schedule()
        {
            Reset();
            var incomingProcesses = new Queue<Process>(Processes.OrderBy(p => p.ArrivalTime));
            var readyQueue = new List<Process>();
            var isolationQueue = new Queue<Process>();
            var colorMap = new Dictionary<Process, Brush>();
            int isolationQueueCredit = 0;
            Processor isolationProcessor = null;

            double avgBurstTime = -1;
            while (CompletedProcesses.Count < Processes.Count) //전체 프로세스 갯수보다 완료한 프로세스가 적은 동안 루프
            {
                int delay = 100;
                int.TryParse(Init.mainApplication.VisDelayTextBox.Text, out delay);
                await Task.Delay(delay); // 시각화 지연시간 적용 - 이 코드는 공통으로 수정하지 말아주세요

                Console.WriteLine($"//-- Time : {CurrentTime} ---//");

                //프로세스 도착
                while (incomingProcesses.Count > 0 && incomingProcesses.Peek().ArrivalTime <= CurrentTime)
                {
                    var arrivedProcess = incomingProcesses.Dequeue();
                    readyQueue.Add(arrivedProcess);
                    Console.WriteLine($"Process Arrived : {arrivedProcess.Name}\t Time : {CurrentTime}");
                }

                //금쪽이 큐 할당된 프로세서 관리
                if (isolationProcessor != null)
                {

                    if (isolationQueueCredit <= 0)
                    {
                        if (!isolationProcessor.IsIdle)
                        {
                            isolationQueue.Enqueue(isolationProcessor.PreemptProcess(CurrentTime));
                        }
                        isolationProcessor = null;
                    }
                }

                //유휴 프로세서에 프로세스 할당
                //isolationQueueCredit이 남아있고, isolationQueue에 프로세스가 있다면
                if (isolationQueueCredit > 0 && isolationQueue.Count > 0 && isolationProcessor == null)
                {

                    foreach (var processor in preferPCore ? Processors.OrderBy(p=>p.Type) : Processors.OrderByDescending(p=>p.Type))
                    {
                        if (processor.IsIdle)
                        {
                            var process = isolationQueue.Dequeue();
                            processor.AssignProcess(process, CurrentTime);
                            isolationProcessor = processor;
                            break;
                        }
                    }
                }

                //유휴 프로세서에 프로세스 할당 - 일반큐
                foreach (var processor in Processors.OrderBy(p => p.Type))
                {
                    if (processor.IsIdle && readyQueue.Count > 0)
                    {
                        processor.AssignProcess(FindNextPrcoess(readyQueue), CurrentTime);
                    }
                }



                //금쪽이 선별
                //모든 프로세서가 바쁨
                if (isAllProcessorBusy() && avgBurstTime > 0 && readyQueue.Count != 0)
                {
                    foreach (var processor in Processors)
                    {
                        if (processor.CurrentProcess.CPUTicks > Math.Max(avgBurstTime,customThreshold) && processor != isolationProcessor)
                        {
                            Process goldenkid = processor.PreemptProcess(CurrentTime);
                            colorMap.Add(goldenkid, goldenkid.ProcessColor);
                            goldenkid.ProcessColor = Brushes.LightGoldenrodYellow; //금쪽이 색상 변경
                            isolationQueue.Enqueue(goldenkid);
                            Console.WriteLine($"금쪽이 선별 및 선점 : {goldenkid.Name}\t금쪽이 큐 Count : {isolationQueue.Count}\t평균BT : {avgBurstTime}\t금쪽이 현재 CPUTick:{goldenkid.CPUTicks}");
                            //금쪽이 선점하였으므로 readyQueue의 일반 프로세스 할당
                            
                            if(readyQueue.Count>0) processor.AssignProcess(FindNextPrcoess(readyQueue), CurrentTime); //일반 프로세스 할당
                        }
                    }
                }
                else if (!isAllProcessorBusy()) //시스템이 바쁘지 않을 때, 금쪽이큐 운영은 하지 않음
                {
                    while (isolationQueue.Count > 0)
                    {
                        Process promotionedProcess = isolationQueue.Dequeue();
                        readyQueue.Add(promotionedProcess);
                        promotionedProcess.ProcessColor = colorMap[promotionedProcess];
                        colorMap.Remove(promotionedProcess);
                        Console.WriteLine($"일반큐 승급: {promotionedProcess.Name}\t금쪽이 큐 Count : {isolationQueue.Count}");
                    }

                }



                //프로세서 틱 처리
                foreach (var processor in Processors)
                {
                    if(processor == isolationProcessor)
                    {
                        isolationQueueCredit--;
                        Console.WriteLine($"금쪽이 프로세스 : {processor.CurrentProcess.Name} @ {processor.Name}\tRemainCredit:{isolationQueueCredit}");
                        Process completedProcess = processor.Tick(CurrentTime);
                        if (completedProcess != null)
                        {
                            CalculateCompletionMetrics(completedProcess, processor, CurrentTime); // 완료 통계 계산
                            CompletedProcesses.Add(completedProcess);
                            //최근 완료된 프로세스의 평균 BurstTime 계산
                            if (avgBurstTime < 0)
                            {
                                avgBurstTime = completedProcess.CPUTicks;
                            }
                            else
                            {
                                avgBurstTime = (avgBurstTime + completedProcess.CPUTicks) / 2;
                            }
                            isolationProcessor = null;
                            // Console.WriteLine($"  Completion: {completedProcess.Name} finished on {processor.Name}.");
                        }
                        
                    }
                    else
                    {
                        Process completedProcess = processor.Tick(CurrentTime);

                        if (completedProcess != null)
                        {
                            CalculateCompletionMetrics(completedProcess, processor, CurrentTime); // 완료 통계 계산
                            CompletedProcesses.Add(completedProcess);
                            //최근 완료된 프로세스의 평균 BurstTime 계산
                            if (avgBurstTime < 0)
                            {
                                avgBurstTime = completedProcess.CPUTicks;
                            }
                            else
                            {
                                avgBurstTime = (avgBurstTime + completedProcess.CPUTicks) / 2;
                            }
                            // Console.WriteLine($"  Completion: {completedProcess.Name} finished on {processor.Name}.");
                        }
                    }


                }



                //4초마다 isolationCredit 에 2부여
                if (CurrentTime % 4 == 0)
                {
                    isolationQueueCredit = 2;
                    Console.WriteLine($"금쪽이 크레딧 부여 : {isolationQueueCredit}");
                }

                //프로세스 할당
                //프로세서 틱 처리 등
                CurrentTime++;
            }
            CalculateAverageMetrics();
        }

        private Boolean isAllProcessorBusy()
        {
            foreach (var processor in Processors)
            {
                if (processor.IsIdle)
                {
                    return false;
                }
            }
            return true;
        }

        private Process FindNextPrcoess(List<Process> readyQueue)
        {
            Process nextProcess = null;
            switch (schedulerIndex)
            {
                case 0: // FCFS
                    nextProcess = readyQueue.OrderBy(p => p.ArrivalTime).First();
                    break;
                case 1: // SPN
                    nextProcess = readyQueue.OrderBy(p => p.BurstTime).First();
                    break;
                case 2: // HRRN
                    double highestRatio = -1.0;
                    foreach (var process in readyQueue)
                    {
                        double ratio = ((CurrentTime - process.ArrivalTime - process.CPUTicks) + process.BurstTime) / process.BurstTime;
                        if(ratio > highestRatio)
                        {
                            highestRatio = ratio;
                            nextProcess = process;
                        }
                    }
                    break;
                default:
                    throw new InvalidOperationException("Invalid scheduler index.");
            }
            readyQueue.Remove(nextProcess);
            return nextProcess;
        }

        private void CalculateCompletionMetrics(Process completedProcess, Processor processor, int currentTime)
        {
            int completionTime = currentTime + 1;
            completedProcess.TurnaroundTime = completionTime - completedProcess.ArrivalTime;

            int actualExecutionTicks;
            double originalBurstTime = completedProcess.BurstTime;

            // 실제 실행 시간 계산 (완료된 코어 기준 - 이 부분은 시나리오에 따라 더 정교화될 수 있음)
            actualExecutionTicks = completedProcess.CPUTicks;

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
