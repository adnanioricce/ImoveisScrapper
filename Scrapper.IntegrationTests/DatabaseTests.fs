module Tests

open System
open Xunit
open ImoveisScrapper.Db
//[<Fact>]
//let ``database schema can be created `` () =
//    let connStr = "Data Source=:memory:"
//    use conn = Database.createConnectionWith connStr
//    match Database.createDatabase conn with
//    | Ok _ -> ()
//    | Error message -> Assert.Fail(message)
[<Fact>]
let ``can insert data``() =
    let dto:Imoveis.ImovelDto = {
        QuantidadeBanheiros = 0
        QuantidadeQuartos = 0
        QuantidadeVagas = 0
        Preco = 0.0m
        Titulo = ""
        Endereco = ""
        Adicionais = ""
        Status = ""
        Images = [|""|]
    }
    use conn = Database.createConnectionWith Env.connectionString
    conn |> Imoveis.insert [dto] |> Async.RunSynchronously        
    |> Result.map (fun r -> if r >= 0 then "Data inserted with success!" else "Data insertion failed!")
    |> Result.mapError (fun e -> sprintf "Exception throwed when inserting data %A" e)
    |> (fun r -> 
        match (r) with
        | Ok _ -> ()
        | Error e -> Assert.Fail(e))
[<Fact>]
let ``can query data``() =
    let dto:Imoveis.ImovelDto = {
        QuantidadeBanheiros = 0
        QuantidadeQuartos = 0
        QuantidadeVagas = 0
        Preco = 0.0m
        Titulo = ""
        Endereco = ""
        Adicionais = ""
        Status = ""
        Images = [|""|]
    }
    use conn = Database.createConnectionWith Env.connectionString
    Imoveis.get 0 10 conn |> Async.RunSynchronously    
    |> Result.map (fun ls -> if ls |> Seq.isEmpty |> not then "Query returned data with success!" else "Query didn't return any data!")
    |> Result.mapError (fun exn -> sprintf "%A" exn)
    |> (fun r -> 
        match (r) with
        | Ok _ -> ()
        | Error e -> Assert.Fail(e))