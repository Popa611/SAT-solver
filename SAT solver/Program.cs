using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("SAT solver benchmark")]

namespace SAT_solver
{
    // Class representing the CNF formula
    public class CNF
    {
        internal List<Clause> Clauses { get; private set; } // List of all clauses (each connected with conjuction)
        internal Dictionary<string, List<Variable>> VariablesDict { get; private set; } // Key is a variable name, value is a list of variables with the same name
                 // All variables in the list must have the same IsAssigned, Value, Name properties if a variable with the name is assigned!

        private int NumberOfVariables { get; set; } // Number of unique variables
        private int NumberOfClauses { get; set; }   // Number of clauses

        public CNF()
        {
            Clauses = new List<Clause>();
            VariablesDict = new Dictionary<string, List<Variable>>();
        }

        internal CNF(List<Clause> Clauses)
        {
            this.Clauses = Clauses;
            VariablesDict = new Dictionary<string, List<Variable>>();
            FillVariablesDict(Clauses);
            NumberOfClauses = Clauses.Count;
            NumberOfVariables = VariablesDict.Count;
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
                    VarList.Add(new Variable(variable.Sign, variable.Value, variable.IsAssigned, variable.Name));
                }

                ClauseList.Add(new Clause(VarList));
            }

            Clauses = ClauseList;
            VariablesDict = new Dictionary<string, List<Variable>>();
            FillVariablesDict(Clauses);
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
                string[] tokens;

                while (true)
                {
                    // Skip comment lines (starting with 'c ')
                    tokens = reader.ReadLine().Split(' ', '\t');

                    if (tokens[0] != "c" && tokens[0] != "")
                    {
                        break;
                    }
                }

                if (tokens[0] != "p" && tokens[1] != "cnf") // Must begin with "p cnf"
                {
                    throw new FormatException();
                }

                NumberOfVariables = Convert.ToInt32(tokens[2]); // Followed by two integers (any exception is caught)
                NumberOfClauses = Convert.ToInt32(tokens[3]);

                // Read next lines (clauses)
                int clausesRead = 0;
                while (true)
                {
                    if (clausesRead == NumberOfClauses)
                    {
                        break;
                    }

                    tokens = reader.ReadLine().Split(' ', '\t');
                    for (int j = 0; j < tokens.Length; j++)
                    {
                        if (tokens[j] == "0")
                        {
                            clause = new Clause(variables);
                            variables.Clear();
                            Clauses.Add(clause);
                            clausesRead++;
                            continue;
                        }
                        if (tokens[j] == "")
                            continue;
                        if (tokens[j] == "c")
                            break;

                        if (tokens[j][0] == '-')    // Negation of a variable
                        {
                            sign = false;
                            name = tokens[j].Remove(0, 1);    // Remove the minus sign and use the number as a name
                        }
                        else
                        {
                            sign = true;
                            name = tokens[j];
                        }

                        variable = new Variable(sign, name);
                        variables.Add(variable);
                    }


                }

