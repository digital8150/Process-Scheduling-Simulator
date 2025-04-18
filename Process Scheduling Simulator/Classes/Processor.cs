using System;
using System.Windows.Media;
using Process_Scheduling_Simulator.View;

namespace Process_Scheduling_Simulator.Classes
{
    public enum CoreType { P, E }

    public class Processor
    {
        // --- 기존 속성 ---
        public string Name { get; private set; }
        public CoreType Type { get; private set; }
        public Process CurrentProcess { get; private set; }
        public bool IsIdle => CurrentProcess == null;
        public double PerformanceFactor { get; private set; }
        public double ActivePower { get; private set; }
        public double StartupPower { get; private set; }
        public double TotalConsumedPower { get; private set; }

        //RRScheduler의 BurstTime 직접 관리(추가)
        public int RemainingBurstTime { get; private set; }
        public object RunningProcess { get; internal set; }

        // --- 내부 상태 ---
        private readonly int _processorIndexInGantt;
        private int _startTimeCurrentProcess = -1;
        private double _workRemaining = 0;
        // _lastActiveTick: 이 프로세서가 *활성 상태였던* 마지막 Tick 시간.
        // -1은 아직 활성 상태였던 적이 없음을 의미.
        private int _lastActiveTick = -1;

        public Processor(string name, CoreType type, int ganttIndex)
        {
            Name = name;
            Type = type;
            _processorIndexInGantt = ganttIndex;
            if (type == CoreType.P) { PerformanceFactor = 2.0; ActivePower = 3.0; StartupPower = 0.5; }
            else { PerformanceFactor = 1.0; ActivePower = 1.0; StartupPower = 0.1; }
            ResetState();
        }

        public void ResetState()
        {
            CurrentProcess = null;
            _workRemaining = 0;
            _startTimeCurrentProcess = -1;
            _lastActiveTick = -1;
            TotalConsumedPower = 0.0;
        }

        /// <summary>
        /// 프로세서를 할당하고, 필요한 경우 시동 전력을 추가합니다.
        /// </summary>
        /// <param name="process">할당할 프로세스</param>
        /// <param name="currentTime">현재 시뮬레이션 시간 (할당이 일어나는 시점)</param>
        /// <returns>할당 성공 여부</returns>
        public bool AssignProcess(Process process, int currentTime)
        {
            if (!IsIdle) return false; // 이미 실행 중이면 할당 불가

            // --- 시동 전력 계산 ---
            // 이전에 활성 상태였던 적이 없거나(_lastActiveTick == -1),
            // 마지막 활성 Tick이 현재 시간보다 2 Tick 이상 이전인 경우 (_lastActiveTick < currentTime) 시동 전력 발생
            bool needsStartupPower = (_lastActiveTick == -1 || _lastActiveTick +1 < currentTime);

            if (needsStartupPower)
            {
                TotalConsumedPower += StartupPower;
                // Console.WriteLine($"  Debug Power: {Name} at Time {currentTime}. Startup Power ({StartupPower}W) added during assignment. LastActive={_lastActiveTick}");
            }

            // 프로세스 상태 설정
            CurrentProcess = process;
            _workRemaining = process.RemainingBurstTime > 0 ? process.RemainingBurstTime : process.BurstTime;
            _startTimeCurrentProcess = currentTime;
            // Console.WriteLine($"Time {currentTime}: Process {CurrentProcess.Name} assigned to {Name}. Work: {_workRemaining}. Needs Startup: {needsStartupPower}");

            // 중요: 프로세서를 할당받아 "이번 Tick부터" 활성 상태가 되므로, _lastActiveTick 업데이트는
            // 실제 작업이 일어나는 Tick 메서드 끝에서 해당 Tick 시간으로 업데이트합니다.
            // 여기서 미리 업데이트하면 안 됩니다.

            return true;
        }

