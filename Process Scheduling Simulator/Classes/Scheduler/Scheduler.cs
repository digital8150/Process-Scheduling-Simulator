using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Process_Scheduling_Simulator.Classes.Scheduler
{

    public abstract class Scheduler
    {
        // --- Properties ---
        public List<Process> Processes { get; protected set; }       // 시뮬레이션 대상 프로세스 목록 (원본 또는 복사본)
        public List<Processor> Processors { get; protected set; }     // 사용 가능한 프로세서(코어) 목록
        public List<Process> CompletedProcesses { get; protected set; } // 실행 완료된 프로세스 목록
        public int CurrentTime { get; protected set; }               // 현재 시뮬레이션 시간
        public double TotalPowerConsumption { get; protected set; } // 누적 총 전력 소모량
                                                                    // 필요에 따라 평균 WT, TT, NTT 등 결과 저장 변수 추가 가능

        /// <summary>
        /// 스케줄러 생성자
        /// </summary>
        /// <param name="processes">스케줄링할 프로세스 목록</param>
        /// <param name="processors">사용할 프로세서 목록</param>
        protected Scheduler(List<Process> processes, List<Processor> processors)
        {
            // 원본 리스트를 수정하지 않도록 프로세스 목록 복사 (Deep Copy 권장)
            // 여기서는 간단히 새 리스트를 만들고, Reset에서 상태 초기화
            Processes = processes.Select(p => new Process(p.Name, p.ArrivalTime, p.BurstTime, p.ProcessColor)
            {
                // 생성자에서 RemainingBurstTime = BurstTime 처리 가정
                // 필요시 다른 초기값 복사
            }).ToList();

            Processors = processors; // 프로세서 목록 참조
            CompletedProcesses = new List<Process>();
        }

        /// <summary>
        /// 스케줄링 시뮬레이션을 실행하는 추상 메서드. 파생 클래스에서 구현해야 함.
        /// </summary>
        public abstract Task Schedule();

        /// <summary>
        /// 시뮬레이션 상태를 초기화하는 메서드.
        /// </summary>
        public virtual void Reset()
        {
            CurrentTime = 0;
            TotalPowerConsumption = 0.0;
            CompletedProcesses.Clear();

            // 각 프로세스의 상태 초기화 (특히 남은 실행 시간)
            foreach (var p in Processes)
            {
                p.RemainingBurstTime = p.BurstTime; // 남은 시간을 원래 BurstTime으로 복원
                p.WaitingTime = 0;
                p.TurnaroundTime = 0;
                p.NormalizedTTime = 0.0;
                // 필요시 다른 상태 초기화
            }

            // 각 프로세서의 상태 초기화
            foreach (var proc in Processors)
            {
                // Processor 클래스에 Reset 메서드가 있다면 호출하는 것이 좋음
                // 예: proc.ResetState();
                // 없다면, 여기서 직접 초기화 (예: 선점 메서드 호출로 강제 초기화)
                if (!proc.IsIdle)
                {
                    // 안전하게 하기 위해, 실행 중인 프로세스가 있다면 강제로 제거 (이전 실행 잔여물 처리)
                    // PreemptProcess는 Gantt에도 그리므로, Reset용 메서드가 더 적합할 수 있음
                    proc.PreemptProcess(0); // 현재 시간을 0으로 가정하고 선점시켜 초기화
                }
                // proc._wasIdleLastTick = true; // Processor 내부 상태도 초기화 필요
            }
            Console.WriteLine("Scheduler and Processors Reset.");
        }

        /// <summary>
        /// 완료된 프로세스들의 평균 성능 지표를 계산하고 출력하는 메서드 (옵션).
        /// </summary>
        public virtual void CalculateAverageMetrics()
        {
            if (CompletedProcesses.Count == 0)
            {
                Console.WriteLine("No processes completed.");
                return;
            }

            double avgWT = CompletedProcesses.Average(p => p.WaitingTime);
            double avgTT = CompletedProcesses.Average(p => p.TurnaroundTime);
            double avgNTT = CompletedProcesses.Average(p => p.NormalizedTTime); // Process 클래스에서 double로 가정

            Console.WriteLine($"Average Waiting Time (WT): {avgWT:F2}");
            Console.WriteLine($"Average Turnaround Time (TT): {avgTT:F2}");
            Console.WriteLine($"Average Normalized Turnaround Time (NTT): {avgNTT:F2}");

            // 결과를 저장해야 한다면 여기에 로직 추가
        }
    }
}
