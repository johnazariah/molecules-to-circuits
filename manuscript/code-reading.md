# Reading and Running the Code

The first code in this book asks a small question: given an integral's
index key, which coefficient should the Hamiltonian builder receive?
Reading that function requires a little F#, but not a course in F#.
We will follow one key all the way through it.

This bridge sits before *The Notation Minefield*, where the physical
meaning of the index permutation is derived. Here the permutation is
an explicit rule to trace. You don't need to understand two-electron
integrals yet to see whether a program followed that rule.

## Three kinds of code

A **complete script** includes its package references, module imports and
input data. Save it as the named `.fsx` file and run it with F# Interactive
(FSI). An **excerpt** uses values defined elsewhere: the surrounding text
names that dependency. Pasting an excerpt into an empty file will not
invent the missing molecule or Hamiltonian. **Pseudocode** describes an
algorithm; it is not a promise about an executable API.

The files under `labs/` and the F# entry points under `code/` are the
practical route through the repository. Their filenames are commands,
not instructions to copy every code block into one enormous script.
Appendix A also contains self-contained small programs with explicit
inputs and expected results.

## Running your first script

Install the **.NET 10 SDK**, not just the runtime. The SDK includes the
F# compiler and FSI. A terminal in the repository root should accept:

```bash
dotnet --version
dotnet fsi labs/01-first-encoding.fsx
```

The first command should report a `10.x` SDK. The second reads the lab
and executes its top-level definitions in order. A package restore may
need internet access the first time. You do not need to build a local
checkout of the FockMap library to run the NuGet-based companions.

The first result is the creation operator on mode 0, displayed as the
two strings `XIII` and `YIII`, with coefficients `0.5` and `-0.5i`.
F# may print a complex number as a pair of real and imaginary parts
rather than with an `i`. That is formatting, not another convention.
The algebra explaining those strings comes later.

At the top of that lab you will find:

```fsharp
#r "nuget: FockMap, 0.9.0"
open Encodings
open Encodings.JordanWigner
```

`#r` adds an assembly reference. This form asks NuGet for the exact package
version `0.9.0`. Keep the version: a newer package is not automatically
the package against which a worked example was written.

`open` makes names from a namespace or module available without their
full prefix. It does not download a package, load a data file or execute
a simulation. The first `open` supplies shared operator types; the
second supplies the Jordan-Wigner functions. Feature modules are opened
explicitly throughout the companions.

`#load` does something different: it loads another F# source script.
The following is a **repository-dependent excerpt**, not a standalone
file. Saved beside the Chapter 3 companion in `code/`, it can use that
script's factory:

```fsharp
#r "nuget: FockMap, 0.9.0"
#load "ch03-spin-orbitals.fsx"
open Encodings.Hamiltonian

let hamiltonian =
    computeHamiltonian
        ``Ch03-spin-orbitals``.h2RawPhysicistFactory
        4u
```

The double backticks delimit an identifier containing characters such
as a hyphen. They are part of F# syntax, not Markdown decoration.
Loading a script executes its top-level code; loading a program that
prints results may therefore print results before your own code runs.

### Which directory?

There are two paths to keep apart. The shell has a **working directory**:
`dotnet fsi code/ch18-pipeline.fsx` finds `code/` relative to it.
A source file also has a location: `#load` resolves its relative source
path from the loading script. F#'s `__SOURCE_DIRECTORY__` identifies
that source directory and is useful for finding adjacent data reliably.

Ordinary file operations such as `File.ReadAllText("input.json")` use
the process working directory unless the code constructs another path.
That is why the repository instructions start from the root and the
companions construct paths for their own inputs. Moving an excerpt to
another directory can change what it loads even when every equation
in the excerpt remains correct.

Use a new FSI process after editing a complete script. An old interactive
session can retain a definition that no longer appears in your file.
That is a particularly unhelpful kind of reproducibility.

## A coefficient is a lookup result

