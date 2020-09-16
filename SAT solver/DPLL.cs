using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

[assembly: InternalsVisibleTo("SAT solver benchmark")]

namespace SAT_solver
{
    // Class representing the DPLL algorithm with its methods and functions.
    public class DPLL
    {
        private Queue<CNF> sharedModelQueue = new Queue<CNF>();    // Queue of work that should be done by other threads (which is shared among them)

        private bool parallel = false;

        public DPLLResultHolder SatisfiableParallel(CNF cnf)
        {
            parallel = true;

            SolverThread.SharedResult = new DPLLResultHolder(false, null);  // Assume unsat

            sharedModelQueue.Clear();
            sharedModelQueue.Enqueue(cnf);  // Initial formula (model) to solve

            IdleThreadsCounter notWorkingThreadsCounter = new IdleThreadsCounter();

            List<SolverThread> threadList = new List<SolverThread>();

            // A temporal thread to start the other working threads
            Thread starterThread = new Thread(() =>
            {
                for (int i = 0; i < Environment.ProcessorCount; i++)
                {
                    threadList.Add(new SolverThread(this, sharedModelQueue, threadList, notWorkingThreadsCounter));
                }

                lock (sharedModelQueue)    // So threads start working as soon as all threads are started (they will be waiting for this to unlock)
                {
                    foreach (var thread in threadList)
                    {
                        thread.Start();
                    }
                }
            });

            lock (SolverThread.SharedResult)
            {
                starterThread.Start();
                Monitor.Wait(SolverThread.SharedResult);    // Wait for the final result
                parallel = false;
                return SolverThread.SharedResult;
            }
        }

        // Checks if a given CNF formula is SAT or not.
        public DPLLResultHolder Satisfiable(CNF cnf)
        {
            Stack<CNF> stack = new Stack<CNF>();
            stack.Push(cnf);

            while (stack.Count != 0)
            {
                List<Clause> clauses = stack.Peek().Clauses;

                if (AllClausesTrue(clauses))    // Formula is already satisfiable with current partial model
                {
                    return new DPLLResultHolder(true, stack.Peek());
                }

                if (OneClauseFalse(clauses))    // One clause in the partial model is already false -> unsatisfiable
                {
                    stack.Pop();    // Pops an unsat model and continues solving others on the stack

                    continue;
                }

                // Unit propagation
                Variable variable = GetUnitClause(stack.Peek());
                if (variable != null)   // If we have found a unit clause, we can set it so it is true
                {
                    variable.SetValue(stack.Peek(), variable.Sign);
                    continue;
                }

                // Pure literal elimination
                variable = GetPureVariable(stack.Peek());
                if (variable != null)   // If we have found a pure literal, we can set it so it is true in all clauses.
                {
                    variable.SetValue(stack.Peek(), variable.Sign);
                    continue;
                }

                variable = FindUnassigned(stack.Peek());
                if (variable != null)   // If we have found a unassigned variable, we are at the decision (branching) point
                {
                    Branch(stack, variable);

                    continue;
                }
                else
                {
                    stack.Pop();   // Otherwise no unassigned variable exist and we did not return SAT yet -> this model is UNSAT
                }
            }

            return new DPLLResultHolder(false, null);
        }

        // Branch method that creates new CNF formula, sets a variable in the original formula to false
        // and sets the variable in the new formula to true and pushes the new formula to the stack (or enqueues it to shared queue in case of parallelism)
        private void Branch(Stack<CNF> stack, Variable variable)
        {
            if (!parallel)
            {
                CNF branchedCNF = new CNF(stack.Peek()); // Copy the CNF formula
                variable.SetValue(stack.Peek(), false);  // Try setting the variable to false in the original CNF formula
                stack.Push(branchedCNF);
                variable.SetValue(stack.Peek(), true); // Try setting the variable to true in the new CNF formula

                return; // Try solving the new CNF formula
            }
            else
            {
                CNF branchedCNF = new CNF(stack.Peek());
                variable.SetValue(stack.Peek(), false);

                variable.SetValue(branchedCNF, true);

                lock (sharedModelQueue)
                {
                    sharedModelQueue.Enqueue(branchedCNF);  // Enqueue and inform about new work
                    Monitor.Pulse(sharedModelQueue);
                }

                return;
            }
        }

        private bool AllClausesTrue(List<Clause> clauses)
        {
            foreach (var clause in clauses)
            {
                if (!clause.IsTrue())   // Clause does not contain a true variable yet
                {
                    return false;
                }
            }

            return true;
        }

        private bool OneClauseFalse(List<Clause> clauses)
        {
            foreach (var clause in clauses)
            {
                if (clause.IsFalse())   // One clause has all variables assigned and is false
                {
                    return true;
                }
            }

            return false;
        }

        // Pure variable is a variable that occurs only with one polarity in the CNF formula.
        // This function finds such variable.
        private Variable GetPureVariable(CNF cnf)
        {
            bool IsPositive;
            bool IsNegative;

            foreach (var variableList in cnf.VariablesDict.Values)
            {
                if (variableList[0].IsAssigned)  // If a variable is assigned we can skip it
                {
                    continue;
                }

                IsPositive = false;
                IsNegative = false;
                foreach (var clause in cnf.Clauses)
                {
                    if (!clause.IsTrue())   // If a clause is already true, we can skip it
                    {
                        foreach (var clauseVar in clause.Variables)
                        {
                            if (clauseVar.Name == variableList[0].Name)
                            {
                                if (clauseVar.Sign)
                                {
                                    IsPositive = true;
                                }
                                else
                                {
                                    IsNegative = true;
                                }
                            }

                            if (IsPositive && IsNegative)   // We can skip this variable as it is no pure
                            {
                                break;
                            }
                        }

                        if (IsPositive && IsNegative)
                        {
                            break;
                        }
                    }
                }

                if (IsPositive && IsNegative)
                {
                    continue;
                }
                else
                {
                    var ret = new Variable(false, variableList[0].Name);

                    if (IsPositive)
                        ret.Sign = true;

                    return ret;
                }
            }

            return null;
        }

        // Returns a Variable that appears as a unit clause in the CNF.
        // Assigned variables act as if they were removed from the clauses
        private Variable GetUnitClause(CNF cnf)
        {
            foreach (var clause in cnf.Clauses)
            {
                if (!clause.IsTrue())
                {
                    Variable ret = null;
                    int count = 0;
                    foreach (var variable in clause.Variables)
                    {
                        if (!variable.IsAssigned)
                        {
                            ret = variable;
                            count++;
                            if (count > 1)
                            {
                                break;
                            }
                        }
                    }

                    if (count == 1)
                    {
                        return ret;
                    }
                }
            }

            return null;
        }

        // Finds the first unassigned variable and returns it
        private Variable FindUnassigned(CNF cnf)
        {
            foreach (var pair in cnf.VariablesDict)
            {
                if (!pair.Value[0].IsAssigned)
                {
                    return pair.Value[0];
                }
            }

            return null;
        }
    }

    // Holds the result of satisfiability problem.
    public class DPLLResultHolder
    {
        public bool SAT { get; set; }  // True if a CNF is satisfiable. Otherwise false.
        public CNF Model { get; set; } // Holds the model of the CNF if CNF is satisfiable. If SAT is false, it should not be read (set to null preferably)

        public DPLLResultHolder(bool SAT, CNF Model)
        {
            this.SAT = SAT;
            this.Model = Model;
        }

        public override string ToString()
        {
            if (SAT)
            {
                return String.Format("Satisfiable with model:\n{0} ", Model.ToString());
            }
            else
            {
                return "Unsatisfiable.";
            }
        }
    }
}
