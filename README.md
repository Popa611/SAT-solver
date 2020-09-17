# Parallel SAT solver
Simple implementation of DPLL algorithm and its parallelization in C#. The code also contains some other NP-complete problems which are solved by being converted to the SAT problem, solving it and interpreting the results.

## DPLL
DPLL is a backtracking algorithm which picks a literal (at a "**branching point**"), assigns a value to it and recursively checks the satisfiability of the simplified formula. If it is satisfiable, the original formula is as well. If it is not, the algorithm backtracks back to the branching point and assigns the literal the opposite value than before.

This algorithm can be improved by using 2 additional rules. The first is called **unit propagation**. It says that if a clause contains single literal then it can be assigned a value so that the clause is true.

The next rule called **pure literal elimination** says that if a literal is pure (a literal with only one sign in the whole formula) then it can be assigned a value so that it is true in all clauses. Using this rule the solver gets rid of all the clauses containing such literal.

### Parallelization
The algorithm tries to divide the work equally between all threads. At the start of the algorithm several (according to the number of available logical processors) threads (*Tasks*) are created which share a queue, in which they add additional non-complete models to be solved. These models are added to the queue at the branching point. If one thread concludes that its model is unsatisfiable, it then tries to get another model to solve from the queue. A thread which finds a satisfiable model returns the model and all the other threads are aborted (Tasks cancelled). This is a version of the producer-consumer model in which the working threads are both producers and consumers.
