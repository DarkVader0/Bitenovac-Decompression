# Walks the reverse edges of the project graph from a set of seed projects, so a project is
# selected when it changed or when anything it depends on changed.
#
#   awk -F'\t' -v edges=<edges.tsv> -v seeds=<seeds.txt> -f affected.awk <edges.tsv> <seeds.txt>
#
# The traversal is O(vertices + edges) with an index cursor over the queue. Doing this in the
# shell instead means an array copy per dequeue and a string append per edge, both of which are
# quadratic in bash and become the dominant cost of every CI step once the graph is large.
#
# Files are told apart by name rather than by the usual NR == FNR trick, which misfires when
# the first file is empty -- a repository whose projects have no ProjectReference at all.

FILENAME == edges {
    if ($1 == "" || $2 == "")
        next

    # Reverse the edge: a change to $2 must rebuild $1.
    dependents[$2, ++count[$2]] = $1
    next
}

FILENAME == seeds {
    if ($0 == "" || ($0 in seen))
        next

    seen[$0] = 1
    queue[++tail] = $0
    next
}

END {
    head = 0
    while (head < tail) {
        current = queue[++head]

        total = count[current]
        for (position = 1; position <= total; position++) {
            next_project = dependents[current, position]
            if (next_project in seen)
                continue

            seen[next_project] = 1
            queue[++tail] = next_project
        }
    }

    for (project in seen)
        print project
}
