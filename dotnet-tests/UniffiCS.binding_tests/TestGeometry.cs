/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using uniffi.geometry;

public class TestGeometry {
    [Fact]
    public void GeometryWorks() {
        var ln1 = new Line(new Point(0, 0), new Point(1, 2));
        var ln2 = new Line(new Point(1, 1), new Point(2, 2));

        Assert.Equal(2, GeometryMethods.Gradient(ln1));
        Assert.Equal(1, GeometryMethods.Gradient(ln2));

        Assert.Equal(new Point(0, 0), GeometryMethods.Intersection(ln1, ln2));
        Assert.Null(GeometryMethods.Intersection(ln1, ln1));
    }
}
