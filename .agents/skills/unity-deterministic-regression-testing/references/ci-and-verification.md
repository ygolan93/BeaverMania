# CI And Verification

## BeaverProject Compile Check

Use the repo compilation shim for C# verification:

```powershell
dotnet build .ci/BeaverMania.CI.csproj
```

This proves:

- syntax validity
- namespace and type resolution
- assembly reference integrity visible to the `.ci` project

This does not prove:

- Inspector wiring
- prefab or scene references
- runtime callback timing
- coroutine completion
- animation, physics, or audio behavior

## Unity Test Runner Batch Commands

Use these as command patterns when the environment has a licensed Unity Editor:

EditMode:

```bash
<UnityEditorPath>/Unity -batchmode -nographics -projectPath <ProjectPath> -runTests -testPlatform editmode -testResults <OutputPath>/editmode-results.xml -quit
```

PlayMode:

```bash
<UnityEditorPath>/Unity -batchmode -nographics -projectPath <ProjectPath> -runTests -testPlatform playmode -testResults <OutputPath>/playmode-results.xml -quit
```

In the documented Cursor Cloud environment, the editor path is `/opt/unity/Editor/Unity`, but PlayMode and batch execution still require a valid Unity license.

## Verification Language

Use precise wording:

- compilation passed
- UTF EditMode tests passed
- UTF PlayMode tests passed
- runtime behavior not verified in Unity Editor

Never collapse compile-only evidence into a runtime-fix claim.

If runtime behavior remains unverified, end the response with:

`Needs Unity Play Mode verification.`
