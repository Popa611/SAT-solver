using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SAT solver benchmark")]

namespace SAT_solver
{
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
