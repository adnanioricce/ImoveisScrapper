namespace Scrapper.Web

open System
open System.Collections.Generic
open System.Linq
open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

module Program =
    open System
    open System.Net.Http
    open FSharp.Data
    open Microsoft.Playwright

    // Define an async function to scrape a webpage using Playwright
    let scrapeWebsite (url: string) =
        async {
            // Initialize Playwright and launch a browser
            let! playwright = Playwright.CreateAsync() |> Async.AwaitTask
            let! browser = playwright.Chromium.LaunchAsync(BrowserTypeLaunchOptions(Headless = true)) |> Async.AwaitTask
            let! page = browser.NewPageAsync() |> Async.AwaitTask
            
            // Navigate to the URL
            let! _ = page.GotoAsync(url) |> Async.AwaitTask
            
            // Example: Extract all links
            let! links = page.QuerySelectorAllAsync("a") |> Async.AwaitTask
            
            // Iterate through the links and extract href attributes
            let linkResults =
                links
                |> Seq.map (fun elem -> elem.GetAttributeAsync("href") |> Async.AwaitTask)
                |> Async.Parallel
                |> Async.RunSynchronously                
            
            // Close the browser
            do! browser.CloseAsync() |> Async.AwaitTask
            
            return linkResults
        }
        
    let run argv =        
        let url = "https://www.redimoveis.com.br/imoveis/a-venda"
    
        // Run the scraper
        let links = scrapeWebsite url |> Async.RunSynchronously
        
        // Print the extracted links
        links |> Array.iter (fun link -> printfn "Found link: %s" link)
        
        0 // return an integer exit code


    [<EntryPoint>]
    let main args =
        run args         
        // let builder = Host.CreateApplicationBuilder(args)
        // builder.Services.AddHostedService<Worker>() |> ignore

        // builder.Build().Run()

        //0 // exit code