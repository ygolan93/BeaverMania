# Codex Workflow

1. Use Codex for a small isolated C# script change or a focused review.
2. Use a branch or worktree for risky tasks.
3. Keep each Codex task focused on one bug or one review.
4. Do not let Codex and Cursor edit the same prefab, scene, animation, or UI files at the same time.
5. Use Cursor with Unity Editor for prefab, UI hierarchy, animation, scene, serialized Inspector, and visual/Play Mode work.
6. Use Codex to review the result after Cursor implementation.
7. Before merge, run the `beavermania-pr-reviewer` skill.
8. After any changes, require a short report containing:
   - Changed files.
   - Risk level.
   - Verification steps performed.
   - Whether Unity Play Mode verification is still required.

For script fixes, prefer `beavermania-script-fixer`. For Unity asset/serialization risk review, prefer `beavermania-unity-safety-reviewer`.
