# https://github.com/NordSecurity/uniffi-bindgen-cs/issues/60

Associated enum class names conflict with top level type definitions.

```C#
public record Rectangle(double @width, double @height) { }
public record Shape
{                    ____________
                     ∨          ∧
    public record Rectangle(Rectangle @s) : Shape { }
    public record Ellipse(Ellipse @s) : Shape { }
}
```
