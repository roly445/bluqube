# Decisions

## Integration Test Infrastructure — WebApplicationFactory Complete (SIMON-2026-002)

**Date:** 2026-04-20  
**Author:** Simon (Tester/QA)  
**Status:** IMPLEMENTED (PARTIAL)

**Summary:** WebApplicationFactory-based integration test infrastructure implemented. 5 out of 8 URL binding integration tests now pass. Infrastructure includes test web application setup, manual endpoint registration, custom JSON converters, and end-to-end HTTP round-trip testing. 134 passed, 2 skipped, 3 failed (139 total).

**Key Finding:** BluQube's Responding source generator only scans referenced assemblies, not current compilation. For test scenarios with all artifacts in one assembly, manual endpoint registration is required.

**Test Results:**
- ✅ **5 Passing:** Route parameter transmission, body+route splitting, case-insensitive matching, URL escaping, multiple parameters ordering
- ❌ **3 Failing:** QueryResult<T> JSON deserialization (requires investigation)

**Handoff:** QueryResult<T> issue escalated to framework team (Kaylee).

**Files Created:** 7 new files in `tests/BluQube.Tests/Integration/` plus Program.cs  
**Modified:** BluQube.Tests.csproj (added Microsoft.AspNetCore.Mvc.Testing), UrlBindingIntegrationTests.cs

---

## URL Binding Documentation Complete (INARA-2026-001)

**Date:** 2026-04-20  
**Author:** Inara (Docs/DevRel)  
**Status:** COMPLETE

**Summary:** Documented URL binding feature in README.md (lines 75–141) with clear examples showing path parameter inference, automatic binding, and WASM compatibility. Feature now discoverable for new developers.

**Content Added:**
- Overview of RESTful pattern support and automatic inference
- Path Parameter Inference subsection with 4 developer-centric examples
- Automatic URL building explanation (AOT-safe implementation)
- POST query default behavior clarification

**Accuracy:** All documented behaviors verified against MAL-2026-005.

**Files Modified:** README.md

---

## JSON Converter Registration & EqualityContract Filter Fixed (KAYLEE-2026-002-REVISED)

**Date:** 2026-04-21  
**Author:** Kaylee (Framework Dev)  
**Status:** IMPLEMENTED

**Summary:** Fixed two critical bugs in the `Responding.cs` source generator preventing integration tests from passing. QueryResult<T> converters were not being registered in same-assembly scenarios (tests, simple apps), and synthesized C# record properties were corrupting generated code.

### Bug 1: Assembly Scanning Gap

**Root cause:** `Responding.cs` generator only scanned `source.Right.References` (external assemblies), missing converters defined in the current compilation. Integration tests define queries, processors, AND converters in the same assembly (`BluQube.Tests`).

**Fix:** Modified lines 66-79 to create `assembliesToCheck` list including both current assembly (`source.Right.Assembly`) and all references:

```csharp
var assembliesToCheck = new List<IAssemblySymbol>();
var currentAssemblyName = source.Right.Assembly.Name;
assembliesToCheck.Add(source.Right.Assembly);  // <-- Add current compilation
foreach (var reference in source.Right.References)
{
    if (source.Right.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol refAssembly)
    {
        if (refAssembly.Name == currentAssemblyName) continue;
        assembliesToCheck.Add(refAssembly);
    }
}
```

### Bug 2: EqualityContract Synthesis

**Root cause:** C# records automatically synthesize `EqualityContract` property (`System.Type`) for runtime type discrimination. This property appeared in `GetMembers()` and was extracted as a positional record parameter, causing invalid generated code.

**Fix:** Added filtering to parameter extraction (lines 114-121, 171-178):

```csharp
foreach (var member in queryTypeDecl.GetMembers().OfType<IPropertySymbol>())
{
    if (member.IsImplicitlyDeclared || member.Name == "EqualityContract")
        continue;
    recordParams.Add(new RecordParameterInfo(member.Name, member.Type.ToDisplayString()));
}
```

### Test Impact

**Before:** 134 passed, 3 failed (GetQueryWithPathAndQuerystring, NullableQuerystringParameter, PostQueryWithRouteParameter)  
**After:** 137 passed, 0 failed  
**All 8 URL binding integration tests now passing**

**File Modified:** `src/BluQube.SourceGeneration/Responding.cs` (~15 lines added)

### Key Learning

Roslyn generators must handle same-assembly scenarios. Integration tests, single-project apps, and other real-world use cases define everything in one compilation unit. Generator must scan `source.Right.Assembly` in addition to `References`.

---

## URL Binding Tests Complete — Gaps Identified (Previous)

**Date:** 2026-04-20  
**Author:** Simon (Tester/QA)  
**Status:** MERGED

URL binding test scaffolds enabled and implemented. 129 tests passed (up from 118), 10 skipped, 0 failed. Real coverage added for PathTemplateParser (7 tests) and generator functionality (5 tests).

**Test Gaps Identified:**
1. GET Query Generator Tests Fail in Isolation (Low severity, workaround: feature works in real builds)
2. Responding Generator Requires Full ASP.NET Context (Low severity, workaround: integration tests verify)
3. Integration Test Infrastructure Not Yet Built (Medium severity, now RESOLVED by SIMON-2026-002)

**Recommendation:** Accept gaps and merge. Feature works, critical paths tested, gaps clearly documented.

**Files Changed:** BluQube.SourceGeneration.csproj, PathTemplateParserTests.cs, UrlBindingGeneratorTests.cs, UrlBindingIntegrationTests.cs
