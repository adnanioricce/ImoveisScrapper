open System        
open System.Text.Json
open System.IO
open System.Text
open ImoveisScrapper
open ImoveisScrapper.Db
open Scrapper
open Microsoft.Extensions.Configuration
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
[<EntryPoint>]
let main (argv:string array) = 
    loadEnv()
    let config = 
        ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build()
    let template index = sprintf "https://www.vivareal.com.br/venda/?pagina=%d" index        
    let getConfigValue str = config.[str]
    
    // Jobs.runExtract template 1 Jobs.NonEmpty
    let agent = HigashiImoveisExtractor.start getConfigValue
    printfn "Processing messages..."
    while true do
        Async.Sleep(TimeSpan.FromSeconds(5)) |> Async.RunSynchronously
    0