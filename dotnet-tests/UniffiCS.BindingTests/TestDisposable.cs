/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using uniffi.disposable;

namespace UniffiCS.BindingTests;

public class TestDisposable {
    [Fact]
    public void ObjectDecrementsLiveCount() {
        using (var resource = DisposableMethods.GetResource()) {
            Assert.Equal(1, DisposableMethods.GetLiveCount());
        }
        Assert.Equal(0, DisposableMethods.GetLiveCount());
    }

    [Fact]
    public void MapDecrementsLiveCount() {
        using (var journal = DisposableMethods.GetResourceJournalMap()) {
            Assert.Equal(2, DisposableMethods.GetLiveCount());
        }
        Assert.Equal(0, DisposableMethods.GetLiveCount());
    }

    [Fact]
    public void ListDecrementsLiveCount() {
        using (var journal = DisposableMethods.GetResourceJournalList()) {
            Assert.Equal(2, DisposableMethods.GetLiveCount());
        }
        Assert.Equal(0, DisposableMethods.GetLiveCount());
    }

    [Fact]
    public void MapListDecrementsLiveCount() {
        using (var journal = DisposableMethods.GetResourceJournalMapList()) {
            Assert.Equal(2, DisposableMethods.GetLiveCount());
        }
        Assert.Equal(0, DisposableMethods.GetLiveCount());
    }

    [Fact]
    public void EnumDecrementsLiveCount() {
        using (var maybe_journal = DisposableMethods.GetMaybeResourceJournal()) {
            Assert.Equal(2, DisposableMethods.GetLiveCount());
        }
        Assert.Equal(0, DisposableMethods.GetLiveCount());
    }
}