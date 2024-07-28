module PersistJobTests

open Xunit
open ImoveisScrapper
open ImoveisScrapper.Db
open System.IO
open Scrapper.Lib.DAL.ImoveisRepository
open Scrapper.Lib.Domain.VivaRealExtractor
open Newtonsoft.Json
open Scrapper.Lib.Domain
open Scrapper.Lib.DAL
open Scrapper.Lib.Utils

[<Fact>]
let ``the persist job should read the json extraction and map it to the database`` () =
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
    let getFiles path =
        Directory.Exists(path)
        |> (fun b -> if b then Directory.GetFiles(path) else [||])
    let parsePrice value =
            match Money.parseMoneyString value with
            | Money.CurrencyAmount (currency,amount) -> amount
            | _ -> 0.0m
    let mapper (d:VivaRealCardDto):ImovelDto =
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
    let getExtractionsLoaders : Extractor.GetExtractions<ImovelDto> =
        fun _ -> 
            let jobs = 
                getFiles  "./Extractions"
                |> Seq.collect (fun extraction ->                
                        let fileContent = File.ReadAllText(extraction)
                        let vivaRealDtos = JsonConvert.DeserializeObject<VivaRealCardDto array>(fileContent)
                        let dtos = vivaRealDtos |> Seq.map Scrapper.Lib.Domain.VivaRealExtractor.mapper
                        dtos
                    )
            async {
                return jobs
            }        
                
    let persistDtos : Extractor.PersistExtractions<ImovelDto> =
        fun getExtractions -> 
            let dtos = getExtractions() |> Async.RunSynchronously
            use conn = createConnection()
            let insert = ImoveisRepository.insert conn 
            insert dtos

    PersistJob.run<ImovelDto> getExtractionsLoaders persistDtos
    |> Async.RunSynchronously    
    |> (fun r ->
        match r with
        | Ok _ -> ()
        | Error e -> 
            Assert.Fail(
                sprintf "the persistance test failed with the following exception: %s" (ErrorHandling.stringfy e)))