A **map** stores key-value pairs. Here a key is a string such as `"0,1"`;
its value is a complex coefficient. A **factory** is simply a function
which receives the key and returns a lookup result. It is not a class
you must instantiate, and it does not compute integrals by itself.

The type

```text
string -> Complex option
```

reads "a function from a string to an optional complex number".
`Some value` means the entry exists. `None` means it is absent.
The caller has to decide what absence means under the data contract.
We will make that decision explicit.

### Complete script: `read-factory.fsx`

Save the following block as `read-factory.fsx` and run
`dotnet fsi read-factory.fsx`. It only uses the .NET SDK.
The coefficients are a deliberately small **illustrative spin-orbital
table**, not a molecular fixture. All omitted valid entries are declared
zero for this example. Chapter 3 explains how real spatial integrals
become a spin-orbital table.

```fsharp
open System
open System.Numerics

let numModes = 2
let real x = Complex(x, 0.0)

let chemistIntegrals =
    Map.ofList [
        ("0,0", real -1.0)
        ("1,1", real -0.5)
        ("0,1", Complex.Zero)
        ("0,0,1,1", real 0.6)
        ("1,1,0,0", real 0.6)
    ]

let parseIndex (text: string) =
    match Int32.TryParse text with
    | true, value when value >= 0 && value < numModes -> value
    | _ -> invalidArg "key" "Expected a mode index in the declared range"

let physicistFactory (key: string) : Complex option =
    let indices = key.Split(',') |> Array.map parseIndex
    match indices with
    | [| p; q |] ->
        Map.tryFind (sprintf "%d,%d" p q) chemistIntegrals
    | [| p; q; r; s |] ->
        Map.tryFind (sprintf "%d,%d,%d,%d" p r q s) chemistIntegrals
    | _ -> invalidArg "key" "Expected two or four comma-separated indices"

for key in ["0,1,0,1"; "0,1"; "1,0"] do
    match physicistFactory key with
    | Some coefficient ->
        printfn "%s -> present: real=%g imaginary=%g"
            key coefficient.Real coefficient.Imaginary
    | None -> printfn "%s -> absent (declared zero in this toy table)" key
```

Expected numerical output:

```text
0,1,0,1 -> present: real=0.6 imaginary=0
0,1 -> present: real=0 imaginary=0
1,0 -> absent (declared zero in this toy table)
```

## One key through the coefficient factory

Take the input `"0,1,0,1"`. `key.Split(',')` produces an array of
four strings: `[| "0"; "1"; "0"; "1" |]`. The vertical bars distinguish
an F# array from a list. `Array.map parseIndex` applies `parseIndex`
to each element and produces `[| 0; 1; 0; 1 |]`.

The `match` chooses a branch by the array's shape. The four-element
pattern `[| p; q; r; s |]` binds `p=0`, `q=1`, `r=0`, `s=1`.
`sprintf` then constructs the key with the middle two positions exchanged:

```text
input order:    p q r s = 0 1 0 1
lookup order:   p r q s = 0 0 1 1
stored key:    "0,0,1,1"
lookup result: Some (Complex(0.6, 0.0))
```

Nothing in that trace multiplies the coefficient by one half. The
factory changes the index convention; the primary Hamiltonian builder
applies the Hamiltonian's two-body prefactor internally. Those are
different operations. The following chapter explains why both are
needed and why applying the prefactor twice gives the wrong operator.

The two-element pattern keeps a one-body key unchanged. A three-element
key reaches the final branch and raises an error. An out-of-range index
or a word such as `"two"` fails in `parseIndex` before any lookup occurs.
Malformed input is not silently converted into a coefficient of zero.

## Missing is not zero

`physicistFactory "0,1"` returns `Some Complex.Zero`: the map contains
that key and its value is zero. `physicistFactory "1,0"` returns `None`:
the key is not stored. The distinction survives the lookup.

