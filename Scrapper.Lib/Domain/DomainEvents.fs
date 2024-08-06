namespace Scrapper.Lib

open System
open System.Collections.Concurrent
open Microsoft.FSharp.Core
    
module Events =
    type ExtractionUrl = ExtractionUrl of string
    type PageContent = string
    type PageDownloadedData = {
        Id: string
        Url: string
        SavedAt: DateTime
        Content: PageContent        
    }
    type DomainEvent =
    // first we download the page...
    | PageDownloaded of PageDownloadedData
    // then we extracted the downloaded page
    | PageExtracted of string
    // and save it with info about the extraction...
    | ExtractionSaved of int
    // then, with another process, we mark the extraction for mapping to the sql storage 
    //| ExtractedPageMarkedForCleaning of ExtractionUrl
    // maybe, we already scrapped the page, but it's been updated since our last visit
    //| ExtractedPageUpdated of ExtractionUrl
    // then we parse/clean the data 
    | ExtractionTransformed of string
    // then we save the cleaned data...
    | FinalDataSaved of ExtractionUrl
    // and finally, we cache it!
    | FinalDataCached of ExtractionUrl
    
    type DomainEventHandler<'a> = 'a -> Async<unit>
    
    module EventPublisher =
        // Concurrent dictionary to store event handlers
        let private eventHandlers = ConcurrentDictionary<Type, ResizeArray<obj>>()

        /// Subscribe a handler to an event
        let subscribe<'a> (handler: DomainEventHandler<'a>) =
            let eventType = typeof<'a>
            let handlers = eventHandlers.GetOrAdd(eventType, fun _ -> ResizeArray())
            handlers.Add(handler :> obj)

        /// Publish an event to all subscribed handlers
        let publish<'a> (event: 'a) = async {
            let eventType = typeof<'a>
            match eventHandlers.TryGetValue(eventType) with
            | true, handlers ->
                let tasks = handlers |> Seq.cast<DomainEventHandler<'a>> |> Seq.map (fun handler -> handler event)
                do! Async.Parallel(tasks) |> Async.Ignore
            | _ -> ()
        }