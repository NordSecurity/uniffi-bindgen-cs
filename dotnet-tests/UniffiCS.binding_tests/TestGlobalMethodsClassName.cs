/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Diagnostics;
using System.IO;
using System;
using uniffi.global_methods_class_name;

public class TestGlobalMethodsClassName {
    [Fact]
    public void GlobalMethodsClassNameWorks() {
        Assert.Equal("Hello, world!", LibGreeter.HelloWorld());
    }
}
