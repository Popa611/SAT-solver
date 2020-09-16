using System;
using System.Collections.Generic;
using System.Threading;

namespace SAT_solver
{
    internal class SolverThread
    {
        private DPLL parallelDPLL { get; }  // Reference to the DPLL class the threads will run in

        private Thread thread { get; set; } // Reference to a thread to be run

        private Queue<CNF> sharedModelQueue { get; }    // Shared queue of models to solve

        private List<SolverThread> threadList;    // List of all run threads (when the result is known, one of the threads aborts the others)

        private IdleThreadsCounter idleThreadsCounter;  // Reference to a count of idle threads

        public static DPLLResultHolder SharedResult = new DPLLResultHolder(false, null);    // The result of the solving threads

        public SolverThread(DPLL parallelDPLL, Queue<CNF> sharedModelQueue, List<SolverThread> threadList, IdleThreadsCounter notWorkingThreadsCounter)
        {
            thread = new Thread(() => ThreadWork());
            this.sharedModelQueue = sharedModelQueue;
            this.threadList = threadList;
            this.idleThreadsCounter = notWorkingThreadsCounter;
            this.parallelDPLL = parallelDPLL;
        }

        public void Start()
        {
            thread.Start();
        }

        public void Abort()
        {
            thread.Abort();
        }

        // Each thread's main function
        private void ThreadWork()
        {
            while (true)
            {
                CNF model;

                lock (sharedModelQueue)
                {
                    while (sharedModelQueue.Count == 0)
                    {
                        Monitor.Wait(sharedModelQueue); // Wait if there's nothing to solve
                    }

                    Interlocked.Decrement(ref idleThreadsCounter.Counter);  // One thread started working - is no longer idle
                    model = sharedModelQueue.Dequeue(); // Else get a CNF model from the shared queue
                }

                DPLLResultHolder solverThreadResult = parallelDPLL.Satisfiable(model);

                if (solverThreadResult.SAT)
                {
                    lock (SharedResult)
                    {
                        SharedResult.SAT = solverThreadResult.SAT;
                        SharedResult.Model = solverThreadResult.Model;

                        foreach (var thread in threadList)
                        {
                            if (thread.thread != Thread.CurrentThread)  // We got the result, abort all other threads
                            {
                                thread.Abort();
                            }
                        }

                        Monitor.Pulse(SharedResult);    // Tell the main thread that the result is ready

                        return;
                    }
                }
                else
                {
                    Interlocked.Increment(ref idleThreadsCounter.Counter);  // The thread is idle for now
                }

                lock (sharedModelQueue)
                {
                    // If all threads are idle and the queue is empty -> we're done and result remains unsat
                    if (Interlocked.Read(ref idleThreadsCounter.Counter) == Environment.ProcessorCount && sharedModelQueue.Count == 0)
                    {
                        lock (SharedResult)
                        {
                            foreach (var thread in threadList)
                            {
                                if (thread.thread != Thread.CurrentThread)
                                {
                                    thread.Abort();
                                }
                            }

                            Monitor.Pulse(SharedResult);

                            return;
                        }
                    }
                }
            }
        }
    }

    internal class IdleThreadsCounter
    {
        public long Counter;    // Counts the number of idle threads

        public IdleThreadsCounter()
        {
            Counter = Environment.ProcessorCount;
        }
    }
}
