using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Process_Scheduling_Simulator.View;

namespace Process_Scheduling_Simulator.Classes.Scheduler
{
    public class OriginalScheduler : Scheduler
    {
        public OriginalScheduler(List<Process> processes, List<Processor> processors)
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
                // TODO : 여기에 스케줄링 알고리즘을 작성하시면 됩니다.
                // 예시 : 프로세스 도착 처리(레디큐에 삽입). 도착 시간으로 정렬한 프로세스 목록 incomingProcesses에서 currentTime보다 ArrivalTime이 작은 프로세스를 readyQueue에 추가하는 코드
                /*
                while (incomingProcesses.Count > 0 && incomingProcesses.Peek().ArrivalTime <= CurrentTime)
                {
                    var arrivedProcess = incomingProcesses.Dequeue();
                    readyQueue.Add(arrivedProcess);
                }
                */
                //프로세스 할당
                //프로세서 틱 처리 등
                CurrentTime++;
            }
        }
    }
}
