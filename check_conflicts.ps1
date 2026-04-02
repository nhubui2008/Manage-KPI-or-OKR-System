$conflicts = git diff --name-only --diff-filter=U
if ($conflicts) {
    echo "Conflicts in:"
    echo $conflicts
} else {
    echo "No conflicts"
}
git status
