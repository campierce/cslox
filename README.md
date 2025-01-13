# cslox

A tree-walk interpreter for the Lox programming language, written in C#, based on [Crafting Interpreters](https://craftinginterpreters.com/).

Lox is a small, dynamically-typed language with C-like syntax, first-class functions, and object-oriented features.

## Example

```lox
class Fib {
  // returns a function that generates Fibonacci numbers
  generator() {
    var seq = list();
    fun next() {
      var n = seq.length();
      if (n < 2) {
        seq.add(n);
      } else {
        seq.add(seq.get(n - 1) + seq.get(n - 2));
      }
      return seq.get(n);
    }
    return next;
  }
}

var next = Fib().generator();
print next(); // 0
print next(); // 1
print next(); // 1
print next(); // 2
print next(); // 3
print next(); // 5
```

## Usage

Requires [.NET 9](https://dotnet.microsoft.com/download/dotnet).

Build with `make publish` or equivalent, then:

```sh
./cslox               # start the REPL
./cslox script.lox    # run a script
./cslox -p script.lox # print the AST (works in REPL mode too)
```