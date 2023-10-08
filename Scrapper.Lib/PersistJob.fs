namespace ImoveisScrapper

open System.IO
open VivaRealExtractor
open ImoveisScrapper.Db
open System.Text.Json
open System.Data

module PersistJob = 
    type FilePath = string
    type GetExtractions = unit -> Imoveis.ImovelDto seq
    type PersistExtractions = GetExtractions -> Async<Result<int,exn>>
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
    let getExtractions mapper getFiles =
        let files = getFiles ()
        let loadDtos file = 
            JsonSerializer.Deserialize<VivaRealCardDto[]>(File.ReadAllText(file))
            |> Seq.map (fun from ->(mapper from))
        let loadExtractions =
            files
            |> Seq.map (fun file -> fun _ -> loadDtos file)
        loadExtractions
    let run getExtractionsLoaders (persistDtos:PersistExtractions) =
        getExtractionsLoaders ()
        |> Seq.map persistDtos            
        |> Async.Sequential
        |> Async.RunSynchronously    