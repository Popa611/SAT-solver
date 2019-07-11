using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAT_solver
{
    class CNF
    {
        public List<Clause> Clauses { get; private set; }
        private int NumberOfVariables { get; set; }
        private int NumberOfClauses { get; set; }

        public CNF()
        {
            Clauses = new List<Clause>();
        }

        // Deep copy constructor
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
            uint name;

            try
            {
                // Read, check and parse first line
                string[] tokens = reader.ReadLine().Split(' ');

                if (tokens[0] != "cnf")
                {
                    throw new FormatException();
                }

                NumberOfVariables = Convert.ToInt32(tokens[1]);
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
                            name = Convert.ToUInt32(token.Remove(0, 1));    // Remove the minus sign and use the number as a name
                        }
                        else
                        {
                            sign = true;
                            name = Convert.ToUInt32(token);
                        }

                        variable = new Variable(sign, name);
                        variables.Add(variable);
                    }

                    clause = new Clause(variables);
                    variables.Clear();
                    Clauses.Add(clause);
                }
            }
            catch (Exception ex)
            {
                if (ex is InvalidCastException || ex is IndexOutOfRangeException || ex is FormatException)
                {
                    throw new FormatException();
                }
            }
        }

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
            List<Variable> res = new List<Variable>();
            bool added;

            foreach (var clause in Clauses)
            {
                foreach (var var in clause.Variables)
                {
                    added = false;
                    foreach (var uvar in res)
                    {
                        if (uvar.Name == var.Name)  // If var is already in the "res" list, do not add it
                        {
                            added = true;
                            break;
                        }
                    }

                    if (!added)
                    {
                        res.Add(var);
                    }
                }
            }

            return res;
        }

        public override string ToString()
        {
            if (Clauses == null)
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

    class Clause
    {
        public List<Variable> Variables { get; private set; }

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

    class Variable
    {
        public bool Sign { get; set; }  // Either positive or negative literal
        public bool Value { get; set; } // Assigned value of the variable
        public bool Assigned { get; set; }  // Indicates whether this variable has been already assigned a value
        public uint Name { get; private set; }

        public Variable (bool Sign, bool Value, bool Assigned, uint Name)
        {
            this.Sign = Sign;
            this.Value = Value;
            this.Assigned = Assigned;
            this.Name = Name;
        }

        public Variable (bool Sign, uint Name)
        {
            this.Sign = Sign;
            Value = true;
            Assigned = false;
            this.Name = Name;
        }

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
                if (variable.Name == this.Name)
                {
                    variable.Value = Value;
                    variable.Assigned = true;
                }
            }
        }

        // Sets the Assigned property for the variable in the whole CNF formula.
        public void Unassign(CNF cnf)
        {
            foreach (var variable in cnf.GetVariables())
            {
                if (variable.Name == this.Name)
                {
                    Assigned = false;
                }
            }
        }
    }

    class DPLL
    {
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

            Variable variable = GetPureVariable(cnf);
            if (variable != null)   // If we have found a pure literal, we can set it so it is true in all clauses.
            {
                variable.SetValue(cnf, variable.Sign);
                return Satisfiable(cnf);
            }

            variable = GetUnitClause(cnf);
            if (variable != null)   // If we have found a unit clause, we can set it so it is true
            {
                variable.SetValue(cnf, variable.Sign);
                return Satisfiable(cnf);
            }

            variable = FindUnassigned(cnf);
            if (variable != null)
            {
                return Branch(cnf, variable);
            }
            else
            {
                return new DPLLResultHolder(false, null);   // No unassigned variable exist and we did not return SAT yet -> UNSAT
            }
        }

        private DPLLResultHolder Branch (CNF cnf, Variable variable)
        {
            CNF BranchCNF = new CNF(cnf);
            variable.SetValue(cnf, true);
            DPLLResultHolder result = Satisfiable(cnf);

            if (result.SAT)
            {
                return result;
            }
            else
            {
                variable.SetValue(BranchCNF, false);
                return Satisfiable(BranchCNF);
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

            foreach (var variable in cnf.GetUniqueVariables())
            {
                if (variable.Assigned)
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

                            if (IsPositive && IsNegative)
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
        public bool SAT { get; private set; }  // True if a CNF is satisfiable. Otherwise false.
        public CNF Model { get; private set; } // Holds the model of the CNF if CNF is satisfiable. If SAT is false, it should not be read (set to null preferably)

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

            while (true)
            {
                try
                {
                    cnf.ReadFormula(Console.In);
                    break;
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid input format!");
                    PrintUsage();
                }
            }

            DPLL dpll = new DPLL();
            DPLLResultHolder result = dpll.Satisfiable(cnf);
            Console.WriteLine();
            Console.WriteLine(result);
        }
    }
}
