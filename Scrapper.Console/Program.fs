open System        
open System.Text.Json
open System.IO
open System.Text
open ImoveisScrapper
open ImoveisScrapper.VivaRealExtractor
open ImoveisScrapper.Db

let loadEnv () = 
    let currentEnv = 
        let value = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        if not (String.IsNullOrWhiteSpace(value)) then value else "Development"
    currentEnv.ToLower()
    |> printfn "Environment: %s"
    let dotnetEnv = 
        match currentEnv.ToLower() with
        | "development" -> "dev"
        | "staging" -> "stage"
        | "production" -> "prod"
        | _ -> "dev"

    File.ReadAllLines($"./{dotnetEnv}.env",Encoding.UTF8)
    |> Seq.iter (fun line -> 
        let name,value = line.Split("=") |> Seq.pairwise |> Seq.head
        Environment.SetEnvironmentVariable(name,value,EnvironmentVariableTarget.User))
type ExtractionState =
| NonEmpty
| Empty
let extractPage url index = async {
    let! data = VivaRealExtractor.extractImoveis url
    match data with
    | [||] ->
        return ExtractionState.Empty
    | _ -> 
        data
        |> (fun e -> JsonSerializer.Serialize(e))
        |> (fun s -> File.WriteAllText($"./Extractions/imoveis.{index}_{DateTime.Now:ddMMyyyy_hhssmmff}.json",s,UTF8Encoding.UTF8))
        return ExtractionState.NonEmpty
}
let runExtract template index state =
    let createDir () =
        if not (Directory.Exists("./Extractions")) then
            Directory.CreateDirectory("./Extractions") |> ignore
        ()
    let rng = Random()
    let getNextSleep = fun _ -> (rng.NextDouble() * 15.0) 
    let rec run index state = 
        match state with              
        | NonEmpty -> 
            let url = template index
            let result = (extractPage url index) |> Async.RunSynchronously            
            Async.Sleep(TimeSpan.FromSeconds(getNextSleep())) |> Async.RunSynchronously
            run (index + 1) result            
        | Empty ->
            ()  
    createDir() |> ignore   
    run index state
let runPersist () =    
    let getFiles path =
        Directory.Exists(path)
        |> (fun b -> if b then Directory.GetFiles(path) else [||])
    
    let createConnection () = Database.createConnection ()
    let getExtractionsLoaders : unit -> PersistJob.GetExtractions seq = 
        fun _ -> PersistJob.getExtractions PersistJob.mapper (fun _ -> getFiles  "./Extractions")
    let persistDtos (func:PersistJob.GetExtractions) =                      
        let dtos = func()            
        createConnection() |> Imoveis.insert dtos
    PersistJob.run getExtractionsLoaders persistDtos
[<EntryPoint>]
let main (argv:string array) = 
    loadEnv()
    let template index = sprintf "https://www.vivareal.com.br/venda/?pagina=%d" index        
    runExtract template 1 NonEmpty
    0