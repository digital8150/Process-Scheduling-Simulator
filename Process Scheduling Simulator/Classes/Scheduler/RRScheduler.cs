using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Process_Scheduling_Simulator.View; // Assuming Init.mainApplication is accessible
using System.Collections.Concurrent; // Using ConcurrentDictionary for potential thread safety if needed, though current loop is sequential. Stick to Dictionary if single-threaded access is guaranteed.

namespace Process_Scheduling_Simulator.Classes.Scheduler
{
    public class RRScheduler : Scheduler
    {
        private readonly int _quantum;
        // Tracks the time slice used by the process currently on a specific processor
        private Dictionary<Processor, int> _currentQuantumUsage;
        // Tracks when a process started waiting (entered ready queue or was preempted)
        private Dictionary<Process, int> _processWaitStartTime;

        public RRScheduler(List<Process> processes, List<Processor> processors, int quantum)
            : base(processes, processors)
        {
            if (quantum <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantum), "Quantum must be positive.");
            }
            _quantum = quantum;
            _currentQuantumUsage = new Dictionary<Processor, int>();
            _processWaitStartTime = new Dictionary<Process, int>();
        }

        public override void Reset()
        {
            base.Reset(); // Resets time, processes, processors (including power), completed list
            _currentQuantumUsage.Clear();
            _processWaitStartTime.Clear();
            // Ensure initial WaitingTime is 0 for all processes handled by the base class reset.
            Console.WriteLine("RRScheduler specific state reset.");
        }

        public async override Task Schedule()
        {
            Reset(); // Start with a clean state

            // Prepare initial process list sorted by arrival time
            var incomingProcesses = new Queue<Process>(Processes.OrderBy(p => p.ArrivalTime));
            var readyQueue = new Queue<Process>();

            Console.WriteLine($"--- RR Simulation Start (Quantum={_quantum}) ---");

            // Main simulation loop
            while (CompletedProcesses.Count < Processes.Count)
            {
                // --- Visualization Delay ---
                int delay = 100; // Default delay
                if (Init.mainApplication != null && int.TryParse(Init.mainApplication.VisDelayTextBox.Text, out int parsedDelay))
                {
                    delay = parsedDelay;
                }
                if (delay > 0) // Only delay if needed
                {
                    await Task.Delay(delay);
                }

                // --- 1. Process Arrival ---
                // Add processes arriving at the current time to the ready queue
                while (incomingProcesses.Count > 0 && incomingProcesses.Peek().ArrivalTime <= CurrentTime)
                {
                    var arrivedProcess = incomingProcesses.Dequeue();
                    Console.WriteLine($"Time {CurrentTime}: Process {arrivedProcess.Name} arrived. Adding to Ready Queue.");
                    readyQueue.Enqueue(arrivedProcess);
                    // Mark the time this process starts waiting
                    if (!_processWaitStartTime.ContainsKey(arrivedProcess))
                    {
                        _processWaitStartTime[arrivedProcess] = CurrentTime;
                    }
                }

                // --- 2. Check for Quantum Expiry & Preempt ---
                // Iterate through processors that are *currently running* a process
                // Create a temporary list to avoid modification issues during iteration
                var processorsToCheck = Processors.Where(p => !p.IsIdle).ToList();
                foreach (var processor in processorsToCheck)
                {
                    // Check if the processor is actually in the dictionary (it should be if !IsIdle)
                    if (_currentQuantumUsage.TryGetValue(processor, out int usage) && usage >= _quantum)
                    {
                        Console.WriteLine($"Time {CurrentTime}: Quantum expired for {processor.CurrentProcess.Name} on {processor.Name}. Preempting.");
                        Process preemptedProcess = processor.PreemptProcess(CurrentTime);

                        if (preemptedProcess != null)
                        {
                            // Add the preempted process back to the ready queue
                            readyQueue.Enqueue(preemptedProcess);
                            // Mark the time it starts waiting again
                            if (!_processWaitStartTime.ContainsKey(preemptedProcess))
                            {
                                _processWaitStartTime[preemptedProcess] = CurrentTime;
                            }
                            else
                            {
                                // If it was already waiting (e.g., preempted immediately after arriving?), update start time.
                                // This case is less likely with proper arrival/assignment logic but handles edge cases.
                                _processWaitStartTime[preemptedProcess] = CurrentTime;
                            }
                        }
                        // Reset quantum usage for this processor as it's now idle (or will be assigned next)
                        _currentQuantumUsage.Remove(processor);
                    }
                }


                // --- 3. Assign Processes to Idle Processors ---
                // Iterate through processors that are *currently idle*
                foreach (var processor in Processors.Where(p => p.IsIdle))
                {
                    if (readyQueue.Count > 0)
                    {
                        var processToAssign = readyQueue.Dequeue();

                        // Accumulate waiting time
                        if (_processWaitStartTime.TryGetValue(processToAssign, out int waitStartTime))
                        {
                            // Ensure WaitingTime doesn't decrease if preempted/requeued multiple times in the same tick somehow
                            int timeSpentWaiting = CurrentTime - waitStartTime;
                            if (timeSpentWaiting > 0)
                            {
                                processToAssign.WaitingTime += timeSpentWaiting;
                                //Console.WriteLine($"Debug: Process {processToAssign.Name} waited {timeSpentWaiting} ticks (Total WT: {processToAssign.WaitingTime}). Assigned at {CurrentTime}.");
                            }
                            _processWaitStartTime.Remove(processToAssign); // Remove from waiting map
                        }
                        else
                        {
                            // This shouldn't happen if logic is correct, but log if it does
                            Console.WriteLine($"Warning: Process {processToAssign.Name} assigned without a wait start time at {CurrentTime}!");
                        }


                        Console.WriteLine($"Time {CurrentTime}: Assigning {processToAssign.Name} (Remaining: {(processToAssign.RemainingBurstTime > 0 ? processToAssign.RemainingBurstTime : processToAssign.BurstTime):F1}) to {processor.Name} ({processor.Type}-Core).");

                        if (processor.AssignProcess(processToAssign, CurrentTime))
                        {
                            // Reset quantum usage timer for the newly assigned process
                            _currentQuantumUsage[processor] = 0;
                        }
                        else
                        {
                            // This should ideally not happen if we only assign to idle processors
                            Console.WriteLine($"Error: Failed to assign {processToAssign.Name} to supposedly idle processor {processor.Name} at time {CurrentTime}. Re-queuing.");
                            // Put it back at the front of the queue for the next cycle
                            var tempQueue = new Queue<Process>();
                            tempQueue.Enqueue(processToAssign);
                            while (readyQueue.Count > 0) tempQueue.Enqueue(readyQueue.Dequeue());
                            readyQueue = tempQueue;
                            // Put back its wait start time marker
                            if (!_processWaitStartTime.ContainsKey(processToAssign))
                            {
                                _processWaitStartTime[processToAssign] = CurrentTime;
                            }
                        }
                    }
                }

                // --- 4. Execute a Tick on All Processors ---
                // Create a temporary list as Tick might modify the processor state (make it idle)
                var activeProcessorsBeforeTick = Processors.Where(p => !p.IsIdle).ToList();
                foreach (var processor in Processors) // Iterate through ALL processors
                {
                    // Store process name before tick in case it completes
                    string runningProcessName = processor.CurrentProcess?.Name ?? "Idle";
                    Process completedProcess = processor.Tick(CurrentTime); // Processor handles work reduction, power, Gantt

                    // --- 5. Handle Process Completion ---
                    if (completedProcess != null)
                    {
                        int completionTime = CurrentTime + 1; // Process completes at the end of the current tick
                        Console.WriteLine($"Time {completionTime}: Process {completedProcess.Name} COMPLETED on {processor.Name}.");

                        // Calculate final metrics
                        completedProcess.TurnaroundTime = completionTime - completedProcess.ArrivalTime;
                        // Waiting time was accumulated. Ensure BurstTime is positive for NTT.
                        completedProcess.NormalizedTTime = (completedProcess.BurstTime > 0)
                            ? (double)completedProcess.TurnaroundTime / completedProcess.BurstTime
                            : 0;

                        // We don't need to calculate actual execution time here for WT,
                        // as WT was accumulated based on time *not* running.
                        // The base Process class stores the original BurstTime.

                        CompletedProcesses.Add(completedProcess);
                        _currentQuantumUsage.Remove(processor); // Remove from quantum tracking
                        _processWaitStartTime.Remove(completedProcess); // Ensure removed if somehow still present

                        Console.WriteLine($"  Metrics for {completedProcess.Name}: TT={completedProcess.TurnaroundTime}, WT={completedProcess.WaitingTime}, NTT={completedProcess.NormalizedTTime:F2}");
                    }
                    // --- 6. Update Quantum Usage for Ongoing Processes ---
                    // Check if the processor *still* has a process after the tick (i.e., it wasn't completed)
                    else if (!processor.IsIdle)
                    {
                        // Ensure the processor is in the dictionary before incrementing
                        if (_currentQuantumUsage.ContainsKey(processor))
                        {
                            _currentQuantumUsage[processor]++;
                            // Console.WriteLine($"Debug: Time {CurrentTime}, {processor.Name}({processor.CurrentProcess.Name}), Quantum Usage: {_currentQuantumUsage[processor]}");
                        }
                        else
                        {
                            // This might happen if a process was assigned and immediately ticked in the same cycle,
                            // which is correct according to the AssignProcess/Tick sequence. Initialize it.
                            _currentQuantumUsage[processor] = 1;
                            // Console.WriteLine($"Debug: Time {CurrentTime}, {processor.Name}({processor.CurrentProcess.Name}), Quantum Usage Initialized: 1");
                        }
                    }
                    else // Processor became idle this tick (either completed or was already idle)
                    {
                        // If it was running before the tick but is now idle (and not completed), it must have been preempted earlier.
                        // If it was idle before the tick, do nothing.
                        // If it completed, the completion block already handled cleanup.
                        if (activeProcessorsBeforeTick.Contains(processor) && completedProcess == null)
                        {
                            // This implies preemption happened before the Tick call in the same time step.
                            // The preemption logic should have already handled _currentQuantumUsage removal.
                            // Console.WriteLine($"Debug: Processor {processor.Name} became idle after Tick at {CurrentTime}, likely due to earlier preemption.");
                        }
                        _currentQuantumUsage.Remove(processor); // Ensure cleanup if it became idle for any reason
                    }
                }


                // --- 7. Advance Time ---
                CurrentTime++;

                // --- Loop Termination Conditions ---
                if (CurrentTime > 20000) // Safety break for runaway simulations
                {
                    Console.WriteLine("Warning: Simulation exceeded maximum time limit (20000 ticks). Stopping.");
                    break;
                }

                // Check if simulation should end: all original processes are completed.
                if (CompletedProcesses.Count >= Processes.Count)
                {
                    Console.WriteLine($"--- All {Processes.Count} processes completed at Time {CurrentTime}. ---");
                    break; // Exit loop cleanly
                }

                // Optional: Check for deadlock/idle state (no incoming, no ready, all processors idle)
                // This check should ideally be covered by CompletedProcesses.Count == Processes.Count
                if (incomingProcesses.Count == 0 && readyQueue.Count == 0 && Processors.All(p => p.IsIdle) && CompletedProcesses.Count < Processes.Count)
                {
                    Console.WriteLine($"Warning: Simulation stalled at Time {CurrentTime}. IncomingQ={incomingProcesses.Count}, ReadyQ={readyQueue.Count}, IdleProcs={Processors.Count(p => p.IsIdle)}, Completed={CompletedProcesses.Count}/{Processes.Count}. Stopping.");
                    break; // Avoid infinite loop if something unexpected happens
                }

            } // End of main simulation loop

            Console.WriteLine("--- RR Simulation End ---");
            CalculateAverageMetrics(); // Calculate and print final stats (including power from Processors)
        }
    }
}