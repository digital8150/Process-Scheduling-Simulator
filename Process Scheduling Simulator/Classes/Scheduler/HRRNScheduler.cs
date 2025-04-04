using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Process_Scheduling_Simulator.Classes.Scheduler
{
    public class HRRNScheduler : Scheduler
    {
        /// <summary>
        /// HRRNScheduler 생성자
        /// </summary>
        public HRRNScheduler(List<Process> processes, List<Processor> processors)
            : base(processes, processors) { } // 부모 클래스 생성자 호출

        /// <summary>
        /// HRRN 스케줄링 알고리즘 시뮬레이션 실행
        /// </summary>
        public async override Task Schedule()
        {
            Reset(); // 시뮬레이션 상태 초기화

            // 도착할 프로세스들을 도착 시간 순으로 정렬하여 큐에 넣음
            var incomingProcesses = new Queue<Process>(Processes.OrderBy(p => p.ArrivalTime));
            // 준비 큐: 도착했지만 아직 실행되지 않은 프로세스들
            var readyQueue = new List<Process>();

            Console.WriteLine($"--- HRRN Simulation Start ---");
            Console.WriteLine($"Total Processes: {Processes.Count}, Processors: {Processors.Count}");

            // --- 메인 시뮬레이션 루프 ---
            // 모든 프로세스가 완료될 때까지 반복
            while (CompletedProcesses.Count < Processes.Count)
            {
                await Task.Delay(250);
                Console.WriteLine($"\n--- Time: {CurrentTime} ---");

                // 1. 프로세스 도착 처리
                // 현재 시간에 도착한 프로세스를 incomingProcesses에서 readyQueue로 이동
                while (incomingProcesses.Count > 0 && incomingProcesses.Peek().ArrivalTime <= CurrentTime)
                {
                    var arrivedProcess = incomingProcesses.Dequeue();
                    readyQueue.Add(arrivedProcess);
                    Console.WriteLine($"  Arrival: Process {arrivedProcess.Name} arrived. Added to Ready Queue.");
                }

                // 2. 프로세서 상태 기록 (Tick 전)
                // 각 프로세서가 이번 Tick 시작 시 유휴 상태였는지 기록 (시동 전력 계산용)
                var processorWasIdle = new Dictionary<Processor, bool>();
                foreach (var processor in Processors)
                {
                    processorWasIdle[processor] = processor.IsIdle;
                }

                // 3. 프로세스 할당 (HRRN 스케줄링 결정)
                // 유휴 상태인 프로세서에 할당할 프로세스를 결정
                var assignedInThisTick = new HashSet<Process>(); // 이번 틱에 이미 할당된 프로세스 추적 (중복 방지)
                foreach (var processor in Processors.OrderBy(p => p.Type)) // 예: E코어보다 P코어에 먼저 할당 시도? (선택적)
                {
                    if (processor.IsIdle && readyQueue.Count > 0)
                    {
                        // 준비 큐에 있는 프로세스들의 응답률(Response Ratio) 계산
                        Process bestProcess = null;
                        double highestRatio = -1.0;

                        Console.WriteLine($"  Processor {processor.Name} is Idle. Calculating Response Ratios in Ready Queue:");
                        foreach (var readyProcess in readyQueue)
                        {
                            // 이미 다른 프로세서에 이번 틱에 할당되었다면 건너뜀
                            if (assignedInThisTick.Contains(readyProcess)) continue;

                            int waitingTime = CurrentTime - readyProcess.ArrivalTime;
                            // BurstTime이 0 이하인 경우 예외 처리
                            if (readyProcess.BurstTime <= 0)
                            {
                                Console.WriteLine($"    - Skip {readyProcess.Name}: Invalid BurstTime ({readyProcess.BurstTime})");
                                continue;
                            }

                            // 응답률 계산: (대기시간 + 실행시간) / 실행시간
                            double responseRatio = (double)(waitingTime + readyProcess.BurstTime) / readyProcess.BurstTime;
                            Console.WriteLine($"    - Candidate {readyProcess.Name}: WT={waitingTime}, BT={readyProcess.BurstTime}, RR={responseRatio:F2}");

                            // 가장 높은 응답률을 가진 프로세스 선택
                            if (responseRatio > highestRatio)
                            {
                                highestRatio = responseRatio;
                                bestProcess = readyProcess;
                            }
                            // 응답률이 같을 경우 Tie-breaking 규칙 적용 (예: FCFS - 도착 시간 빠른 순)
                            else if (responseRatio == highestRatio)
                            {
                                if (bestProcess == null || readyProcess.ArrivalTime < bestProcess.ArrivalTime)
                                {
                                    bestProcess = readyProcess;
                                    Console.WriteLine($"      (Tie-breaking: {bestProcess.Name} chosen based on Arrival Time)");
                                }
                            }
                        }

                        // 선택된 프로세스를 프로세서에 할당
                        if (bestProcess != null)
                        {
                            readyQueue.Remove(bestProcess); // 준비 큐에서 제거
                            processor.AssignProcess(bestProcess, CurrentTime); // 프로세서에 할당
                            assignedInThisTick.Add(bestProcess); // 이번 틱 할당 목록에 추가
                            Console.WriteLine($"  Assignment: Processor {processor.Name} assigned Process {bestProcess.Name} (Highest RR: {highestRatio:F2})");
                        }
                        else
                        {
                            Console.WriteLine("  Assignment: No suitable process found in Ready Queue for this processor.");
                        }
                    }
                }

                // 4. 프로세서 시간 진행 (Tick) 및 완료 처리
                foreach (var processor in Processors)
                {
                    // 현재 Tick에서 프로세서 작업 수행 및 완료 여부 확인
                    Process completedProcess = processor.Tick(CurrentTime);

                    if (completedProcess != null)
                    {
                        // --- 프로세스 완료 처리 ---
                        int completionTime = CurrentTime + 1; // 완료 시점 (현재 Tick의 끝)
                        completedProcess.TurnaroundTime = completionTime - completedProcess.ArrivalTime;

                        // --- 실제 실행 시간 (Ticks) 계산 ---
                        int actualExecutionTicks;
                        double originalBurstTime = completedProcess.BurstTime; // 원래 작업량

                        // 프로세스가 완료된 코어의 타입을 확인
                        if (processor.Type == CoreType.P)
                        {
                            // P-Core 실행 시간: ceil(BurstTime / 2.0)
                            // PDF 명세: "P코어에 할당된 작업의 남은 일의 양이 1이어도, 1초를 소모함" [Source 10]
                            // 이는 Math.Ceiling으로 자연스럽게 처리됨
                            if (originalBurstTime <= 0)
                            {
                                actualExecutionTicks = 0; // 작업량이 0이면 실행 시간도 0
                            }
                            else
                            {
                                actualExecutionTicks = (int)Math.Ceiling(originalBurstTime / processor.PerformanceFactor); // PerformanceFactor = 2.0
                            }
                            Console.WriteLine($"  Debug: {completedProcess.Name} on P-Core. BurstTime={originalBurstTime}, Actual Ticks={actualExecutionTicks}");
                        }
                        else // E-Core
                        {
                            // E-Core 실행 시간: BurstTime (작업량과 동일)
                            actualExecutionTicks = (int)Math.Max(0, originalBurstTime); // BurstTime이 음수일 경우 대비
                            Console.WriteLine($"  Debug: {completedProcess.Name} on E-Core. BurstTime={originalBurstTime}, Actual Ticks={actualExecutionTicks}");
                        }

                        // --- WaitingTime 계산 수정 ---
                        // WaitingTime = 시스템 총 체류 시간 - 실제 CPU 실행 시간(Tick)
                        completedProcess.WaitingTime = completedProcess.TurnaroundTime - actualExecutionTicks;

                        // 계산된 WaitingTime 검증 (음수 방지 및 확인)
                        if (completedProcess.WaitingTime < 0)
                        {
                            Console.WriteLine($"!!! WARNING: Negative WaitingTime calculated for {completedProcess.Name} (WT={completedProcess.WaitingTime})");
                            Console.WriteLine($"    TT={completedProcess.TurnaroundTime}, ActualExecTicks={actualExecutionTicks}, BurstTime={originalBurstTime}, ProcType={processor.Type}");
                            // 임시 조치: 음수 WaitingTime을 0으로 조정하거나 원인 분석 필요
                            // completedProcess.WaitingTime = 0;
                        }

                        // Normalized Turnaround Time 계산 (원래 BurstTime 기준)
                        completedProcess.NormalizedTTime = (originalBurstTime > 0)
                            ? (double)completedProcess.TurnaroundTime / originalBurstTime
                            : 0;

                        CompletedProcesses.Add(completedProcess); // 완료 목록에 추가
                        Console.WriteLine($"  Completion: Process {completedProcess.Name} finished on {processor.Name}. " +
                                            $"WT: {completedProcess.WaitingTime}, TT: {completedProcess.TurnaroundTime}, NTT: {completedProcess.NormalizedTTime:F2}");
                    }
                }

                // 5. 전력 소모량 누적
                foreach (var processor in Processors)
                {
                    // Tick 시작 전 기록된 상태(`processorWasIdle[processor]`)를 사용
                    TotalPowerConsumption += GetProcessorPowerForTick(processor, CurrentTime, processorWasIdle[processor]);
                }
                Console.WriteLine($"  Power: Accumulated Power = {TotalPowerConsumption:F1}W");

                // 6. 시간 증가
                CurrentTime++;

                // --- 루프 종료 조건 및 예외 처리 ---
                // 무한 루프 방지 (안전 장치)
                if (CurrentTime > 20000) // 적절한 시간 제한 설정
                {
                    Console.WriteLine("Error: Simulation time limit exceeded. Halting.");
                    break;
                }
                // 진행 불가 상태 감지 (프로세스는 남았는데 더 이상 진행 안 될 때)
                if (incomingProcesses.Count == 0 && readyQueue.Count == 0 && Processors.All(p => p.IsIdle) && CompletedProcesses.Count < Processes.Count)
                {
                    Console.WriteLine("Error: Deadlock detected or no more progress possible. Halting.");
                    // 남은 프로세스 정보 출력 등 디버깅 정보 추가 가능
                    break;
                }
            }

            Console.WriteLine($"\n--- HRRN Simulation End ---");
            Console.WriteLine($"Total Simulation Time: {CurrentTime}");
            Console.WriteLine($"Total Power Consumed: {TotalPowerConsumption:F1}W");

            // 최종 평균 성능 지표 계산 및 출력
            CalculateAverageMetrics();
        }


        /// <summary>
        /// 지정된 프로세서의 현재 Tick 전력 소모량을 계산하는 헬퍼 메서드.
        /// </summary>
        private double GetProcessorPowerForTick(Processor processor, int currentTime, bool wasIdleBeforeTick)
        {
            // Processor 클래스의 GetCurrentTickPower 메서드를 사용하거나, 여기서 직접 계산
            // return processor.GetCurrentTickPower(currentTime, wasIdleBeforeTick); // Processor 메서드 사용 시

            // 직접 계산 시:
            if (processor.IsIdle)
            {
                return 0.0;
            }
            else
            {
                double power = processor.ActivePower;
                if (wasIdleBeforeTick)
                { // 이번 Tick에 막 시작했다면 시동 전력 추가
                    power += processor.StartupPower;
                }
                // Console.WriteLine($"   Power Calc: {processor.Name} consumes {power}W (Active: {processor.ActivePower}W, Startup: {(wasIdleBeforeTick ? processor.StartupPower : 0)}W)");
                return power;
            }
        }
    }
}
