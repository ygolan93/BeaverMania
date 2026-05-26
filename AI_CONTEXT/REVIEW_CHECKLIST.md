# Review Checklist

- [ ] Does the diff match the requested task?
- [ ] Were unrelated files changed?
- [ ] Were prefabs or scenes changed unexpectedly?
- [ ] Were serialized fields renamed or removed?
- [ ] Were public fields changed?
- [ ] Are plausible missing Inspector references protected with appropriate null checks?
- [ ] Are `Update`, `FixedUpdate`, `LateUpdate`, or other hot loops affected?
- [ ] Is manual Inspector assignment required?
- [ ] Is Unity Play Mode verification required?
- [ ] Is this safe to commit or merge?
