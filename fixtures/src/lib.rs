/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

mod uniffi_fixtures {
    arithmetical::uniffi_reexport_scaffolding!();
    custom_types::uniffi_reexport_scaffolding!();
    uniffi_callbacks::uniffi_reexport_scaffolding!();
    uniffi_geometry::uniffi_reexport_scaffolding!();
    uniffi_rondpoint::uniffi_reexport_scaffolding!();
    uniffi_sprites::uniffi_reexport_scaffolding!();
    uniffi_todolist::uniffi_reexport_scaffolding!();

    uniffi_chronological::uniffi_reexport_scaffolding!();
    uniffi_coverall::uniffi_reexport_scaffolding!();
    uniffi_fixture_callbacks::uniffi_reexport_scaffolding!();
    uniffi_fixture_docstring::uniffi_reexport_scaffolding!();

    global_methods_class_name::uniffi_reexport_scaffolding!();
    uniffi_cs_custom_types_builtin::uniffi_reexport_scaffolding!();
    uniffi_cs_disposable::uniffi_reexport_scaffolding!();
}
