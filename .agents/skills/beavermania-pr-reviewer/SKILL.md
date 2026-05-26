---
name: beavermania-pr-reviewer
description: Use this skill to review a Beavermania branch or PR before merge. Checks scope, changed files, regression risk, commit quality, and prepares a concise PR summary.
---

# Beavermania PR Reviewer

Review the branch or diff before merge.

## Check

- Does the diff match the requested task?
- Were unrelated files changed?
- Were scene or prefab files changed unexpectedly?
- Are there regression risks?
- Are manual Unity checks required?
- Is the change small enough to merge safely?

## Output

## PR Summary

Short summary of the change.

## Changed Files

List important files and why they changed.

## Risk Assessment

Low / Medium / High with explanation.

## Required Verification

List Play Mode or build checks.

## Merge Recommendation

Merge / Do not merge / Needs manual Unity verification first.
