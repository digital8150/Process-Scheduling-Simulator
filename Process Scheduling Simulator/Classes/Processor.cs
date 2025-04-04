using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Process_Scheduling_Simulator.Classes
{
    using System;
    using System.Windows.Media;
    using Process_Scheduling_Simulator.View;


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
        private int _lastActiveTick = -1;      // 직전 Tick 유휴 상태 여부

        // _currentGanttBarElement 멤버 변수는 이 방식에서는 사용하지 않으므로 제거합니다.

        // 생성자
        public Processor(string name, CoreType type, int ganttIndex)
        {
            Name = name;
            Type = type;
            _processorIndexInGantt = ganttIndex;
            ResetState();

            // 코어 타입별 설정
            if (type == CoreType.P) { PerformanceFactor = 2.0; ActivePower = 3.0; StartupPower = 0.5; }
            else { PerformanceFactor = 1.0; ActivePower = 1.0; StartupPower = 0.1; }
        }

        /// <summary>
        /// 프로세서의 내부 상태를 초기화합니다. (시뮬레이션 시작 전 호출 필요)
        /// </summary>
        public void ResetState()
        {
            CurrentProcess = null;
            _workRemaining = 0;
            _startTimeCurrentProcess = -1;
            // _wasIdleLastTick = true; // 필요시 유지 가능하나, _lastActiveTick 기반 로직에선 불필요
            _lastActiveTick = -1;    // 마지막 활성 시간 초기화
        }

        // 프로세스 할당 (여기서는 그리기 로직 없음)
        public bool AssignProcess(Process process, int currentTime)
        {
            if (!IsIdle) return false;
            CurrentProcess = process;
            _workRemaining = CurrentProcess.RemainingBurstTime > 0 ? CurrentProcess.RemainingBurstTime : CurrentProcess.BurstTime;
            _startTimeCurrentProcess = currentTime;
            Console.WriteLine($"Time {currentTime}: Process {CurrentProcess.Name} assigned to {Name}.");
            // 첫 Tick 그리는 것은 Tick() 메서드에 맡김
            return true;
        }

        // 매 Tick 처리
        public Process Tick(int currentTime)
        {
            if (IsIdle)
            {
                return null;
            }

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

        /// <summary>
        /// 현재 시간 Tick에 대한 전력 소모량을 계산합니다. (시동 전력 조건 수정됨)
        /// </summary>
        /// <param name="currentTime">현재 시뮬레이션 시간 (Tick)</param>
        /// <returns>해당 Tick에서 소모된 전력량 (W)</returns>
        public double CalculatePowerForTick(int currentTime) // 메서드 이름 변경 또는 오버로딩 가능
        {
            double powerConsumed = 0.0;
            bool isCurrentlyActive = !IsIdle; // 현재 Tick이 끝난 시점의 상태

            if (isCurrentlyActive)
            {
                // 기본 활성 전력은 소모됨
                powerConsumed = ActivePower;

                // 시동 전력 조건 확인: 현재 활성이면서, 마지막 활성 시간이 (현재시간 - 1) 보다 이전인가?
                // 즉, (현재시간 - 1) Tick 동안은 완전히 유휴 상태였는가?
                if (_lastActiveTick < currentTime - 1)
                {
                    powerConsumed += StartupPower;
                    // Console.WriteLine($"  Debug Power: {Name} at Time {currentTime}. Startup Power Added. LastActive={_lastActiveTick}");
                }
                else
                {
                    // Console.WriteLine($"  Debug Power: {Name} at Time {currentTime}. Active Power Only. LastActive={_lastActiveTick}");
                }

                // 마지막 활성 시간 업데이트
                _lastActiveTick = currentTime;
            }
            else // 현재 유휴 상태
            {
                powerConsumed = 0.0;
                // _lastActiveTick는 변경하지 않음 (마지막으로 활성이었던 시간 유지)
                // Console.WriteLine($"  Debug Power: {Name} at Time {currentTime}. Idle Power = 0. LastActive={_lastActiveTick}");
            }

            return powerConsumed;
        }
    }
}