        /// <summary>
        /// 현재 Tick에서 프로세스 작업을 수행하고, 활성 전력을 누적합니다.
        /// </summary>
        /// <param name="currentTime">현재 시뮬레이션 시간</param>
        /// <returns>완료된 프로세스 (없으면 null)</returns>
        public Process Tick(int currentTime)
        {
            if (IsIdle)
            {
                // 유휴 상태에서는 아무 작업도, 전력 소모도 없음 (시동 전력은 AssignProcess에서 처리)
                return null;
            }

            // --- 활성 전력 누적 ---
            // 현재 Tick에서 작업을 수행하므로 활성 전력을 소모합니다.
            TotalConsumedPower += ActivePower;
            // Console.WriteLine($"  Debug Power: {Name} at Time {currentTime}. Active Power ({ActivePower}W) added during Tick. Total Now: {TotalConsumedPower:F1}");

            // 간트 차트 그리기 (필요시 유지)
            DrawGanttForCurrentTick(currentTime);

            // 작업량 처리
            _workRemaining -= PerformanceFactor;

            // --- 마지막 활성 Tick 업데이트 ---
            // 현재 Tick에서 활성 상태였으므로 _lastActiveTick를 현재 시간으로 업데이트합니다.
            _lastActiveTick = currentTime;

            // 완료 체크
            if (_workRemaining <= 0)
            {
                Process completedProcess = CurrentProcess;
                int endTime = currentTime + 1; // 완료는 현재 Tick의 끝
                // Console.WriteLine($"Time {endTime}: Process {completedProcess.Name} completed on {Name}.");
                CompleteProcess(endTime); // 내부 상태 초기화 및 최종 간트 그리기
                return completedProcess;
            }

            return null; // 아직 실행 중
        }

        // --- Helper Methods (CompleteProcess, PreemptProcess, DrawGantt) ---

        private void CompleteProcess(int endTime)
        {
            if (CurrentProcess == null) return;
            DrawGanttForCompletion(endTime); // 최종 간트 업데이트
            CurrentProcess = null;
            _workRemaining = 0;
            _startTimeCurrentProcess = -1;
            // _lastActiveTick는 Tick 메서드에서 이미 업데이트되었음
        }

        // 선점 로직은 전력 계산과 직접 관련 없으므로 기존 로직 유지 가능
        // 단, 선점 후 다시 AssignProcess될 때 시동 전력 로직이 올바르게 동작하는지 확인 필요
        public Process PreemptProcess(int currentTime)
        {
            if (IsIdle) return null;

            Process preemptedProcess = CurrentProcess;
            int endTime = currentTime; // 선점은 Tick 시작 시점

            // Console.WriteLine($"Time {currentTime}: Preempting {preemptedProcess.Name} on {Name}. Remaining Work: {_workRemaining}");
            preemptedProcess.RemainingBurstTime = Math.Max(0, _workRemaining);

            DrawGanttForPreemption(endTime); // 선점 시점까지 간트 그리기

            // 상태 초기화
            CurrentProcess = null;
            _workRemaining = 0;
            _startTimeCurrentProcess = -1;
            // _lastActiveTick는 마지막으로 활성이었던 시간(아마도 currentTime - 1)을 유지하고 있을 것임.
            // 다음에 AssignProcess될 때 시동 전력 조건(_lastActiveTick < nextAssignTime)이 평가됨.

            return preemptedProcess;
        }

        // 간트 차트 그리기 헬퍼 메서드들 (기존 로직 사용)
        private void DrawGanttForCurrentTick(int currentTime)
        {
            if (CurrentProcess != null && Init.mainApplication != null && _startTimeCurrentProcess != -1)
            {
                Init.mainApplication.DrawGanttBar(_startTimeCurrentProcess, currentTime + 1, _processorIndexInGantt, CurrentProcess.Name, CurrentProcess.ProcessColor);
            }
        }
        private void DrawGanttForCompletion(int endTime)
        {
            // 완료 시 최종 모습 그리기
            if (CurrentProcess != null && Init.mainApplication != null && _startTimeCurrentProcess != -1)
            {
                Init.mainApplication.DrawGanttBar(_startTimeCurrentProcess, endTime, _processorIndexInGantt, CurrentProcess.Name, CurrentProcess.ProcessColor);
            }
        }
        private void DrawGanttForPreemption(int endTime)
        {
            // 선점 시 최종 모습 그리기
            if (CurrentProcess != null && Init.mainApplication != null && _startTimeCurrentProcess != -1)
            {
                Init.mainApplication.DrawGanttBar(_startTimeCurrentProcess, endTime, _processorIndexInGantt, CurrentProcess.Name, CurrentProcess.ProcessColor);
            }
        }

        // --- RecordPowerForTick 메서드는 이제 사용되지 않으므로 제거 ---
        // public void RecordPowerForTick(int currentTime) { ... }
    }
}