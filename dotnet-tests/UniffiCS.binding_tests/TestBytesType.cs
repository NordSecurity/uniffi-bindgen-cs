/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using uniffi.bytes_type;

// This test checks that `bytes` type emits correct code when
// neither `Vec<u8>` nor `u8` are referenced in UDL file.

public class TestBytesType {
    [Fact]
    public void TestBytesTypeWorks() {
        Assert.Equal(new List<byte> { 3, 2, 1 }, BytesTypeMethods.Reverse(new List<byte> { 1, 2, 3 }));
    }
}
