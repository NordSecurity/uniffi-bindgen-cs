// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using uniffi.sprites;

namespace UniffiCS.BindingTests;

public class TestSprites
{
    [Fact]
    public void SpritesWork()
    {
        using (var sempty = new Sprite((Point?)null))
        {
            Assert.Equal(new Point(0, 0), sempty.GetPosition());
        }

        var s = new Sprite(new Point(0, 1));
        Assert.Equal(new Point(0, 1), s.GetPosition());

        s.MoveTo(new Point(1, 2));
        Assert.Equal(new Point(1, 2), s.GetPosition());

        s.MoveBy(new Vector(-4, 2));
        Assert.Equal(new Point(-3, 4), s.GetPosition());

        s.Dispose();
        Assert.Throws<System.ObjectDisposedException>(() => s.MoveBy(new Vector(0, 0)));

        using (var srel = Sprite.NewRelativeTo(new Point(0, 1), new Vector(1, 1.5)))
        {
            Assert.Equal(new Point(1, 2.5), srel.GetPosition());
        }
    }
}