                FillVariablesDict(Clauses);
            }
            catch (Exception ex)    // Most likely the formatting was wrong.
            {
                if (ex is InvalidCastException || ex is IndexOutOfRangeException || ex is FormatException)
                {
                    throw new FormatException();
                }
                else
                {
                    throw ex;
                }
            }
        }

        internal void FillVariablesDict(List<Clause> clauses)
        {
            foreach (var clause in clauses)
            {
                foreach (var variable in clause.Variables)
                {
                    List<Variable> outList;
                    if (VariablesDict.TryGetValue(variable.Name, out outList))
                    {
                        outList.Add(variable);
                    }
                    else
                    {
                        VariablesDict.Add(variable.Name, new List<Variable>());
                        VariablesDict[variable.Name].Add(variable);
                    }
                }
            }
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

            var variableNames = VariablesDict.Keys.ToList();

            variableNames.Sort((x, y) =>
            {
                bool xContainsLetter = false;
                bool yContainsLetter = false;

                foreach (var c in x)
                {
                    if (char.IsLetter(c))
                    {
                        xContainsLetter = true;
                        break;
                    }
                }

                foreach (var c in y)
                {
                    if (char.IsLetter(c))
                    {
                        yContainsLetter = true;
                        break;
                    }
                }

                if (!xContainsLetter && !yContainsLetter)
                {
                    return Convert.ToInt32(x).CompareTo(Convert.ToInt32(y));
                }
                else
                {
                    return x.CompareTo(y);
                }
            });

            foreach (var variableName in variableNames)
            {
                stringBuilder.Append(String.Format("{0}: {1}\n", variableName, VariablesDict[variableName][0].Value));
            }

            return stringBuilder.ToString();
        }

        // Returns a possible original input in the DIMACS format
        public string GetInputFormat()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(String.Format("p cnf {0} {1}\n", VariablesDict.Count, Clauses.Count));

            int nameCounter = 1;

            Dictionary<string, int> X = new Dictionary<string, int>();

            // Assign a number to each variable
            foreach (var pair in VariablesDict)
            {
                X[pair.Key] = nameCounter++;
            }

            foreach (var clause in Clauses)
            {
                foreach (var variable in clause.Variables)
                {
                    string sign;

                    if (variable.Sign)
                        sign = "";
                    else
                        sign = "-";

                    stringBuilder.Append(String.Format("{0}{1} ", sign, X[variable.Name]));
                }

                stringBuilder.Append("0\n");
            }

            return stringBuilder.ToString();
        }
    }

    // Class representing one clause in the CNF formula
    internal class Clause
    {
        public List<Variable> Variables { get; private set; } // List of all variables in the clause

        public Clause(List<Variable> variables)
        {
            List<Variable> vars = new List<Variable>();

            foreach (var variable in variables)
            {
                vars.Add(new Variable(variable.Sign, variable.Value, variable.IsAssigned, variable.Name));
            }

            this.Variables = vars;
        }

        // Checks whether there is atleast one variable that is set to true
        // Not all variables have to be assigned yet
        public bool IsTrue()
        {
            foreach (var var in Variables)
            {
                if (var.IsAssigned && var.GetFinalValue())
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
                if (!var.IsAssigned)  // All variables have to be assigned
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
    internal class Variable
    {
        public bool Sign { get; set; }  // Either positive or negative literal
        public bool Value { get; set; } // Assigned value of the variable
        public bool IsAssigned { get; set; }  // Indicates whether this variable has been already assigned a value
        public string Name { get; private set; }  // Unique identifier

        public Variable(bool Sign, bool Value, bool IsAssigned, string Name)
        {
            this.Sign = Sign;
            this.Value = Value;
            this.IsAssigned = IsAssigned;
            this.Name = Name;
        }

        public Variable(bool Sign, string Name)
        {
            this.Sign = Sign;
            Value = false;
            IsAssigned = false;
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
            foreach (var variable in cnf.VariablesDict[this.Name])  // All variables in the formula with the same name get assigned
            {
                variable.Value = Value;
                variable.IsAssigned = true;
            }
        }

        // Sets the Assigned property for the variable in the whole CNF formula to false.
        public void UnsetValue(CNF cnf)
        {
            if (this.IsAssigned == false)
                return;

            foreach (var variable in cnf.VariablesDict[this.Name])
            {
                variable.IsAssigned = false;
            }
        }
    }

    // Class representing the DPLL algorithm with its methods and functions.
    public class DPLL
    {
        private Queue<CNF> sharedModelQueue = new Queue<CNF>();    // Queue of work that should be done by other threads (which is shared among them)

        private bool parallel = false;

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

    public class IndependentSetProblem
    {
        internal Graph Graph = new Graph();

        internal int K { get; set; }  // Does an independent set of size atleast K in a graph exist?

        public void ReadInput(TextReader reader)
        {
            var line = reader.ReadLine();
            K = Convert.ToInt32(line);
            Graph.ReadGraph(reader);
        }

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
            for (int i = 1; i <= K; i++)
            {
                for (int j = 1; j <= Graph.Vertices.Count; j++)
                {
                    Variable X_ij = new Variable(false, "X" + i.ToString() + j.ToString());

                    for (int k = 1; k <= K; k++)
                    {
                        if (i == k)
                        {
                            continue;
                        }

                        Variable X_kj = new Variable(false, "X" + k.ToString() + j.ToString());
                        clauses.Add(new Clause(new List<Variable> { X_ij, X_kj }));
                    }
                }
            }

            // Create clauses (!X_ij || !X_ik) for j != k
            for (int i = 1; i <= K; i++)
            {
                for (int j = 1; j <= Graph.Vertices.Count; j++)
                {
                    Variable X_ij = new Variable(false, "X" + i.ToString() + j.ToString());

                    for (int k = 1; k <= Graph.Vertices.Count; k++)
                    {
                        if (j == k)
                        {
                            continue;
                        }

                        Variable X_ik = new Variable(false, "X" + i.ToString() + k.ToString());
                        clauses.Add(new Clause(new List<Variable> { X_ij, X_ik }));
                    }
                }
            }

            // Create clauses (X_i0 || X_i1 || ... || X_in)
            for (int i = 1; i <= K; i++)
            {
                List<Variable> variables = new List<Variable>();

                for (int j = 1; j <= Graph.Vertices.Count; j++)
                {
                    variables.Add(new Variable(true, "X" + i.ToString() + j.ToString()));
                }

                if (variables.Count != 0)
                {
                    clauses.Add(new Clause(variables));
                }
            }

            // Create clauses (!X_ij || vertex_j)
            for (int i = 1; i <= K; i++)
            {
                for (int j = 1; j <= Graph.Vertices.Count; j++)
                {
                    clauses.Add(new Clause(new List<Variable> { new Variable(false, "X" + i.ToString() + j.ToString()), new Variable(true, j.ToString()) }));
                }
            }

            return new CNF(clauses);
        }

        // Returns a string explaining the result of the DPLL algorithm
        public string InterpretDPLLResult(DPLLResultHolder result)
        {
            if (result != null)
            {
                if (!result.SAT)
                {
                    return "Such independent set does not exist.";
                }

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(String.Format("The following vertices can be chosen for the independent set of size atleast {0}:\n", K));

                var variableNames = result.Model.VariablesDict.Keys.ToList();

                variableNames.Sort((x, y) =>
                {
                    if (x[0] != 'X' && y[0] != 'X')
                        return Convert.ToInt32(x).CompareTo(Convert.ToInt32(y));
                    else
                        return x.CompareTo(y);
                });

                foreach (var variableName in variableNames)
                {
                    if (!variableName.Contains('X') && result.Model.VariablesDict[variableName][0].Value) // Helper variables start with 'X', we do not want to output them
                    {
                        stringBuilder.Append(String.Format("{0}\n", variableName));
                    }
                }

                return stringBuilder.ToString();
            }

            return null;
        }
    }

    internal class Graph
    {
        public List<Vertex> Vertices;

        public Graph()
        {
            Vertices = new List<Vertex>();
        }

        public void ReadGraph(TextReader reader)
        {
            try
            {
                var line = reader.ReadLine();
                var tokens = line.Split(' ');

                foreach (var token in tokens)
                {
                    Convert.ToInt32(token); // Only numbers allowed (otherwise exception is thrown)
                    Vertices.Add(new Vertex(token));
                }

                if (Vertices.Count == 0)    // Empty graphs not supported
                    throw new FormatException();

                while (true)
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
            catch (Exception ex)
            {
                if (ex is IndexOutOfRangeException || ex is NullReferenceException || ex is FormatException)
                {
                    throw new FormatException();
                }
                else
                {
                    throw ex;
                }
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

    internal class Vertex
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

    public class ThreeColorabilityProblem
    {
        internal Graph graph = new Graph();

        public void ReadInput(TextReader textReader)
        {
            graph.ReadGraph(textReader);
        }

        public CNF ConvertToCNF()
        {
            List<Clause> clauses = new List<Clause>();

            foreach (var vertex in graph.Vertices)
            {
                clauses.Add(new Clause(new List<Variable>   // Vertex has either of 3 colors
                { new Variable(true, vertex.Id+"-Red"), new Variable(true, vertex.Id+"-Green"), new Variable(true, vertex.Id+"-Blue") }));

                // Vertex cannot have 2 colours at the same time
                clauses.Add(new Clause(new List<Variable> { new Variable(false, vertex.Id + "-Red"), new Variable(false, vertex.Id + "-Green") }));
                clauses.Add(new Clause(new List<Variable> { new Variable(false, vertex.Id + "-Red"), new Variable(false, vertex.Id + "-Blue") }));
                clauses.Add(new Clause(new List<Variable> { new Variable(false, vertex.Id + "-Green"), new Variable(false, vertex.Id + "-Blue") }));

                // Vertices sharing an edge cannot have the same colour
                foreach (var neighbour in vertex.Neighbours)
                {
                    clauses.Add(new Clause(new List<Variable> { new Variable(false, vertex.Id + "-Red"), new Variable(false, neighbour.Id + "-Red") }));
                    clauses.Add(new Clause(new List<Variable> { new Variable(false, vertex.Id + "-Green"), new Variable(false, neighbour.Id + "-Green") }));
                    clauses.Add(new Clause(new List<Variable> { new Variable(false, vertex.Id + "-Blue"), new Variable(false, neighbour.Id + "-Blue") }));
                }
            }

            return new CNF(clauses);
        }

        public string InterpretDPLLResult(DPLLResultHolder result)
        {
            if (result != null)
            {
                if (!result.SAT)
                {
                    return "Not 3-colorable.";
                }

                var variableNames = result.Model.VariablesDict.Keys.ToList();

                variableNames.Sort((x, y) => x.CompareTo(y));

                StringBuilder stringBuilder = new StringBuilder();

                foreach (var variableName in variableNames)
                {
                    if (result.Model.VariablesDict[variableName][0].Value)
                    {
                        string[] tokens = variableName.Split('-');

                        if (tokens.Length != 2) // The "result" is not a result of 3-colorability problem converted to SAT problem
                            throw new ArgumentException();

                        stringBuilder.Append(String.Format("{0} is {1}\n", tokens[0], tokens[1]));
                    }
                }

                return stringBuilder.ToString();
            }

            return null;
        }
    }

    public class HamiltonianPathProblem
    {
        internal Graph graph = new Graph();

        public void ReadInput(TextReader textReader)
        {
            graph.ReadGraph(textReader);
        }

        public CNF ConvertToCNF()
        {
            List<Clause> clauses = new List<Clause>();

            // X_i_j means that vertex j is on the i-th position of the path

            for (int i = 1; i <= graph.Vertices.Count; i++)
            {
                List<Variable> mustAppearList = new List<Variable>();
                List<Variable> posMustBeOccupied = new List<Variable>();

                for (int j = 1; j <= graph.Vertices.Count; j++)
                {
                    // Each vertex must appear in the path (X1j || X2j || ...)
                    mustAppearList.Add(new Variable(true, "X-" + j.ToString() + "-" + i.ToString()));

                    // Every position on the path must be occupied (Xi1 || Xi2 || ...)
                    posMustBeOccupied.Add(new Variable(true, "X-" + i.ToString() + "-" + j.ToString()));

                    Variable Xij = new Variable(false, "X-" + i.ToString() + "-" + j.ToString());

                    for (int k = 1; k <= graph.Vertices.Count; k++)
                    {
                        // No vertex can appear twice in the path (!Xij || !Xkj)
                        if (i != k)
                            clauses.Add(new Clause(new List<Variable> { Xij, new Variable(false, "X-" + k.ToString() + "-" + j.ToString()) }));

                        // No two vertices occupy same position in the path (!Xij || !Xik)
                        if (j != k)
                            clauses.Add(new Clause(new List<Variable> { Xij, new Variable(false, "X-" + i.ToString() + "-" + k.ToString()) }));
                    }
                }

                clauses.Add(new Clause(mustAppearList));
                clauses.Add(new Clause(posMustBeOccupied));
            }

            // Nonadjacent nodes cannot be adjacent in the path
            for (int i = 1; i <= graph.Vertices.Count; i++)
            {
                for (int j = 1; j <= graph.Vertices.Count; j++)
                {
                    if (graph.Vertices[i - 1].Neighbours.Contains(graph.Vertices[j - 1]))
                        continue;

                    for (int k = 1; k <= graph.Vertices.Count - 1; k++)
                    {
                        clauses.Add(new Clause(new List<Variable>
                        { new Variable(false, "X-"+k.ToString() + "-" + i.ToString()), new Variable(false, "X-"+(k+1).ToString() + "-" + j.ToString())}));
                    }
                }
            }

            return new CNF(clauses);
        }

        public string InterpretDPLLResult(DPLLResultHolder result)
        {
            if (result != null)
            {
                if (!result.SAT)
                {
                    return "No hamiltonian path exist.";
                }

                var variableNames = result.Model.VariablesDict.Keys.ToList();

                variableNames.Sort((x, y) => x.CompareTo(y));

                StringBuilder stringBuilder = new StringBuilder();

                foreach (var variableName in variableNames)
                {
                    if (result.Model.VariablesDict[variableName][0].Value)
                    {
                        string[] tokens = variableName.Split('-');

                        if (tokens.Length != 3) // The "result" is not a result of hamiltonian path problem converted to SAT problem
                            throw new ArgumentException();

                        stringBuilder.Append(String.Format("{0}. position is vertex {1}\n", tokens[1], tokens[2]));
                    }
                }

                return stringBuilder.ToString();
            }

            return null;
        }
    }

    class Program
    {
        static void PrintSATSolverUsage()
        {
            Console.WriteLine("-----------------------------------------SAT solver help-----------------------------------------");
            Console.WriteLine("Comment lines start with \"c \" ");
            Console.WriteLine("First line of input is \"p cnf number_of_variables number_of_clauses\".");
            Console.WriteLine("Then clause lines follow. It is recommended that clause is composed of integers starting at 1 " +
                "(Strings are possible if \"c\" string is not used as a variable.");
            Console.WriteLine("Each clause must end with 0 (zero).");
            Console.WriteLine("Each integer acts as a variable. Negative integer is negation of the variable.");
            Console.WriteLine();
            Console.WriteLine("Input format example: ");
            Console.WriteLine("c Ignored comment.");
            Console.WriteLine("p cnf 3 2");
            Console.WriteLine("-1 2 3 0");
            Console.WriteLine("2 -3 0");
            Console.WriteLine("-------------------------------------------------------------------------------------------------");
        }

        static void PrintIndependentSetUsage()
        {
            Console.WriteLine("----------------------------------Independent set problem help----------------------------------");
            Console.WriteLine("First line must be a positive integer which determines the minimum cardinality of the independent set to be found.");
            Console.WriteLine("Next the graph is to be input in the following format:");
            PrintGraphInputHelp();
            Console.WriteLine("------------------------------------------------------------------------------------------------");
        }

        static void Print3ColorabilityUsage()
        {
            Console.WriteLine("----------------------------------3-Colorability problem help-----------------------------------");
            PrintGraphInputHelp();
            Console.WriteLine("------------------------------------------------------------------------------------------------");
        }

        static void PrintHamiltonianPathUsage()
        {
            Console.WriteLine("----------------------------------Hamiltonian path problem help-----------------------------------");
            PrintGraphInputHelp();
            Console.WriteLine("--------------------------------------------------------------------------------------------------");
        }

        static void PrintGraphInputHelp()
        {
            Console.WriteLine("First line are integeres separated by a space which are vertices numbered from 1.");
            Console.WriteLine("Next lines are edges in the form \"first_vertex second_vertex\" (no need to add it also vice versa).");
            Console.WriteLine("An empty line means end of the input.");
            Console.WriteLine();
            Console.WriteLine("Graph input format example: ");
            Console.WriteLine("1 2 3");
            Console.WriteLine("1 2");
            Console.WriteLine("1 3");
            Console.WriteLine("");
        }

        static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    Console.WriteLine("Exit program (0), SAT solver (1), Independent set problem (2), 3-Colorability problem (3), Hamiltonian path problem (4)");
                    Console.Write("Choose an option (number): ");
                    int problemNumber = Convert.ToInt32(Console.ReadLine());

                    switch (problemNumber)
                    {
                        case 1: // SAT solver
                            PrintSATSolverUsage();

                            CNF cnf = new CNF();
                            cnf.ReadFormula(Console.In);

                            CNF cnfCopy = new CNF(cnf);

                            // Sequential solution
                            DPLL sequentialDPLL = new DPLL();
                            DPLLResultHolder sequentialResult = sequentialDPLL.Satisfiable(cnf);
                            Console.WriteLine();
                            Console.WriteLine(sequentialResult);

                            // Parallel solution
                            DPLL parallelDPLL = new DPLL();
                            DPLLResultHolder parallelResult = parallelDPLL.SatisfiableParallel(cnfCopy);
                            Console.WriteLine();
                            Console.WriteLine(parallelResult);

                            break;

                        case 2: // Independent set problem
                            PrintIndependentSetUsage();

                            IndependentSetProblem independentSetProblem = new IndependentSetProblem();
                            independentSetProblem.ReadInput(Console.In);

                            CNF independentSetProblemCNF = independentSetProblem.ConvertToCNF();

                            DPLL independentSetProblemDPLLParallel = new DPLL();
                            DPLLResultHolder independentSetProblemDPLLResultParallel = independentSetProblemDPLLParallel.SatisfiableParallel(independentSetProblemCNF);
                            Console.WriteLine();
                            Console.WriteLine(independentSetProblem.InterpretDPLLResult(independentSetProblemDPLLResultParallel));

                            break;

                        case 3: // 3-Colorability problem
                            Print3ColorabilityUsage();

                            ThreeColorabilityProblem threeColorabilityProblem = new ThreeColorabilityProblem();
                            threeColorabilityProblem.ReadInput(Console.In);

                            CNF threeColorabilityProblemCNF = threeColorabilityProblem.ConvertToCNF();

                            DPLL threeColorabilityProblemDPLLParallel = new DPLL();
                            DPLLResultHolder threeColorabilityProblemDPLLResultParallel = threeColorabilityProblemDPLLParallel.SatisfiableParallel(threeColorabilityProblemCNF);
                            Console.WriteLine();
                            Console.WriteLine(threeColorabilityProblem.InterpretDPLLResult(threeColorabilityProblemDPLLResultParallel));

                            break;

                        case 4: // Hamiltonian path problem
                            PrintHamiltonianPathUsage();

                            HamiltonianPathProblem hamiltonianPathProblem = new HamiltonianPathProblem();
                            hamiltonianPathProblem.ReadInput(Console.In);

                            CNF hamiltonianPathProblemCNF = hamiltonianPathProblem.ConvertToCNF();

                            DPLL hamiltonianPathProblemDPLLParallel = new DPLL();
                            DPLLResultHolder hamiltonianPathProblemDPLLResultParallel = hamiltonianPathProblemDPLLParallel.SatisfiableParallel(hamiltonianPathProblemCNF);
                            Console.WriteLine();
                            Console.WriteLine(hamiltonianPathProblem.InterpretDPLLResult(hamiltonianPathProblemDPLLResultParallel));

                            break;

                        default:    // Everything else will exit the program
                            break;
                    }

                    break;
                }
                catch (FormatException)
                {
                    Console.WriteLine();
                    Console.WriteLine("Invalid input format!");
                    Console.WriteLine();
                }
            }
        }
    }
}
