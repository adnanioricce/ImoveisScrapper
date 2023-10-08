open System.IO
open System
open System.Text
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
