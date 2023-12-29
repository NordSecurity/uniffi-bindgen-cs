/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using uniffi.arithmetic;
using ArithmeticException = uniffi.arithmetic.ArithmeticException;

namespace UniffiCS.BindingTests;

public class TestArithmetic
{
    [Fact]
    public void ArithmeticWorks()
    {
        Assert.Equal(6ul, ArithmeticMethods.Add(2, 4));
        Assert.Equal(12ul, ArithmeticMethods.Add(4, 8));

        Assert.Throws<ArithmeticException.IntegerOverflow>(() => ArithmeticMethods.Sub(0, 2));

        Assert.Equal(2ul, ArithmeticMethods.Sub(4, 2));
        Assert.Equal(4ul, ArithmeticMethods.Sub(8, 4));

        Assert.Equal(2ul, ArithmeticMethods.Div(8, 4));

        Assert.Throws<PanicException>(() => ArithmeticMethods.Div(8, 0));

        Assert.True(ArithmeticMethods.Equal(2, 2));
        Assert.True(ArithmeticMethods.Equal(4, 4));

        Assert.False(ArithmeticMethods.Equal(2, 4));
        Assert.False(ArithmeticMethods.Equal(4, 8));
    }
}
