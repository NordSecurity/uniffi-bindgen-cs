# Problem

C# does not support initializing an empty list in a default parameter, i.e.
```
void Method(List<String> parameter = new List<String>());
```

# Proposal

Use `null` to represent an empty list in default method parameters, i.e.
```
void Method(List<String> parameter = null);
```

# Considerations

Using `null` to represent an empty list is a code smell. Its very easy to forget
to check for null, especially when using a list. However, in this case its possible
to workaround this issue:
- Lower: check for null, write empty list into RustBuffer instead of null
- Lift: lifting is not affected by this problem, since lifting is not affected by
    default parameter limitations, and can easily return an empty list instead of null.

# Links

[stackoverflow](https://stackoverflow.com/questions/6947470/c-how-to-use-empty-liststring-as-optional-parameter)
