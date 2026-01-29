// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using uniffi.todolist;

namespace UniffiCS.BindingTests;

public class TestTodoList
{
    [Fact]
    public void TodoListWorks()
    {
        var todo = new TodoList();

        Assert.Throws<TodoException.EmptyTodoList>(() => todo.GetLast());

        Assert.Throws<TodoException.EmptyString>(() => TodolistMethods.CreateEntryWith(""));

        todo.AddItem("Write strings support");
        Assert.Equal("Write strings support", todo.GetLast());

        todo.AddItem("Write tests for strings support");
        Assert.Equal("Write tests for strings support", todo.GetLast());

        var entry = TodolistMethods.CreateEntryWith("Write bindings for strings as record members");
        todo.AddEntry(entry);
        Assert.Equal("Write bindings for strings as record members", todo.GetLast());
        Assert.Equal("Write bindings for strings as record members", todo.GetLastEntry().Text);

        todo.AddItem("Test Ünicode hàndling without an entry 🤣");
        Assert.Equal("Test Ünicode hàndling without an entry 🤣", todo.GetLast());

        var entry2 = new TodoEntry("Test Ünicode hàndling in an entry 🤣");
        todo.AddEntry(entry2);
        Assert.Equal("Test Ünicode hàndling in an entry 🤣", todo.GetLastEntry().Text);

        Assert.Equal(5, todo.GetEntries().Length);

        todo.AddEntries([new TodoEntry("foo"), new TodoEntry("bar") ]);
        Assert.Equal(7, todo.GetEntries().Length);
        Assert.Equal("bar", todo.GetLastEntry().Text);

        todo.AddItems([ "bobo", "fofo"] );
        Assert.Equal(9, todo.GetItems().Length);
        Assert.Equal("bobo", todo.GetItems()[7]);

        Assert.Null(TodolistMethods.GetDefaultList());

        // https://github.com/xunit/xunit/issues/2027
#pragma warning disable CS8602

        // Note that each individual object instance needs to be explicitly destroyed,
        // either by using the `.use` helper or explicitly calling its `.destroy` method.
        // Failure to do so will leak the underlying Rust object.
        using (var todo2 = new TodoList())
        {
            TodolistMethods.SetDefaultList(todo);
            using (var defaultList = TodolistMethods.GetDefaultList())
            {
                Assert.NotNull(defaultList);
                Assert.Equal(todo.GetEntries(), defaultList.GetEntries());
                Assert.NotEqual(todo2.GetEntries(), defaultList.GetEntries());
            }

            todo2.MakeDefault();
            using (var defaultList = TodolistMethods.GetDefaultList())
            {
                Assert.NotEqual(todo.GetEntries(), defaultList.GetEntries());
                Assert.Equal(todo2.GetEntries(), defaultList.GetEntries());
            }

            todo.AddItem("Test liveness after being demoted from default");
            Assert.Equal("Test liveness after being demoted from default", todo.GetLast());

            todo2.AddItem("Test shared state through local vs default reference");
            using (var defaultList = TodolistMethods.GetDefaultList())
            {
                Assert.Equal("Test shared state through local vs default reference", defaultList.GetLast());
            }
        }

#pragma warning restore CS8602

        // Ensure the kotlin version of deinit doesn't crash, and is idempotent.
        todo.Dispose();
        todo.Dispose();
    }
}
