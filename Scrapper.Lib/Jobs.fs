namespace Scrapper

open ImoveisScrapper
open System.Text.Json
open System.IO
open System
open System.Text
open ImoveisScrapper.Db

module Jobs = 
    type ExtractionState =
    | NonEmpty
    | Empty
    let extractPage url = async {        
        match! VivaRealExtractor.extractImoveis url with
        | [||] ->
            return ExtractionState.Empty
        | data -> 
            use conn = Database.createConnection ()
            let! response =     
                let dtos = data |> Seq.map PersistJob.mapper
                Imoveis.insert dtos conn
            printfn "%A" response
            return ExtractionState.NonEmpty
    }
    
    let runExtract template index state =        
        let rng = Random()
        let getNextSleep = fun _ -> (rng.NextDouble() * 15.0) 
        let rec run index state = 
            match state with              
            | NonEmpty -> 
                let url = template index
                let result = (extractPage url) |> Async.RunSynchronously            
                Async.Sleep(TimeSpan.FromSeconds(getNextSleep())) |> Async.RunSynchronously
                run (index + 1) result            
            | Empty ->
                ()          
        run index state    
