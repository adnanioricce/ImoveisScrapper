module PersistJobTests

open Xunit
open ImoveisScrapper
open ImoveisScrapper.Db
open System.IO
open ImoveisScrapper.VivaRealExtractor

[<Fact>]
let ``the persist job should read the json extraction and map it to the database`` () =
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
    let getFiles path =
        Directory.Exists(path)
        |> (fun b -> if b then Directory.GetFiles(path) else [||])
    let parsePrice value =
            match Money.parseMoneyString value with
            | Money.CurrencyAmount (currency,amount) -> amount
            | _ -> 0.0m
    let mapper (d:VivaRealCardDto):Imoveis.ImovelDto =
        let parseInt (value:string) = 
            match System.Int32.TryParse(value) with
            | true,parsedValue -> parsedValue
            | false,_ -> 0
        { 
            QuantidadeBanheiros = parseInt d.banheiros
            QuantidadeQuartos = parseInt (string d.quartos)
            QuantidadeVagas = parseInt (string d.vagas)
            Preco = parsePrice d.price
            Titulo = d.title
            Status = d.status
            Endereco = d.address
            Adicionais = d.amenities |> String.concat ";"
            Images = d.images
        }
    let conn = Database.createConnectionWith Env.connectionString
    let createConnection () = conn
    let getExtractionsLoaders : unit -> PersistJob.GetExtractions seq = 
        fun _ -> PersistJob.getExtractions mapper (fun _ -> getFiles  "./Extractions")
    let persistDtos (func:PersistJob.GetExtractions) =                      
        let dtos = func()            
        createConnection() |> Imoveis.insert dtos
    PersistJob.run getExtractionsLoaders persistDtos
    |> Array.reduce (fun lhs rhs ->
        match lhs, rhs with
        | Ok a,Ok b -> Result.Ok (a + b)
        | Ok a,Error e -> Result.Ok a
        | Error a,Ok b -> Result.Ok b
        | Error a,Error b -> Result.Error a)
    |> Result.mapError string
    |> (fun r ->
        match r with
        | Ok _ -> ()
        | Error e -> Assert.Fail(e))