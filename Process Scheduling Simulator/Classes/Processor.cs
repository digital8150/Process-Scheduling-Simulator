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
        // --- 기존 속성들 ---
        public string Name { get; private set; }
        public CoreType Type { get; private set; }
        public Process CurrentProcess { get; private set; }
        public bool IsIdle => CurrentProcess == null;
        public double PerformanceFactor { get; private set; }
        public double ActivePower { get; private set; }
        public double StartupPower { get; private set; }

        // --- 내부 상태 ---
        private readonly int _processorIndexInGantt;
        private int _startTimeCurrentProcess = -1; // 프로세스 시작 시간
        private double _workRemaining = 0;         // 남은 작업량
        private bool _wasIdleLastTick = true;      // 직전 Tick 유휴 상태 여부

        // _currentGanttBarElement 멤버 변수는 이 방식에서는 사용하지 않으므로 제거합니다.

        // 생성자
        public Processor(string name, CoreType type, int ganttIndex)
        {
            Name = name;
            Type = type;
            _processorIndexInGantt = ganttIndex;
            CurrentProcess = null;
            _workRemaining = 0;
            _startTimeCurrentProcess = -1;
            _wasIdleLastTick = true;

            // 코어 타입별 설정
            if (type == CoreType.P) { PerformanceFactor = 2.0; ActivePower = 3.0; StartupPower = 0.5; }
            else { PerformanceFactor = 1.0; ActivePower = 1.0; StartupPower = 0.1; }
        }

        // 프로세스 할당 (여기서는 그리기 로직 없음)
        public bool AssignProcess(Process process, int currentTime)
        {
            if (!IsIdle) return false;
            CurrentProcess = process;
            _workRemaining = CurrentProcess.RemainingBurstTime > 0 ? CurrentProcess.RemainingBurstTime : CurrentProcess.BurstTime;
            _startTimeCurrentProcess = currentTime;
            _wasIdleLastTick = true; // 직전까지 유휴였음을 기록 (시동 전력 계산용)
            Console.WriteLine($"Time {currentTime}: Process {CurrentProcess.Name} assigned to {Name}.");
            // 첫 Tick 그리는 것은 Tick() 메서드에 맡김
            return true;
        }

        // 매 Tick 처리
        public Process Tick(int currentTime)
        {
            if (IsIdle)
            {
                _wasIdleLastTick = true;
                return null;
            }

            bool wasIdle = _wasIdleLastTick; // 전력 계산 위해 상태 저장
            _wasIdleLastTick = false;      // 현재 Tick은 활성 상태

            // --- 매 Tick마다 간트 바 새로 그리기 ---
            if (CurrentProcess != null && Init.mainApplication != null && _startTimeCurrentProcess != -1)
            {
                // 현재 프로세스의 시작 시간부터 현재 Tick의 종료 시점까지 막대를 그림
                // DrawGanttBar는 새 Border 객체를 생성하여 Canvas에 추가함 (덧씌워짐)
                Init.mainApplication.DrawGanttBar(
                    _startTimeCurrentProcess,    // 이 프로세스가 시작된 시간
                    currentTime + 1,             // 현재 Tick이 끝나는 시간
                    _processorIndexInGantt,      // 이 프로세서의 Gantt 행 인덱스
                    CurrentProcess.Name,         // 현재 프로세스 이름
                    CurrentProcess.ProcessColor  // 현재 프로세스 색상
                );

                // 시간 축도 업데이트 필요
                //Init.mainApplication.UpdateTimeAxis(currentTime + 1);
            }
            // --- 그리기 끝 ---

            // 작업량 처리
            _workRemaining -= PerformanceFactor;
            Console.WriteLine($"Time {currentTime}: {Name} working on {CurrentProcess.Name}. Remaining: {_workRemaining}");

            // 완료 체크
            if (_workRemaining <= 0)
            {
                Process completedProcess = CurrentProcess;
                int endTime = currentTime + 1;
                Console.WriteLine($"Time {endTime}: Process {completedProcess.Name} completed on {Name}.");

                // 완료 처리 (최종 상태 막대 그리기는 CompleteProcess 내부에서 처리)
                CompleteProcess(endTime);

                return completedProcess;
            }

            return null; // 아직 실행 중
        }

        // 프로세스 완료 처리
        private void CompleteProcess(int endTime)
        {
            if (CurrentProcess == null) return;

            // --- 최종 상태의 간트 바 그리기 ---
            // 마지막 Tick에서 그려진 막대 위에 최종 길이의 막대를 한 번 더 그림 (정확한 종료 표현)
            if (Init.mainApplication != null && _startTimeCurrentProcess != -1)
            {
                Init.mainApplication.DrawGanttBar(
                    _startTimeCurrentProcess,
                    endTime, // 최종 종료 시간
                    _processorIndexInGantt,
                    CurrentProcess.Name,
                    CurrentProcess.ProcessColor
                );
            }

            // --- 상태 초기화 ---
            CurrentProcess.RemainingBurstTime = 0;
            CurrentProcess = null;
            _workRemaining = 0;
            _startTimeCurrentProcess = -1;
            // _wasIdleLastTick 는 다음 Tick 시작 시 IsIdle 상태 보고 판단함
        }

        // 프로세스 선점 처리
        public Process PreemptProcess(int currentTime)
        {
            if (IsIdle) return null;

            Process preemptedProcess = CurrentProcess;
            int endTime = currentTime; // 선점 시점

            Console.WriteLine($"Time {currentTime}: Preempting {preemptedProcess.Name} on {Name}.");
            preemptedProcess.RemainingBurstTime = Math.Max(0, _workRemaining);

            // --- 선점된 시점까지의 최종 막대 그리기 ---
            if (Init.mainApplication != null && _startTimeCurrentProcess != -1)
            {
                Init.mainApplication.DrawGanttBar(
                    _startTimeCurrentProcess,
                    endTime, // 선점된 시간까지 그림
                    _processorIndexInGantt,
                    preemptedProcess.Name,
                    preemptedProcess.ProcessColor
                );
            }

            // --- 상태 초기화 ---
            CurrentProcess = null;
            _workRemaining = 0;
            _startTimeCurrentProcess = -1;

            return preemptedProcess;
        }

        // 전력 계산 로직 (변경 없음)
        public double GetCurrentTickPower(int currentTime)
        {
            if (IsIdle) { _wasIdleLastTick = true; return 0.0; }
            else
            {
                double power = ActivePower;
                if (_wasIdleLastTick) { power += StartupPower; }
                _wasIdleLastTick = false;
                return power;
            }
        }
    }
}
