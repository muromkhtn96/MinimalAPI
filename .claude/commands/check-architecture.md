Scan `backend/src/` and verify architecture rules from CLAUDE.md.

Check:
- **Layer deps** (CRITICAL): Domain has no refs to App/Infra/Api. App has no refs to Infra/Api. Check .csproj + usings.
- **File placement** (HIGH): files in correct layer per CLAUDE.md Architecture section.
- **Naming** (MEDIUM): files match CLAUDE.md Naming conventions.
- **Patterns** (HIGH): factory methods, private setters, Result<T>, no generic repo.

Output: `PASS | WARN | FAIL` + violations list with severity.
