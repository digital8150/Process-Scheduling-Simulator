using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Process_Scheduling_Simulator.Classes
{
    class Process{
        public int arrivalTime;
        public int burstTime;
        public int waitingTime;
        public int turnaroundTime;
        public int normalizedTTime;

        public Process(params int[] args)
        {
            arrivalTime = args[0];
            burstTime = args[1];
        }
    }

    abstract class ProcessSchedulerCore
    {
        protected List<Process> readyQueue;
        protected List<Process> processes;
        protected List<bool> pCores;
        protected List<bool> eCores;


        public ProcessSchedulerCore()
        {
            readyQueue = new List<Process>();
            processes = new List<Process>();
            pCores = new List<bool>();
            eCores = new List<bool>();
        }

        protected int currentTime = 0;

        public void AddProcess(Process[] process)
        {
            foreach (Process p in process)
            {
                processes.Add(p);
            }
        }

        public Boolean IsEmpty()
        {
            return processes.Count == 0;
        }

        public abstract void ScheduleTick();

    }

    class FCFS : ProcessSchedulerCore
    {
        public FCFS()
        {
            readyQueue = new List<Process>();
            processes = new List<Process>();
            pCores = new List<bool>();
            eCores = new List<bool>();
        }

        public override void ScheduleTick()
        {
            
        }
    }
}
