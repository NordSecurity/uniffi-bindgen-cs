## Figure out next version

Following [versioning rules](../README.md/#versioning), figure
out the version to be made. If there were any breaking changes since last version, bump the minor
component. If there weren't any breaking changes, bump the patch component.

The version follows semver, and consists of `uniffi-bindgen-cs` base version, followed by
upstream `uniffi-rs` version in the build metadata component (denoted as `+`). The upstream explicit
upstream `uniffi-rs` aids consumer to target the same upstream version when mixing multiple
generators, e.g. `uniffi-bindgen-cs` and `uniffi-bindgen-go`.
```
v0.6.0+v0.25.0
```

## Update version in [bindgen/Cargo.toml](../bindgen/Cargo.toml)

Note that the version in [bindgen/Cargo.toml](../bindgen/Cargo.toml) does is not prefixed with `v`,
i.e. `0.6.0+v0.25.0` instead of `v0.6.0+v0.25.0`.

## Update version in [Cargo.lock](../Cargo.lock)

Run any `cargo` command to include the new version in `Cargo.lock` file, e.g.
```
cargo build
```

## Update version in [README.md](../README.md)

- Update the [installation command](../README.md#how-to-install) to use the new version.
- If upstream uniffi-rs version was updated, add new entry to [versioning table](../README.md#versioning).

## Update [CHANGELOG.md](../CHANGELOG.md)

Inspect Git history, create a list of changes since last version.
- For breaking changes, prefix the change with `**BREAKING**:`
- For important changes, such as memory leak or race condition fixes, prefix the change with `**IMPORTANT**:`

## Create PR

Create PR, get the PR reviewed, and merge into `main`.

## Create tag

Once the PR is merged into `main`, create new tag in `main` branch.
