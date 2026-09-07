#r "nuget: FockMap, 0.9.0"
#load "../labs/PauliMatrix.fsx"

open System
open System.Numerics
open PauliMatrix

let rejects label action =
    let rejected = try action (); false with _ -> true
    if not rejected then failwithf "Negative control accepted: %s" label
    printfn "Rejected: %s" label

let diagonal values = Array2D.init (Array.length values) (Array.length values) (fun i j ->
    if i = j then Complex(values[i], 0.0) else Complex.Zero)
let expected = Array.sort h2ElectronicSpectrum
let split = Array.copy expected
let indices = expected |> Array.indexed |> Array.filter (fun (_, x) -> abs (x + 0.4750688487721783) < 1e-10)
split[fst indices[0]] <- split[fst indices[0]] - 1e-4
split[fst indices[1]] <- split[fst indices[1]] + 1e-4
let oldError =
    Array.map2 (fun (x: Complex) (y: Complex) -> Complex.Abs(x-y) / max 1.0 (Complex.Abs y))
        (spectralMoments (diagonal split)) (spectralMoments (diagonal expected))
    |> Array.max
if oldError >= 1e-7 then failwith "Counterexample no longer passes the old moment gate"
printfn "Old moment diagnostic accepts split degeneracy: %.3e" oldError
rejects "split degeneracy" (fun () -> assertSpectrumMatrix "split" 1e-7 expected (diagonal split))

let y = array2D [[Complex.Zero; -Complex.ImaginaryOne]; [Complex.ImaginaryOne; Complex.Zero]]
assertSpectrumMatrix "Pauli Y" 1e-12 [|-1.0; 1.0|] y
rejects "Y real-part projection" (fun () -> assertSpectrumMatrix "projection" 1e-7 [|-1.0; 1.0|] (Array2D.zeroCreate 2 2))
rejects "non-Hermitian" (fun () -> hermitianEigenvalues (array2D [[Complex.Zero; Complex.One]; [Complex.Zero; Complex.Zero]]) |> ignore)
rejects "NaN" (fun () -> hermitianEigenvalues (diagonal [|Double.NaN|]) |> ignore)
rejects "infinity" (fun () -> hermitianEigenvalues (diagonal [|Double.PositiveInfinity|]) |> ignore)
rejects "nonsquare" (fun () -> hermitianEigenvalues (Array2D.zeroCreate 2 3) |> ignore)
rejects "wrong spectrum dimension" (fun () -> assertSpectrumMatrix "shape" 1e-7 [|0.0|] y)
let mixed = Array2D.zeroCreate<Complex> 3 3
mixed[0,0] <- Complex(1e10,0.0)
mixed[1,2] <- Complex(0.0,-9e-7)
mixed[2,1] <- Complex(0.0,9e-7)
rejects "mixed scale cannot certify 1e-7 absolute accuracy" (fun () ->
    assertSpectrumMatrix "mixed" 1e-7 [|0.0;0.0;1e10|] mixed)
// With attainable accuracy, retain the small imaginary block rather than
// discarding it according to the unrelated largest diagonal entry.
mixed[0,0] <- Complex(1e3,0.0)
assertSpectrumMatrix "mixed resolvable" 1e-7 [|-9e-7;9e-7;1e3|] mixed
rejects "mixed-scale imaginary block omitted" (fun () ->
    assertSpectrumMatrix "mixed omitted" 1e-7 [|0.0;0.0;1e3|] mixed)
