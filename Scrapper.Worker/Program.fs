namespace Scrapper.Worker

open System
open System.Collections.Generic
open System.Linq
open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open ImoveisScrapper

module Program =
    let createHostBuilder args =
        Host.CreateDefaultBuilder(args)
            .ConfigureServices(fun hostContext services ->
                services.AddHostedService<ExtracterWorker>() |> ignore)

    [<EntryPoint>]
    let main args =
        let webEngine = Env.webEngine
        let webEngineExists = System.IO.File.Exists(webEngine)
        printfn "%s -> file exists? %s" webEngine (if webEngineExists then "Yes" else "No")
        createHostBuilder(args).Build().Run()
        0 // exit code