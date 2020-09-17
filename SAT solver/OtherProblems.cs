using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SAT_solver
{
    public class IndependentSetProblem
    {
        public Graph Graph = new Graph();

        public int K { get; set; }  // Does an independent set of size atleast K in a graph exist?

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

    public class Graph
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

    public class Vertex
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
}
