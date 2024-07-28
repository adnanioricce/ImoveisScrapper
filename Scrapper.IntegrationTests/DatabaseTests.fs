module Tests

open System
open Xunit
open ImoveisScrapper.Db
open Scrapper.Lib.DAL.ImoveisRepository
open Scrapper.Lib.DAL
//[<Fact>]
//let ``database schema can be created `` () =
//    let connStr = "Data Source=:memory:"
//    use conn = Database.createConnectionWith connStr
//    match Database.createDatabase conn with
//    | Ok _ -> ()
//    | Error message -> Assert.Fail(message)
[<Fact>]
let ``can insert data``() =
    let dto:ImovelDto = {
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
    ImoveisRepository.insert conn [dto] |> Async.RunSynchronously
    |> Result.map (fun r -> if r >= 0 then "Data inserted with success!" else "Data insertion failed!")
    |> Result.mapError (fun e -> sprintf "Exception throwed when inserting data %A" e)
    |> (fun r -> 
        match (r) with
        | Ok _ -> ()
        | Error e -> Assert.Fail(e))
[<Fact>]
let ``can query data``() =
    let dto:ImovelDto = {
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
    get conn 0 10 |> Async.RunSynchronously
    |> Result.map (fun ls -> if ls |> Seq.isEmpty |> not then "Query returned data with success!" else "Query didn't return any data!")
    |> Result.mapError (fun exn -> sprintf "%A" exn)
    |> (fun r -> 
        match (r) with
        | Ok _ -> ()
        | Error e -> Assert.Fail(e))