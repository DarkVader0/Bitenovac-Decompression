# Emits one "project<TAB>referencedProject" edge per ProjectReference, for every project file
# handed to it, in a single pass.
#
# This replaces a grep-per-project plus a realpath fork per reference, which at several hundred
# projects is thousands of processes per CI step.
#
# Paths are normalised here rather than by shelling out, so a reference written as
# ../../src/Foo/Foo.csproj with Windows separators compares equal to the discovered path.
# References are matched against the whole file rather than line by line: an element split
# across lines is valid MSBuild, and a line-oriented match drops it.

function normalise(path,    parts, count, position, stack, depth, result) {
    gsub(/\\/, "/", path)
    gsub(/\/+/, "/", path)

    count = split(path, parts, "/")
    depth = 0
    for (position = 1; position <= count; position++) {
        if (parts[position] == "" || parts[position] == ".")
            continue
        if (parts[position] == "..") {
            if (depth > 0)
                depth--
            continue
        }
        stack[++depth] = parts[position]
    }

    result = ""
    for (position = 1; position <= depth; position++)
        result = (position == 1) ? stack[position] : result "/" stack[position]

    return result
}

# Called when the last line of a project file has been read, which awk only signals by the
# filename changing or by the input ending.
function flush(    directory, rest, element, include) {
    if (current == "")
        return

    directory = current
    if (!sub(/\/[^\/]*$/, "", directory))
        directory = "."

    rest = buffer
    while (match(rest, /<ProjectReference[^>]*Include[ \t]*=[ \t]*"[^"]*"/)) {
        element = substr(rest, RSTART, RLENGTH)
        rest = substr(rest, RSTART + RLENGTH)

        if (!match(element, /Include[ \t]*=[ \t]*"[^"]*"/))
            continue

        include = substr(element, RSTART, RLENGTH)
        sub(/^Include[ \t]*=[ \t]*"/, "", include)
        sub(/"$/, "", include)
        if (include == "")
            continue

        printf "%s\t%s\n", current, normalise(directory "/" include)
    }
}

FNR == 1 {
    flush()
    current = FILENAME
    sub(/^\.\//, "", current)
    buffer = ""
}

{
    sub(/\r$/, "")
    # Joining with a space lets the element regex span what were separate lines.
    buffer = buffer " " $0
}

END {
    flush()
}
