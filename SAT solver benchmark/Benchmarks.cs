using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using SAT_solver;
using System.IO;

namespace SAT_solver_benchmark
{
    public class SATBenchmark
    {
        [Params("42Vars_133Clauses_UNSAT.txt", "155Vars_1135Clauses_SAT.txt", "350Vars_1349Clauses_SAT.txt", "350Vars_1349Clauses_UNSAT.txt")]
        public string file;

        [Benchmark]
        public DPLLResultHolder SequentialSAT()
        {
            CNF cnf = new CNF();
            cnf.ReadFormula(new StreamReader(file));
            DPLL dpll = new DPLL();
            return dpll.Satisfiable(cnf);
        }

        [Benchmark]
        public DPLLResultHolder ParallelSAT()
        {
            CNF cnf = new CNF();
            cnf.ReadFormula(new StreamReader(file));
            DPLL dpll = new DPLL();
            return dpll.SatisfiableParallel(cnf);
        }
    }
    class Benchmarks
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<SATBenchmark>();
        }
    }
}
