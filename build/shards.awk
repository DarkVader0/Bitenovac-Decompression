# Partitions the affected projects into shards that can each be built, tested and coverage-gated
# on their own runner, with no data passed between them.
#
#   awk -F'\t' -v edges=<edges.tsv> -v affected=<affected.txt> -v info=<info.tsv> \
#       -v seeds=<seeds.txt> -v shards=<count> \
#       -f shards.awk <edges.tsv> <affected.txt> <info.tsv> <seeds.txt>
#
# Emits "shardIndex<TAB>project".
#
# A project's coverage is measured by the test projects referencing it, and those reports must
# be merged before a percentage means anything, since each test project exercises only part of a
# library. A gated project and every test project reaching it therefore land in the same shard;
# otherwise the gate reads a fraction of the real coverage and fails a fully covered library.
#
# Only the seeds are gated: the projects the pull request changed. Fusing on every affected
# project instead lets one widely referenced library drag its entire dependent tree into a single
# indivisible group, collapsing the partition to one shard on a repository whose projects share a
# common core.
#
# Dependents are still built and their tests still run, but they are not re-measured, so they
# carry no grouping constraint and are packed wherever there is room.
#
# The constraint is expressed as a union-find over the forward dependency closure of each test
# project. The resulting groups are indivisible; they are then packed into the requested number
# of shards largest-first, which is the standard greedy approximation for balancing bin loads.

function find_root(node) {
    while (parent[node] != node) {
        parent[node] = parent[parent[node]]
        node = parent[node]
    }
    return node
}

function merge(left, right,    left_root, right_root) {
    left_root = find_root(left)
    right_root = find_root(right)
    if (left_root != right_root)
        parent[right_root] = left_root
}

FILENAME == edges {
    if ($1 == "" || $2 == "")
        next

    dependencies[$1, ++count[$1]] = $2
    next
}

FILENAME == affected {
    if ($0 == "")
        next

    is_affected[$0] = 1
    projects[++project_count] = $0
    next
}

FILENAME == info {
    if ($1 != "")
        is_test[$1] = ($3 == "true")
    next
}

FILENAME == seeds {
    if ($0 != "")
        is_gated[$0] = 1
    next
}

END {
    if (project_count == 0)
        exit 0

    for (i = 1; i <= project_count; i++)
        parent[projects[i]] = projects[i]

    # Every gated project a test project can reach joins that test project's group. A test
    # project that reaches nothing gated stays a group of one, as does every dependent that is
    # only being rebuilt.
    for (i = 1; i <= project_count; i++) {
        root = projects[i]
        if (!is_test[root])
            continue

        delete stack
        delete visited
        depth = 0
        stack[++depth] = root

        while (depth > 0) {
            current = stack[depth--]
            if (current in visited)
                continue
            visited[current] = 1

            if ((current in is_affected) && (current in is_gated))
                merge(root, current)

            total = count[current]
            for (j = 1; j <= total; j++)
                stack[++depth] = dependencies[current, j]
        }
    }

    # Collect the groups and order them largest first.
    for (i = 1; i <= project_count; i++) {
        root = find_root(projects[i])
        if (!(root in group_size))
            roots[++group_count] = root
        group_size[root]++
    }

    for (i = 2; i <= group_count; i++) {
        candidate = roots[i]
        j = i - 1
        while (j >= 1 && group_size[roots[j]] < group_size[candidate]) {
            roots[j + 1] = roots[j]
            j--
        }
        roots[j + 1] = candidate
    }

    bins = shards + 0
    if (bins < 1)
        bins = 1
    if (bins > group_count)
        bins = group_count

    for (i = 0; i < bins; i++)
        load[i] = 0

    for (i = 1; i <= group_count; i++) {
        lightest = 0
        for (b = 1; b < bins; b++)
            if (load[b] < load[lightest])
                lightest = b

        shard_of[roots[i]] = lightest
        load[lightest] += group_size[roots[i]]
    }

    for (i = 1; i <= project_count; i++)
        printf "%d\t%s\n", shard_of[find_root(projects[i])], projects[i]
}
