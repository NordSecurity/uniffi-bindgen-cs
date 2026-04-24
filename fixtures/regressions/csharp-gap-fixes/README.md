# C# 0.30 gap-fix regressions

This fixture guards two regressions:

- Method name conflicts when an object method matches its class name.
- Nested jagged array generation for `sequence<sequence<sequence<u8>>>`.
