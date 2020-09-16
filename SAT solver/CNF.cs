using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SAT_solver
{
    // Class representing the CNF formula
    public sealed class CNF
    {
        public List<Clause> Clauses { get; private set; } // List of all clauses (each connected with conjuction)
        public Dictionary<string, List<Variable>> VariablesDict { get; private set; } // Key is a variable name, value is a list of variables with the same name
                 // All variables in the list must have the same IsAssigned, Value, Name properties if a variable with the name is assigned!

        public int NumberOfVariables { get; private set; } // Number of unique variables
        public int NumberOfClauses { get; private set; }   // Number of clauses

        public CNF()
        {
            Clauses = new List<Clause>();
            VariablesDict = new Dictionary<string, List<Variable>>();
        }

        public CNF(List<Clause> Clauses)
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

                    if (tokens == null)
                        throw new FormatException();

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
            catch (Exception ex) when (ex is InvalidCastException || ex is IndexOutOfRangeException || ex is FormatException)    // Most likely the formatting was wrong.
            {
                throw new FormatException();
            }
        }

        private void FillVariablesDict(List<Clause> clauses)
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
    public sealed class Clause
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
    public sealed class Variable
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
}