For this toy sparse table we have explicitly declared omitted valid
entries to be zero. FockMap's Hamiltonian builders skip a contribution
when their factory returns `None`. That is useful for a validated sparse
table. It does **not** establish that an absent record in an incomplete
JSON file is physically zero. The input loader must establish the schema,
index ranges, convention and completeness policy before the factory
hands entries to the builder.

Likewise, `Option.defaultValue Complex.Zero` would turn absence into a
number. It is sometimes an appropriate final interpretation of a
validated sparse table, but it is not validation. Don't put it at the
file boundary to make an error disappear.

Presence also matters for a sparse Hamiltonian skeleton. A key present
with a zero coefficient can reserve structure that becomes nonzero at
another geometry; a key absent when the structure is discovered may
not be represented. Appendix A returns to that practical distinction.

## Reading the remaining punctuation

`let numModes = 2` binds a name to a value. `let real x = ...` binds a
name to a function: `real -1.0` calls it with one argument. The type
annotation `(key: string)` states what the argument is; `: Complex option`
states what comes back. Neither annotation changes the numerical value.

F# uses spaces for ordinary function application. In
`Map.tryFind key chemistIntegrals`, the function takes a key and then a
map. This is a **curried** function: supplying only the key produces a
function still waiting for its map. Parentheses in
`("0,0", real -1.0)` instead create a **tuple**, one value with two
components. The list passed to `Map.ofList` contains such key-value tuples.

The pipeline operator `|>` sends its left-hand value to the final
argument on its right. These expressions have the same meaning:

```fsharp
Map.tryFind "0,0" chemistIntegrals
chemistIntegrals |> Map.tryFind "0,0"
```

Similarly, `key.Split(',') |> Array.map parseIndex` reads from left to
right without changing the mapping operation. A pipeline does not
necessarily mean concurrency, mutation or quantum time evolution.
It is a way to write function application.

Array indexing begins at zero. `indices.[0]` retrieves the first
element; it does not check that an element exists. The patterns in our
factory check the shape and bind the elements together, avoiding a
series of unchecked indexed reads. `Int32.TryParse` returns a tuple
`(success, value)`; the pattern `true, value when ...` accepts only
successful parses whose value passes the range guard.

In the printing loop, `for key in [...] do` visits each key in the list.
The `| Some coefficient ->` branch binds the coefficient for use in
that branch. The underscore in `| _ ->` is a wildcard: it matches a
case without giving it a name. `printfn` formats a line; `%s` expects
a string and `%g` a floating-point number.

## From the lookup to the library

The same language ideas explain the following **contextual excerpt**,
using the factory defined in the complete script:

```fsharp
#r "nuget: FockMap, 0.9.0"
open Encodings.Hamiltonian
open Encodings.JordanWigner

let hamiltonian =
    computeHamiltonianWith jordanWignerTerms physicistFactory 2u
```

`computeHamiltonianWith` receives an encoding function, a coefficient
factory and a mode count. A function can be passed as an argument just
like a number. The builder calls the factory with the keys it needs.
`2u` is an unsigned 32-bit integer, the type this API uses for mode
counts. It means two modes, not two electrons or two electron pairs.
An index passed to the encoder must lie between `0u` and `n-1u`.
Check a signed input before converting it to `uint32`; a type conversion
is not a range check.

Later examples contain records such as `{ options with Precision = 12 }`.
This creates a new record by copying the named fields from `options`
and replacing `Precision`. It is not a mutation of every previous export.
Tree recursion and physical-sector option records receive their own
local explanations when the chapters first use them.

### When execution stops

An unresolved name usually means a missing `open`, a missing `#load`,
or an excerpt being run without its definitions. A package-restore error
is not a failed physics calculation: first establish access to the pinned
package. A missing file is a path or input problem; do not manufacture a
zero-filled file to get past it. A failed scientific assertion needs the
convention and input checked, not the assertion removed.

You now have enough F# to follow the first coefficient factory. The
important question is no longer what the punctuation means. It is
whether the key and coefficient describe the operator we intended.
