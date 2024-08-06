namespace Scrapper.Lib.Utils

open System
open System.Threading.Tasks
// [<Open>]
module AsyncExtensions =
    let inline (>>!=) (computation: Async<'T>) (binder: 'T -> Async<'U>) : Async<'U> =
        async.Bind(computation, binder)
        
    let inline (<!>) (computation: Async<'T>) (mapper: 'T -> 'U) : Async<'U> =
        async {
            let! result = computation
            return mapper(result)
        }

    let inline (>=>) (f: 'A -> Async<'B>) (g: 'B -> Async<'C>) (x: 'A) : Async<'C> =
        f x >>!= g

    let inline returnAsync (x: 'T) : Async<'T> =
        async.Return(x)

    let inline (>>.) (a: Async<'T>) (b: Async<'U>) : Async<'U> =
        async {
            do! a
            return! b
        }

    let inline (.*.) (a: Async<'T>) (b: Async<'U>) : Async<'T * 'U> =
        async {
            let! ra = a
            let! rb = b
            return (ra, rb)
        }
module AsyncResult =

    let inline bind (computation: Async<Result<'T, 'E>>) (binder: 'T -> Async<Result<'U, 'E>>) : Async<Result<'U, 'E>> =
        async {
            let! result = computation
            match result with
            | Ok value -> return! binder value
            | Error e -> return Error e
        }
        
    let inline map (computation: Async<Result<'T, 'E>>) (mapper: 'T -> 'U) : Async<Result<'U, 'E>> =
        async {
            let! result = computation
            return match result with
                   | Ok value -> Ok (mapper value)
                   | Error e -> Error e
        }

    let inline (>>=) (computation: Async<Result<'T, 'E>>) (binder: 'T -> Async<Result<'U, 'E>>) : Async<Result<'U, 'E>> =
        bind computation binder
        
    let inline (<!!>) (computation: Async<Result<'T, 'E>>) (mapper: 'T -> 'U) : Async<Result<'U, 'E>> =
        map computation mapper

    let inline returnAsyncResult (x: 'T) : Async<Result<'T, 'E>> =
        async.Return(Ok x)

    let inline returnErrorAsyncResult (e: 'E) : Async<Result<'T, 'E>> =
        async.Return(Error e)
    
    let inline sequence (comps: Async<Result<'T, 'E>> seq) : Async<Result<'T list, 'E>> =
        async {
            let! results = comps |> Async.Parallel
            return 
                results 
                |> Array.fold (fun state result ->
                    match state, result with
                    | Ok xs, Ok x -> Ok (x::xs)
                    | Error e, _ -> Error e
                    | _, Error e -> Error e) (Ok [])
                |> Result.map List.rev
        }        
module ErrorHandling =
    exception ExtractionNotFoundException of string
    exception DownloadFailedException of string
    type AppError =
    | Unknown of exn
    | ExtractionNotFound of string
    | DownloadFailed of string
    | DatabaseFailure of exn              
    let unwrap error =
        match error with
        | DatabaseFailure e -> e
        | Unknown e -> e
        | ExtractionNotFound extractionId -> ExtractionNotFoundException extractionId
    let stringfy error =
        match error with
        | DatabaseFailure e -> string e
        | ExtractionNotFound extractionId -> sprintf "Extraction = { Id = %s} not found while processing trying to process it" extractionId 
        | Unknown e -> string e 
    // type DatabaseResult =
    // | Updated of int
    // | Inserted of int 
    
