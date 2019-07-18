using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SAT_solver
{
    // Class representing the CNF formula
    class CNF
    {
        public List<Clause> Clauses { get; private set; } // List of all clauses (each connected with conjuction)
        private int NumberOfVariables { get; set; } // Number of unique variables
        private int NumberOfClauses { get; set; }   // Number of clauses

        public CNF()
        {
            Clauses = new List<Clause>();
        }

        public CNF(List<Clause> Clauses)
        {
            this.Clauses = Clauses;

            NumberOfClauses = Clauses.Count;
        }

        // Deep copy constructor
        // Constructs new CNF type with copied VALUE of clauses, variables, ...
        public CNF(CNF cnf)
        {
            List<Clause> ClauseList = new List<Clause>();

            foreach (var clause in cnf.Clauses)
            {
                List<Variable> VarList = new List<Variable>();

                foreach (var variable in clause.Variables)
                {
                    VarList.Add(new Variable(variable.Sign, variable.Value, variable.Assigned, variable.Name));
                }

                ClauseList.Add(new Clause(VarList));
            }

            Clauses = ClauseList;
            NumberOfVariables = cnf.NumberOfVariables;
            NumberOfClauses = cnf.NumberOfClauses;
        }

        // Reads and parses the CNF formula into clauses list from a TextReader input.
        public void ReadFormula(TextReader reader)
        {
            Variable variable;
            List<Variable> variables = new List<Variable>();
            Clause clause;
            bool sign;  // sign and name for variables parsing
            string name;

            try
            {
                // Read, check and parse first line
                string[] tokens = reader.ReadLine().Split(' ');

                if (tokens[0] != "cnf") // Must begin with "cnf"
                {
                    throw new FormatException();
                }

                NumberOfVariables = Convert.ToInt32(tokens[1]); // Followed by two integers (any exception is caught)
                NumberOfClauses = Convert.ToInt32(tokens[2]);

                // Read next lines (clauses)
                for (int i = 0; i < NumberOfClauses; i++)
                {
                    tokens = reader.ReadLine().Split(' ');
                    foreach (var token in tokens)   // For each variable
                    {
                        if (token[0] == '-')    // Negation of a variable
                        {
                            sign = false;
                            name = token.Remove(0, 1);    // Remove the minus sign and use the number as a name
                        }
                        else
                        {
                            sign = true;
                            name = token;
                        }

                        variable = new Variable(sign, name);
                        variables.Add(variable);
                    }

                    clause = new Clause(variables);
                    variables.Clear();
                    Clauses.Add(clause);
                }
            }
            catch (Exception ex)    // Most likely the formatting was wrong.
            {
                if (ex is InvalidCastException || ex is IndexOutOfRangeException || ex is FormatException)
                {
                    throw new FormatException();
                }
            }
        }

        // Returns a list with all variables in the CNF formula
        public List<Variable> GetVariables()
        {
            List<Variable> res = new List<Variable>();

            foreach (var clause in Clauses)
            {
                foreach (var var in clause.Variables)
                {
                    res.Add(var);
                }
            }

            return res;
        }

        // Gets list of unique variables
        public List<Variable> GetUniqueVariables()
        {
            List<Variable> uniqueVariables = new List<Variable>();
            bool added;

            foreach (var clause in Clauses)
            {
                foreach (var var in clause.Variables)
                {
                    added = false;
                    foreach (var uniqueVar in uniqueVariables)
                    {
                        if (uniqueVar.Name == var.Name)  // If var is already in the uniqueVariables list, do not add it
                        {
                            added = true;
                            break;
                        }
                    }

                    if (!added)
                    {
                        uniqueVariables.Add(var);
                    }
                }
            }

            return uniqueVariables;
        }

        // Converts the model of CNF formula to string, e.g.:
        // 1: true
        // 2: false
        // ...
        public override string ToString()
        {
            if (Clauses == null)    // If it was not even initialized by 'new', no model can exist
            {
                return null;
            }

            StringBuilder stringBuilder = new StringBuilder();
            List<Variable> variables = GetUniqueVariables();

            foreach (var variable in variables)
            {
                stringBuilder.Append(String.Format("{0}: {1}\n", variable.Name, variable.Value));
            }

            return stringBuilder.ToString();
        }
    }

    // Class representing one clause in the CNF formula
    class Clause
    {
        public List<Variable> Variables { get; private set; } // List of all variables in the clause

        public Clause(List<Variable> variables)
        {
            List<Variable> vars = new List<Variable>();

            foreach (var variable in variables)
            {
                vars.Add(new Variable(variable.Sign, variable.Value, variable.Assigned, variable.Name));
            }

            this.Variables = vars;
        }

        // Checks whether there is atleast one variable that is set to true
        // Not all variables have to be assigned yet
        public bool IsTrue()
        {
            foreach (var var in Variables)
            {
                if (var.Assigned && var.GetFinalValue())
                {
                    return true;
                }
            }

            return false;
        }

        // Checks whether all variables are assigned and whether they are all false
        public bool IsFalse()
        {
            foreach (var var in Variables)
            {
                if (!var.Assigned)  // All variables have to be assigned
                {
                    return false;
                }
            }

            foreach (var var in Variables)
            {
                if (var.GetFinalValue())
                {
                    return false;
                }
            }

            return true;
        }
    }

    // Class representing a single variable in a clause of a CNF formula
    class Variable
    {
        public bool Sign { get; set; }  // Either positive or negative literal
        public bool Value { get; set; } // Assigned value of the variable
        public bool Assigned { get; set; }  // Indicates whether this variable has been already assigned a value
        public string Name { get; private set; }  // Unique identifier

        public Variable (bool Sign, bool Value, bool Assigned, string Name)
        {
            this.Sign = Sign;
            this.Value = Value;
            this.Assigned = Assigned;
            this.Name = Name;
        }

        public Variable (bool Sign, string Name)
        {
            this.Sign = Sign;
            Value = false;
            Assigned = false;
            this.Name = Name;
        }

        // Returns the final value of the variable ( == takes sign and value into account)
        public bool GetFinalValue()
        {
            if (Sign)
            {
                return Value;
            }
            else
            {
                return !Value;
            }
        }

        // Sets the value of the variable in the CNF formula.
        public void SetValue(CNF cnf, bool Value)
        {
            foreach (var variable in cnf.GetVariables())
            {
                if (variable.Name == this.Name) // All variables in the formula with the same name get assigned
                {
                    variable.Value = Value;
                    variable.Assigned = true;
                }
            }
        }

        // Sets the Assigned property for the variable in the whole CNF formula to false.
        public void Unassign(CNF cnf)
        {
            foreach (var variable in cnf.GetVariables())
            {
                if (variable.Name == this.Name)
                {
                    variable.Assigned = false;
                }
            }
        }
    }

    // Class representing the DPLL algorithm with its methods and functions.
    class DPLL
    {
        private static Queue<CNF> sharedModelQueue = new Queue<CNF>();    // Queue of work that should be done by other threads (which is shared among them)

        private static bool parallel = false;

        public DPLLResultHolder SatisfiableParallel(CNF cnf)
        {
            parallel = true;

            sharedModelQueue.Clear();
            sharedModelQueue.Enqueue(cnf);  // Initial formula (model) to solve

            IdleThreadsCounter notWorkingThreadsCounter = new IdleThreadsCounter();

            List<SolverThread> threadList = new List<SolverThread>();

            // A temporal thread to start the other working threads
            Thread starterThread = new Thread(() =>
            {
                for (int i = 0; i < Environment.ProcessorCount; i++)
                {
                    threadList.Add(new SolverThread(sharedModelQueue, threadList, notWorkingThreadsCounter));
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

        public DPLLResultHolder Satisfiable(CNF cnf)
        {
            List<Clause> clauses = cnf.Clauses;

            if (AllClausesTrue(clauses))    // Formula is already satisfiable with current partial model
            {
                return new DPLLResultHolder(true, cnf);
            }

            if (OneClauseFalse(clauses))    // One clause in the partial model is already false -> unsatisfiable
            {
                return new DPLLResultHolder(false, null);
            }

            // Unit propagation
            Variable variable = GetUnitClause(cnf);
            if (variable != null)   // If we have found a unit clause, we can set it so it is true
            {
                variable.SetValue(cnf, variable.Sign);
                return Satisfiable(cnf);
            }

            // Pure literal elimination
            variable = GetPureVariable(cnf);
            if (variable != null)   // If we have found a pure literal, we can set it so it is true in all clauses.
            {
                variable.SetValue(cnf, variable.Sign);
                return Satisfiable(cnf);
            }

            variable = FindUnassigned(cnf);
            if (variable != null)   // If we have found a unassigned variable, we are at the decision (branching) point
            {
                return Branch(cnf, variable);
            }
            else
            {
                return new DPLLResultHolder(false, null);   // Otherwise no unassigned variable exist and we did not return SAT yet -> UNSAT
            }
        }

        // Branch method that tries to assign a variable and solve the CNF recursively
        // If the assignment failed, try the other assignment value.
        private DPLLResultHolder Branch (CNF cnf, Variable variable)
        {
            if (!parallel)
            {
                CNF BranchCNF = new CNF(cnf);   // Copy the CNF formula
                variable.SetValue(cnf, true);   // Try setting the variable to true in the original CNF formula
                DPLLResultHolder result = Satisfiable(cnf); // Try solving the original CNF formula

                if (result.SAT) // If SAT, we're done
                {
                    return result;
                }
                else    // Otherwise try setting the variable to false and try solving the copy of the original formula (original might have changed).
                {
                    variable.SetValue(BranchCNF, false);
                    return Satisfiable(BranchCNF);
                }
            }
            else
            {
                // NAIVE THREADING
                /*CNF BranchCNF = new CNF(cnf);
                variable.SetValue(cnf, true);

                DPLLResultHolder otherThreadResult = null;
                Thread otherThread = new Thread(() =>
                {
                    variable.SetValue(BranchCNF, false);
                    otherThreadResult = Satisfiable(BranchCNF);
                });
                otherThread.Start();

                DPLLResultHolder result = Satisfiable(cnf);

                if (result.SAT)
                {
                    return result;
                }

                otherThread.Join();
                
                if (otherThreadResult.SAT)
                {
                    return otherThreadResult;
                }

                return new DPLLResultHolder(false, null);*/

                // BETTER THREADING
                CNF BranchCNF = new CNF(cnf);
                variable.SetValue(cnf, true);
                variable.SetValue(BranchCNF, false);

                lock (sharedModelQueue)
                {
                    sharedModelQueue.Enqueue(BranchCNF);
                    Monitor.Pulse(sharedModelQueue);
                }

                DPLLResultHolder result = Satisfiable(cnf);

                if (result.SAT)
                {
                    return result;
                }

                return new DPLLResultHolder(false, null);
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
            /*bool IsPositive;
            bool IsNegative;

            foreach (var variable in cnf.GetUniqueVariables())
            {
                if (variable.Assigned)  // If a variable is assigned we can skip it
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
                            if (clauseVar.Name == variable.Name)
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
                    //return variable;
                    cnf.Clauses.RemoveAll(x =>
                    {
                        foreach (var v in x.Variables)
                        {
                            if (v.Name == variable.Name)
                            {
                                return true;
                            }
                        }

                        return false;
                    });

                    cnf.Clauses.Add(new Clause(new List<Variable>{ new Variable(true, true, true, variable.Name)}));
                }
            }*/

            foreach (var variable in cnf.GetUniqueVariables())
            {
                bool occured_positively = false;
                bool occured_negatively = false;

                foreach (var clause in cnf.Clauses)
                {
                    if (!clause.IsTrue())
                    {
                        foreach (var clauseVar in clause.Variables)
                        {
                            if (clauseVar.Name == variable.Name)
                            {
                                if (clauseVar.Assigned && !clauseVar.Sign)
                                {
                                    occured_negatively = true;
                                }
                                else if (clauseVar.Assigned && clauseVar.Sign)
                                {
                                    occured_positively = true;
                                }
                            }

                            if (occured_positively && occured_negatively)
                            {
                                break;
                            }
                        }
                    }
                }

                if (occured_positively && occured_negatively)
                {
                    return variable;
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
                    int count = clause.Variables.Count;
                    foreach (var variable in clause.Variables)
                    {
                        if (variable.Assigned)
                        {
                            count--;
                        }
                        else
                        {
                            ret = variable;
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
            foreach (var variable in cnf.GetVariables())
            {
                if (!variable.Assigned)
                {
                    return variable;
                }
            }

            return null;
        }
    }

    // Holds the result of satisfiability problem.
    class DPLLResultHolder
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

    class SolverThread
    {
        private Thread thread { get; set; } // Reference to a thread to be run

        private Queue<CNF> sharedModelQueue { get; }    // Shared queue of models to solve

        private List<SolverThread> threadList;    // List of all run threads (when the result is known, one of the threads aborts the others)

        private IdleThreadsCounter idleThreadsCounter;  // Reference to a count of idle threads

        public static DPLLResultHolder SharedResult = new DPLLResultHolder(false, null);    // The result of the solving threads

        public SolverThread(Queue<CNF> sharedModelQueue, List<SolverThread> threadList, IdleThreadsCounter notWorkingThreadsCounter)
        {
            thread = new Thread(() => ThreadWork());
            this.sharedModelQueue = sharedModelQueue;
            this.threadList = threadList;
            this.idleThreadsCounter = notWorkingThreadsCounter;
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
                DPLL parallelDPLL = new DPLL();
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

    class IdleThreadsCounter
    {
        public long Counter;    // Counts the number of idle threads

        public IdleThreadsCounter()
        {
            Counter = Environment.ProcessorCount;
        }
    }

    class IndependentSetProblem
    {
        public Graph Graph = new Graph();

        public int K { get; set; }  // Does an independent set of size atleast K in a graph exist?

        public void ReadInput(TextReader reader)
        {
            var line = reader.ReadLine();
            K = Convert.ToInt32(line);
            Graph.ReadGraph(reader);
        }

        // Graph vertices must be named 0, 1, 2, ... (starting from zero!)
        public CNF ConvertToCNF()
        {
            List<Clause> clauses = new List<Clause>();

            foreach (var vertex in Graph.Vertices)
            {
                foreach (var neighbour in vertex.Neighbours)    // For each edge ij add a clause (!v_i || !v_j)  (which corresponds to independency)
                {
                    clauses.Add(new Clause(new List<Variable> { new Variable(false, vertex.Id), new Variable(false, neighbour.Id) }));
                }
            }
            
            // Create clauses (!X_ij || !X_kj) for i != k
            for (int i = 0; i < K; i++)
            {
                for (int j = 0; j < Graph.Vertices.Count; j++)
                {
                    Variable X_ij = new Variable(false, "X"+i.ToString() + j.ToString());

                    for (int k = 0; k < K; k++)
                    {
                        if (i == k)
                        {
                            continue;
                        }

                        Variable X_kj = new Variable(false, "X"+k.ToString() + j.ToString());
                        clauses.Add(new Clause(new List<Variable> { X_ij, X_kj }));
                    }
                }
            }

            // Create clauses (!X_ij || !X_ik) for j != k
            for (int i = 0; i < K; i++)
            {
                for (int j = 0; j < Graph.Vertices.Count; j++)
                {
                    Variable X_ij = new Variable(false, "X"+i.ToString() + j.ToString());

                    for (int k = 0; k < Graph.Vertices.Count; k++)
                    {
                        if (j == k)
                        {
                            continue;
                        }

                        Variable X_ik = new Variable(false, "X"+i.ToString() + k.ToString());
                        clauses.Add(new Clause(new List<Variable> { X_ij, X_ik }));
                    }
                }
            }

            // Create clauses (X_i0 || X_i1 || ... || X_in)
            for (int i = 0; i < K; i++)
            {
                List<Variable> variables = new List<Variable>();

                for (int j = 0; j < Graph.Vertices.Count; j++)
                {
                    variables.Add(new Variable(true, "X"+i.ToString() + j.ToString()));
                }

                if (variables.Count != 0)
                {
                    clauses.Add(new Clause(variables));
                }
            }

            // Create clauses (!X_ij || vertex_j)
            for (int i = 0; i < K; i++)
            {
                for (int j = 0; j < Graph.Vertices.Count; j++)
                {
                    clauses.Add(new Clause(new List<Variable> { new Variable(false, "X"+i.ToString() + j.ToString()), new Variable(true, j.ToString()) }));
                }
            }

            return new CNF(clauses);
        }
    }

    class Graph
    {
        public List<Vertex> Vertices;

        public Graph()
        {
            Vertices = new List<Vertex>();
        }

        public void ReadGraph(TextReader reader)
        {
            var line = reader.ReadLine();
            var tokens = line.Split(' ');

            foreach (var token in tokens)
            {
                Vertices.Add(new Vertex(token));
            }

            if (Vertices.Count == 0)    // Empty graphs not supported
                throw new FormatException();

            while(true)
            {
                line = reader.ReadLine();
                
                if (line == "")
                {
                    break;
                }

                tokens = line.Split(' ');

                var vertex1 = GetVertexById(tokens[0]);
                var vertex2 = GetVertexById(tokens[1]);

                vertex1.AddNeighbour(vertex2);
                vertex2.AddNeighbour(vertex1);
            }
        }

        private Vertex GetVertexById(string Id)
        {
            foreach (var vertex in Vertices)
            {
                if (vertex.Id == Id)
                {
                    return vertex;
                }
            }

            return null;
        }
    }

    class Vertex
    {
        public string Id { get; set; }
        public List<Vertex> Neighbours;

        public Vertex(string Id)
        {
            this.Id = Id;
            Neighbours = new List<Vertex>();
        }

        public void AddNeighbour(Vertex Vertex)
        {
            Neighbours.Add(Vertex);
        }
    }

    class Program
    {
        static void PrintUsage()
        {
            Console.WriteLine("-----------------------------------------HELP---------------------------------------");
            Console.WriteLine("First line of input is \"cnf number_of_variables number_of_clauses\"");
            Console.WriteLine("Then number_of_clauses lines follow which consist of integers separated by space.");
            Console.WriteLine("Each integer acts as a variable. Negative integer is negation of the variable.");
            Console.WriteLine("Each line is one clause.");
            Console.WriteLine();
            Console.WriteLine("Input format example: ");
            Console.WriteLine("cnf 3 2");
            Console.WriteLine("-1 2 3");
            Console.WriteLine("2 -3");
            Console.WriteLine("------------------------------------------------------------------------------------");
        }

        static void Main(string[] args)
        {
            PrintUsage();
            CNF cnf = new CNF();
            IndependentSetProblem independentSetProblem = new IndependentSetProblem();

            while (true)
            {
                try
                {
                    cnf.ReadFormula(Console.In);
                    //independentSetProblem.ReadInput(Console.In);
                    break;
                }
                catch (FormatException)
                {
                    Console.WriteLine();
                    Console.WriteLine("Invalid input format!");
                    Console.WriteLine();
                    PrintUsage();
                }
            }

            /*var independentSetProblemCNF = independentSetProblem.ConvertToCNF();

            DPLL independentSetProblemDPLL = new DPLL();
            DPLLResultHolder independentSetProblemDPLLResult = independentSetProblemDPLL.Satisfiable(independentSetProblemCNF);
            Console.WriteLine();
            Console.WriteLine(independentSetProblemDPLLResult);*/

            CNF cnfCopy = new CNF(cnf);

            Stopwatch stopwatch = new Stopwatch();

            DPLL sequentialDPLL = new DPLL();
            stopwatch.Start();
            DPLLResultHolder sequentialResult = sequentialDPLL.Satisfiable(cnf);
            stopwatch.Stop();
            Console.WriteLine();
            Console.WriteLine(sequentialResult);

            var seq = stopwatch.Elapsed;
            stopwatch.Reset();

            DPLL parallelDPLL = new DPLL();
            stopwatch.Start();
            DPLLResultHolder parallelResult = parallelDPLL.SatisfiableParallel(cnfCopy);
            stopwatch.Stop();
            Console.WriteLine();
            Console.WriteLine(parallelResult);

            Console.WriteLine("Sequential: {0}", seq);
            Console.WriteLine("Parallel: {0}", stopwatch.Elapsed);

            
        }
    }
}
