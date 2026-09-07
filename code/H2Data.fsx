module H2Data

open System
open System.Globalization
open System.IO
open System.Numerics
open System.Security.Cryptography
open System.Text
open System.Text.Json

let expectedGrid = [|0.30;0.40;0.50;0.60;0.70;0.74;0.80;0.90;1.00;1.20;1.40;1.60;1.80;2.00;2.50;3.00;4.00;5.00|]
let private require condition message = if not condition then failwith message
let private finite (value: float) =
    require (Double.IsFinite value) "Nonfinite H2 data"
    value
let private hash (bytes: byte[]) = SHA256.HashData bytes |> Convert.ToHexString |> fun s -> s.ToLowerInvariant()
let fileHash path = File.ReadAllBytes path |> hash
let private text (element: JsonElement) (key: string) = element.GetProperty(key).GetString()
let private number (element: JsonElement) (key: string) = element.GetProperty(key).GetDouble() |> finite

let rec private validateJson (element: JsonElement) =
    match element.ValueKind with
    | JsonValueKind.Object ->
        let entries = element.EnumerateObject() |> Seq.toArray
        require ((entries |> Array.map _.Name |> Array.distinct).Length = entries.Length) "Duplicate JSON key"
        entries |> Array.iter (fun p -> validateJson p.Value)
    | JsonValueKind.Array -> element.EnumerateArray() |> Seq.iter validateJson
    | JsonValueKind.Number -> element.GetDouble() |> finite |> ignore
    | _ -> ()

let private read path =
    use document = JsonDocument.Parse(File.ReadAllText path)
    validateJson document.RootElement
    document.RootElement.Clone()

let rec private checkShape dimensions (element: JsonElement) =
    match dimensions with
    | [] -> element.GetDouble() |> finite |> ignore
    | width :: rest ->
        require (element.ValueKind = JsonValueKind.Array && element.GetArrayLength() = width) "Raw provenance tensor shape mismatch"
        element.EnumerateArray() |> Seq.iter (checkShape rest)

let private integralMap (element: JsonElement) =
    element.EnumerateObject()
    |> Seq.map (fun p ->
        let indices = p.Name.Split(',') |> Array.map (fun s -> Int32.Parse(s, CultureInfo.InvariantCulture))
        require ((indices.Length = 2 || indices.Length = 4) && indices |> Array.forall (fun i -> i >= 0 && i < 4)) "Invalid spin-integral key"
        p.Name, Complex(p.Value.GetDouble() |> finite, 0.0))
    |> Map.ofSeq

let private semanticHash (element: JsonElement) =
    // The producer's sorted compact JSON numeric tokens bind stored tensor bytes,
    // independently of any later tolerance-based chemistry recomputation.
    element.EnumerateObject()
    |> Seq.sortWith (fun a b -> StringComparer.Ordinal.Compare(a.Name, b.Name))
    |> Seq.map (fun p -> sprintf "\"%s\":%s" p.Name (p.Value.GetRawText()))
    |> String.concat ","
    |> fun s -> Encoding.UTF8.GetBytes("{" + s + "}") |> hash

let private sourceFields =
    [ "source_repo", "johnazariah/encodings-research"
      "source_commit", "66ebdfe255c0cc6ba25a6d1b76b58401aee3ab06"
      "source_path", "papers/results/h2_sto3g/physicist_spin_integrals.json"
      "file_sha256", "6539afb30a1c03ec89202a2960a06c6580a91afaebf13a6cadbcfd32c2d71812"
      "git_blob_sha1", "e0477e70c0dfd35b865000bb23b7b31882b062d3" ]

