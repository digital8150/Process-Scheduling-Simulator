using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Process_Scheduling_Simulator.View;
using System.Collections.Concurrent;
//Processor에 없는 RemainingBurstTime 추가
//각 함수가 무엇을 하는지 작성

namespace Process_Scheduling_Simulator.Classes.Scheduler
{
    public class RRScheduler : Scheduler
    {
        //입력받을 timeQauntum 변수
        private readonly int timeQuantum;
        // 현재 특정 프로세서에서 프로세스가 사용한 시간
        private Dictionary<Processor, int> currentQuantumUsage;
        // 프로세스가 대기를 시작한 시간
        private Dictionary<Process, int> processWaitStartTime;

        //생성자
        public RRScheduler(List<Process> processes, List<Processor> processors, int quantum)
            : base(processes, processors)//부모클래스 호출 및 초기화
        {
            //timequantum이 음수면 예외 발생
            if (quantum <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantum), "Quantum must be positive.");
            }
            timeQuantum = quantum;
            currentQuantumUsage = new Dictionary<Processor, int>();
            processWaitStartTime = new Dictionary<Process, int>();
        }

        //초기화
        public override void Reset()
        {
            base.Reset();
            currentQuantumUsage.Clear();
            processWaitStartTime.Clear();

            //Debug
            Console.WriteLine("RRScheduler specific state reset.");
        }

        public async override Task Schedule()
        {
            //초기화 후 작업
            Reset();

            // 도착 시간 순으로 정렬된 초기 프로세스 목록 준비
            var incomingProcesses = new Queue<Process>(Processes.OrderBy(p => p.ArrivalTime));
            var readyQueue = new Queue<Process>();

            Console.WriteLine($"--- RR Simulation Start (Quantum={timeQuantum}) ---");
            // ... (시작 로그)

            // 메인 시뮬레이션 루프
            //모든 프로세스가 작업 끝날 때까지 진행
            while (CompletedProcesses.Count < Processes.Count)
            {
                int delay = 100;
                //지연 시간 입력 확인, 정수 확인
                if (Init.mainApplication != null && int.TryParse(Init.mainApplication.VisDelayTextBox.Text, out int parsedDelay))
                {
                    delay = parsedDelay;
                }
                if (delay > 0) // 필요한 경우에만 지연
                {
                    await Task.Delay(delay);
                }

                // --- 프로세스 도착 ---
                // 현재 시간에 도착한 프로세스를 준비 큐에 추가
                while (incomingProcesses.Count > 0 && incomingProcesses.Peek().ArrivalTime <= CurrentTime)
                {
                    var arrivedProcess = incomingProcesses.Dequeue();
                    Console.WriteLine($"Time {CurrentTime}: Process {arrivedProcess.Name} arrived. Adding to Ready Queue.");
                    readyQueue.Enqueue(arrivedProcess);
                    // 이 프로세스가 대기를 시작하는 시간 기록
                    if (!processWaitStartTime.ContainsKey(arrivedProcess))
                    {
                        processWaitStartTime[arrivedProcess] = CurrentTime;
                    }
                }

                //--- quantum 만료 확인 및 선점 ---
                // 현재 프로세스를 *실행 중인* 프로세서들을 순회
                var processorsToCheck = Processors.Where(p => !p.IsIdle).ToList();
                foreach (var processor in processorsToCheck)
                {
                    // 프로세서가 실제로 딕셔너리에 있는지 확인 (!IsIdle이면 있어야 함)
                    if (currentQuantumUsage.TryGetValue(processor, out int usage) && usage >= timeQuantum)
                    {
                        Console.WriteLine($"Time {CurrentTime}: Quantum expired for {processor.CurrentProcess.Name} on {processor.Name}. Preempting.");
                        Process preemptedProcess = processor.PreemptProcess(CurrentTime);

                        if (preemptedProcess != null)
                        {
                            // 선점된 프로세스를 준비 큐에 다시 추가
                            readyQueue.Enqueue(preemptedProcess);

                            // 다시 대기를 시작하는 시간 기록
                            if (!processWaitStartTime.ContainsKey(preemptedProcess))
                            {
                                processWaitStartTime[preemptedProcess] = CurrentTime;
                            }
                            else
                            {
                                // 만약 이미 대기 중이었다면 (예: 도착 직후 선점?), 시작 시간 업데이트.
                                // 적절한 도착/할당 로직에서는 발생 가능성이 낮지만, 예외 상황을 처리합니다.
                                processWaitStartTime[preemptedProcess] = CurrentTime;
                            }
                        }
                        // 이 프로세서는 이제 유휴 상태이거나 다음에 할당될 것이므로 퀀텀 사용량을 리셋
                        currentQuantumUsage.Remove(processor);
                    }
                }


                // --- 유휴 프로세서에 프로세스 할당 ---
                //대기중인 프로세서들을 순회
                foreach (var processor in Processors.Where(p => p.IsIdle))
                {
                    if (readyQueue.Count > 0)
                    {
                        //큐에 대기중인 프로세스 중 제일 먼저 들어온 프로세스 꺼내기
                        var processToAssign = readyQueue.Dequeue();

                        // 대기 시간 누적
                        if (processWaitStartTime.TryGetValue(processToAssign, out int waitStartTime))
                        {
                            // 같은 틱 내에서 여러 번 선점되더라도 WaitingTime이 감소하지 않도록 보장
                            int timeSpentWaiting = CurrentTime - waitStartTime;
                            if (timeSpentWaiting > 0)
                                processToAssign.WaitingTime += timeSpentWaiting;

                            processWaitStartTime.Remove(processToAssign); // 대기에서 제거
                        }
                        else
                            Console.WriteLine($"Warning: Process {processToAssign.Name} assigned without a wait start time at {CurrentTime}");

                        Console.WriteLine($"Time {CurrentTime}: Assigning {processToAssign.Name} (Remaining: {(processToAssign.RemainingBurstTime > 0 ? processToAssign.RemainingBurstTime : processToAssign.BurstTime):F1}) to {processor.Name} ({processor.Type}-Core).");

                        //새로 프로세서 할당한 프로세스는 timequantum 0부터 시작
                        if (processor.AssignProcess(processToAssign, CurrentTime))
                            currentQuantumUsage[processor] = 0;

                        //할당 실패
                        else
                        {
                            Console.WriteLine($"Error: Failed to assign {processToAssign.Name} to supposedly idle processor {processor.Name} at time {CurrentTime}. Re-queuing.");
                            // 다음 사이클을 위해 큐의 맨 앞에 다시 넣음
                            var tempQueue = new Queue<Process>();
                            tempQueue.Enqueue(processToAssign);
                            while (readyQueue.Count > 0) tempQueue.Enqueue(readyQueue.Dequeue());
                            readyQueue = tempQueue;
                            // 대기 시작 시간 마커를 다시 설정
                            if (!processWaitStartTime.ContainsKey(processToAssign))
                                processWaitStartTime[processToAssign] = CurrentTime;
                        }
                    }
                }

                // --- 모든 프로세서에서 틱 실행 ---
                var activeProcessorsBeforeTick = Processors.Where(p => !p.IsIdle).ToList();

                foreach (var processor in Processors)
                {
                    string runningProcessName = processor.CurrentProcess?.Name ?? "Idle";
                    Process completedProcess = processor.Tick(CurrentTime); // 프로세서가 작업량 감소, 전력, 간트차트 처리

                    // --- 프로세스 완료 처리 ---
                    if (completedProcess != null)
                    {
                        int completionTime = CurrentTime + 1;
                        Console.WriteLine($"Time {completionTime}: Process {completedProcess.Name} COMPLETED on {processor.Name}.");
                        completedProcess.TurnaroundTime = completionTime - completedProcess.ArrivalTime;
                        //NTT 계산
                        completedProcess.NormalizedTTime = (completedProcess.BurstTime > 0)
                            ? (double)completedProcess.TurnaroundTime / completedProcess.BurstTime
                            : 0;

                        // 중요 : WT(대기시간) 계산을 위해 여기서 실제 실행 시간을 계산할 필요는 없음,
                        // WT는 실행되지 않은 시간을 기준으로 누적되었기 때문.
                        // 기본 Process 클래스가 원래 BurstTime을 저장하고 있음.

                        CompletedProcesses.Add(completedProcess);
                        currentQuantumUsage.Remove(processor); // 퀀텀 추적에서 제거
                        processWaitStartTime.Remove(completedProcess); //확실히 제거

                        Console.WriteLine($"  Metrics for {completedProcess.Name}: TT={completedProcess.TurnaroundTime}, WT={completedProcess.WaitingTime}, NTT={completedProcess.NormalizedTTime:F2}");
                    }

                    // --- 진행 중인 프로세스의 타임퀀텀 사용량 업데이트 --
                    else if (!processor.IsIdle)
                    {
                        if (currentQuantumUsage.ContainsKey(processor))
                            currentQuantumUsage[processor]++;
                        else
                            currentQuantumUsage[processor] = 1;
                    }
                    else
                        currentQuantumUsage.Remove(processor);
                }


                //시간 진행
                CurrentTime++;

                // --- 루프 종료 조건 ---
                if (CurrentTime > 20000) // 무한 루프 방지 (20000으로 설정)
                {
                    Console.WriteLine("Warning: Simulation exceeded maximum time limit (20000 ticks). Stopping.");
                    break;
                }

                // 시뮬레이션 종료 여부 확인
                if (CompletedProcesses.Count >= Processes.Count)
                {
                    Console.WriteLine($"--- All {Processes.Count} processes completed at Time {CurrentTime}. ---");
                    break; // 루프 정상 종료
                }

                // 교착 상태/유휴 상태 확인 (들어오는 프로세스 없고, 준비 큐 비어있고, 모든 프로세서 유휴 상태)
                // 확인 필요
                if (incomingProcesses.Count == 0 && readyQueue.Count == 0 && Processors.All(p => p.IsIdle) && CompletedProcesses.Count < Processes.Count)
                {
                    Console.WriteLine($"Warning: Simulation stalled at Time {CurrentTime}. IncomingQ={incomingProcesses.Count}, ReadyQ={readyQueue.Count}, IdleProcs={Processors.Count(p => p.IsIdle)}, Completed={CompletedProcesses.Count}/{Processes.Count}. Stopping.");
                    break; // 예기치 않은 상황 발생 시 무한 루프 방지
                }

            }

            Console.WriteLine("--- RR Simulation End ---");
            CalculateAverageMetrics();
        }
    }
}