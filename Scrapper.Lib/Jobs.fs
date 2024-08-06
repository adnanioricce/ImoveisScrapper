namespace Scrapper

open ImoveisScrapper
open System.Text.Json
open System.IO
open System
open System.Text
open ImoveisScrapper.Db
open Scrapper.Lib.DAL
open Scrapper.Lib.Domain

module Jobs = 
    type ExtractionState =
    | NonEmpty
    | Empty
    let extractPageWith
        (extractPage: string -> Async<'dto array>)
        (saveExtraction:'dto array -> Async<unit>)
        url = async {
        match! extractPage url with
        | [||] ->
            return ExtractionState.Empty
        | data ->
            let! res = saveExtraction data
            return ExtractionState.NonEmpty
    }
    let extractPage url = async {
        let saveData data = async {
            use conn = Database.createConnection ()
            let! response =
                let dtos = data |> Seq.map VivaRealExtractor.mapper
                ImoveisRepository.insert conn dtos 
            printfn "%A" response
        }
        return! extractPageWith VivaRealExtractor.extractImoveis saveData url        
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