let loadScan path =
    let directory = Path.GetDirectoryName(Path.GetFullPath path)
    let canonicalPath = Path.Combine(directory, "physicist_spin_integrals.json")
    let source = read (Path.Combine(directory, "physicist_spin_integrals.provenance.json"))
    let root = read path
    let metadata = root.GetProperty("_metadata")
    let canonicalMetadata = metadata.GetProperty("canonical_fixture")
    for field, expected in sourceFields do
        require (text source field = expected && text canonicalMetadata field = expected) ("Canonical provenance mismatch: " + field)
    require (fileHash canonicalPath = text source "file_sha256") "Canonical fixture SHA-256 mismatch"
    for field, expected in
        [ "molecule", "H2"
          "basis", "sto-3g (PySCF built-in basis data)"
          "spin_orbital_order", "interleaved: spatial p -> 2p alpha, 2p+1 beta"
          "two_body_convention", "<pq|rs>_physicist = (pr|qs)_chemist"
          "hamiltonian_contract", "0.5 sum_pqrs <pq|rs> a^p a^q a_s a_r; Vnn separate" ] do
        require (text metadata field = expected) ("Unexpected H2 metadata: " + field)
    let grid = metadata.GetProperty("bond_lengths_angstrom").EnumerateArray() |> Seq.map _.GetDouble() |> Seq.toArray
    require (grid = expectedGrid) "Unexpected declared H2 geometry grid"
    let expectedKeys = expectedGrid |> Array.map (fun r -> r.ToString("F2", CultureInfo.InvariantCulture)) |> Set.ofArray
    let actualKeys = root.EnumerateObject() |> Seq.map _.Name |> Seq.filter ((<>) "_metadata") |> Set.ofSeq
    require (actualKeys = expectedKeys) "Missing or unexpected H2 geometry records"
    let canonical = read canonicalPath
    let canonicalMap =
        [ for entry in canonical.GetProperty("one_body_spin").EnumerateArray() do
            yield sprintf "%d,%d" (entry.GetProperty("p").GetInt32()) (entry.GetProperty("q").GetInt32()), Complex(number entry "value",0.0)
          for entry in canonical.GetProperty("two_body_spin_physicist").EnumerateArray() do
            yield sprintf "%d,%d,%d,%d" (entry.GetProperty("p").GetInt32()) (entry.GetProperty("q").GetInt32()) (entry.GetProperty("r").GetInt32()) (entry.GetProperty("s").GetInt32()), Complex(number entry "value",0.0) ]
        |> Map.ofList
    require (canonicalMap.Count = 36) "Canonical entry count mismatch"
    let fixture = read (Path.Combine(directory, "h2_0.74_fixture.json"))
    require (JsonElement.DeepEquals(fixture.GetProperty("_metadata"),metadata)) "Standalone fixture/scan metadata mismatch"
    let threshold = number metadata "input_threshold"
    require (threshold > 0.0 && threshold <= 1e-12) "Unexpected integral threshold"
    let records =
        expectedGrid |> Array.map (fun r ->
            let record = root.GetProperty(r.ToString("F2", CultureInfo.InvariantCulture))
            require (number record "bond_length_angstrom" = r) "Geometry key/value mismatch"
            let vnn = number record "Vnn"
            require (abs (vnn*r - 0.52917721092) < 1e-10) "Nuclear repulsion/geometry mismatch"
            number record "E_HF" |> ignore
            let integrals = record.GetProperty("integrals")
            let map = integralMap integrals
            require (map.Count > 0) "Empty integral map"
            let provenance = record.GetProperty("provenance")
            for field, shape in [
                "rhf_mo_coefficients",[2;2]; "rhf_mo_energies_Ha",[2]
                "spatial_one_body_mo_Ha",[2;2]; "spatial_eri_chemist_mo_Ha",[2;2;2;2] ] do
                checkShape shape (provenance.GetProperty field)
            require (semanticHash integrals = text provenance "spin_integrals_sha256") "Spin-integral checksum mismatch"
            let one = provenance.GetProperty("spatial_one_body_mo_Ha")
            let two = provenance.GetProperty("spatial_eri_chemist_mo_Ha")
            for p in 0 .. 3 do
                for q in 0 .. 3 do
                    let expected = if p%2 = q%2 then one.[p/2].[q/2].GetDouble() else 0.0
                    let actual = map |> Map.tryFind (sprintf "%d,%d" p q) |> Option.defaultValue Complex.Zero
                    require (Complex.Abs(actual - Complex(expected,0.0)) < 5e-10) "One-body provenance/tensor mismatch"
                    for s in 0 .. 3 do
                        for t in 0 .. 3 do
                            let expected = if p%2 = s%2 && q%2 = t%2 then two.[p/2].[s/2].[q/2].[t/2].GetDouble() else 0.0
                            let actual = map |> Map.tryFind (sprintf "%d,%d,%d,%d" p q s t) |> Option.defaultValue Complex.Zero
                            require (Complex.Abs(actual - Complex(expected,0.0)) < 5e-10) "Two-body provenance/tensor mismatch"
            if r = 0.74 then
                require (map = canonicalMap) "Equilibrium tensor differs from immutable canonical fixture"
                require (semanticHash integrals = text canonicalMetadata "semantic_map_sha256") "Canonical semantic hash mismatch"
                require ((integralMap (fixture.GetProperty("0.74").GetProperty("integrals"))) = map) "Standalone fixture/scan tensor mismatch"
                require (JsonElement.DeepEquals(fixture.GetProperty("0.74").GetProperty("provenance"),provenance)) "Standalone fixture/scan raw provenance mismatch"
                for field in ["Vnn"; "E_HF"; "bond_length_angstrom"] do
                    require (number (fixture.GetProperty("0.74")) field = number record field) ("Standalone fixture/scan mismatch: " + field)
            r, vnn, map)
    root, records
