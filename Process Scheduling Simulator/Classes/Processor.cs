using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Process_Scheduling_Simulator.Classes
{
    using System;
    using System.Windows.Media; // Brush 사용을 위해 추가
    using Process_Scheduling_Simulator.View;

    // Init 클래스가 정의되어 있고, public static MainWindow mainApplication; 멤버가 있다고 가정합니다.
    // 예시:
    // public static class Init
    // {
    //     public static MainWindow mainApplication;
    // }
    // MainWindow 클래스에는 DrawGanttBar 메서드가 public으로 정의되어 있다고 가정합니다.
    // 예시:
    // public partial class MainWindow : Window
    // {
    //     // ... 기존 코드 ...
    //     public void DrawGanttBar(double startTime, double endTime, int processorIndex, string processName, Brush barColor)
    //     {
    //         // MainWindow의 Gantt 차트 그리기 로직 구현
    //         // 예: ganttChartControl.DrawGanttBar(startTime, endTime, processorIndex, processName, barColor);
    //     }
    //     // ...
    // }


    public enum CoreType { P, E } // 코어 타입을 나타내는 열거형

    public class Processor
    {
        // --- Public Properties ---
        public string Name { get; private set; }
        public CoreType Type { get; private set; }
        public Process CurrentProcess { get; private set; }
        public bool IsIdle => CurrentProcess == null;
        public double PerformanceFactor { get; private set; }
        public double ActivePower { get; private set; }
        public double StartupPower { get; private set; }

        // --- Internal State ---
        private double _workRemaining;
        private int _startTimeCurrentProcess;
        private bool _wasIdleLastTick;

        // --- Dependencies ---
        // GanttChart 직접 참조 대신, 해당 프로세서의 Gantt 차트 상 인덱스만 저장
        private readonly int _processorIndexInGantt;

        /// <summary>
        /// Processor 클래스 생성자
        /// </summary>
        /// <param name="name">프로세서 이름</param>
        /// <param name="type">코어 타입 (P 또는 E)</param>
        /// <param name="ganttIndex">GanttChart에서의 행 인덱스</param>
        public Processor(string name, CoreType type, int ganttIndex) // GanttChart 참조 제거
        {
            Name = name;
            Type = type;
            _processorIndexInGantt = ganttIndex; // Gantt 인덱스는 여전히 필요
            CurrentProcess = null;
            _workRemaining = 0;
            _startTimeCurrentProcess = -1;
            _wasIdleLastTick = true;

            if (type == CoreType.P)
            {
                PerformanceFactor = 2.0;
                ActivePower = 3.0;
                StartupPower = 0.5;
            }
            else // E-Core
            {
                PerformanceFactor = 1.0;
                ActivePower = 1.0;
                StartupPower = 0.1;
            }
        }

        /// <summary>
        /// 프로세서를 유휴 상태일 때 프로세스를 할당합니다.
        /// </summary>
        /// <param name="process">할당할 프로세스</param>
        /// <param name="currentTime">현재 시뮬레이션 시간</param>
        /// <returns>할당 성공 여부</returns>
        public bool AssignProcess(Process process, int currentTime)
        {
            if (!IsIdle)
            {
                Console.WriteLine($"Warning: Processor {Name} is busy. Cannot assign Process {process.Name}.");
                return false;
            }

            CurrentProcess = process;
            _workRemaining = CurrentProcess.RemainingBurstTime > 0 ? CurrentProcess.RemainingBurstTime : CurrentProcess.BurstTime;
            _startTimeCurrentProcess = currentTime;

            Console.WriteLine($"Time {currentTime}: Process {CurrentProcess.Name} (Work: {_workRemaining}) assigned to Processor {Name}.");
            return true;
        }

        /// <summary>
        /// 시뮬레이션 시간 단위를 1 증가시키고, 프로세스를 처리합니다.
        /// </summary>
        /// <param name="currentTime">현재 시뮬레이션 시간 (Tick 시작 시점)</param>
        /// <returns>이번 Tick에서 완료된 프로세스 (없으면 null)</returns>
        public Process Tick(int currentTime)
        {
            bool wasIdle = IsIdle;

            if (IsIdle)
            {
                _wasIdleLastTick = true;
                return null;
            }

            double workDone = PerformanceFactor;
            _workRemaining -= workDone;

            Console.WriteLine($"Time {currentTime}: Processor {Name} working on {CurrentProcess.Name}. Work Done: {workDone}, Remaining: {_workRemaining}");

            if (_workRemaining <= 0)
            {
                Process completedProcess = CurrentProcess;
                int endTime = currentTime + 1;
                Console.WriteLine($"Time {endTime}: Process {completedProcess.Name} completed on Processor {Name}.");

                CompleteProcess(endTime); // Gantt 차트 업데이트 포함

                // _wasIdleLastTick는 CompleteProcess 이후 상태 기준으로 다음 Tick 시작 시 결정됨
                // 여기서는 일단 false로 마킹 (완료 직전까지는 busy였으므로)
                // 실제 다음 틱 시작 시 isIdle 값으로 _wasIdleLastTick 업데이트 필요
                _wasIdleLastTick = false; // 임시 마킹. 메인 루프에서 Tick 시작 시 isIdle로 최종 판단 필요.


                return completedProcess;
            }

            _wasIdleLastTick = false;
            return null;
        }

        /// <summary>
        /// 현재 프로세스 실행을 완료하고 Gantt 차트에 기록합니다.
        /// </summary>
        /// <param name="endTime">프로세스 완료 시간</param>
        private void CompleteProcess(int endTime)
        {
            if (CurrentProcess == null) return;

            // 현재 완료되는 프로세스의 색상을 가져옴
            Brush colorToDraw = CurrentProcess.ProcessColor;

            if (Init.mainApplication != null)
            {
                Init.mainApplication.DrawGanttBar(
                    _startTimeCurrentProcess,
                    endTime,
                    _processorIndexInGantt,
                    CurrentProcess.Name,
                    colorToDraw); // ProcessColor 전달
            }
            // ... (프로세서 상태 초기화) ...
            CurrentProcess.RemainingBurstTime = 0;
            CurrentProcess = null;
            // ...
        }

        public Process PreemptProcess(int currentTime)
        {
            if (IsIdle) return null;

            Process preemptedProcess = CurrentProcess;
            int endTime = currentTime;
            // ... (남은 작업량 계산 및 저장) ...
            preemptedProcess.RemainingBurstTime = Math.Max(0, _workRemaining);

            // 선점되는 프로세스의 색상을 가져옴
            Brush colorToDraw = preemptedProcess.ProcessColor;

            if (Init.mainApplication != null)
            {
                Init.mainApplication.DrawGanttBar(
                    _startTimeCurrentProcess,
                    endTime,
                    _processorIndexInGantt,
                    preemptedProcess.Name,
                    colorToDraw); // ProcessColor 전달
            }
            // ... (프로세서 상태 초기화) ...
            CurrentProcess = null;
            _workRemaining = 0;
            _startTimeCurrentProcess = -1;

            return preemptedProcess;
        }

        /// <summary>
        /// 현재 Tick에서의 전력 소모량을 계산합니다.
        /// </summary>
        /// <param name="currentTime">현재 시간 (로깅용)</param>
        /// <returns>현재 Tick에서 소모된 전력량 (W)</returns>
        public double GetCurrentTickPower(int currentTime)
        {
            // Tick 시작 시점의 IsIdle 상태를 기준으로 _wasIdleLastTick을 먼저 업데이트 하는 것이 더 안전할 수 있습니다.
            // (메인 시뮬레이션 루프에서 Tick 호출 전에 처리)
            // 예: processor._wasIdleLastTick = processor.IsIdle; (루프 시작 시)

            if (IsIdle)
            {
                // 현재 Tick이 유휴 상태임을 기록 (다음 Tick 계산용)
                // 이 업데이트는 실제로는 메인 루프에서 Tick() 호출 *전에* 수행되어야 정확함.
                // 여기서는 GetCurrentTickPower가 Tick과 거의 동시에 호출된다고 가정.
                _wasIdleLastTick = true;
                return 0.0;
            }
            else
            {
                double power = ActivePower;
                if (_wasIdleLastTick) // 직전 Tick에서 유휴였다면 시동 전력 추가
                {
                    power += StartupPower;
                    Console.WriteLine($"Time {currentTime}: Processor {Name} Startup Power ({StartupPower}W) + Active Power ({ActivePower}W) = {power}W");
                }
                else
                {
                    Console.WriteLine($"Time {currentTime}: Processor {Name} Active Power ({ActivePower}W)");
                }

                // 현재 Tick에서 작업했으므로, 다음 Tick 계산 시에는 _wasIdleLastTick는 false여야 함.
                _wasIdleLastTick = false;
                return power;
            }
        }
    }
}
