namespace Scrapper.Web

open System
open System.Collections.Generic
open System.Linq
open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
module Play =
  open Microsoft.Playwright
  let playwright = Playwright.CreateAsync() |> Async.AwaitTask |> Async.RunSynchronously
  let openBrowser () =
    playwright.Chromium.LaunchAsync(BrowserTypeLaunchOptions(Headless = true))
    |> Async.AwaitTask
    |> Async.RunSynchronously
  let closeBrowser (browser) = async {
    do! browser.CloseAsync() |> Async.AwaitTask
  }
module Lopes =
  open Microsoft.Playwright
  type LopesListing = {
      Link: string
      ImageUrl: string
      ImageAlt: string
      Location: string
      // Area: string
      Price: string
      Description: string
  }
  let scrapPages (page:IPage) = async {
    let! pageLinkNodes = page.QuerySelectorAllAsync(".page-link") |> Async.AwaitTask
    let! links = 
        pageLinkNodes 
        |> Seq.map (fun pageLink -> pageLink.GetAttributeAsync("href") |> Async.AwaitTask)
        |> Async.Sequential    
    return links
  }
  let scrapCards (page:IPage) url = async {            
      let queryAndEvaluate selector expr (el:IElementHandle) = async {
        let! elements = el.QuerySelectorAsync(selector) |> Async.AwaitTask
        return! elements.EvaluateAsync<string>(expr) |> Async.AwaitTask
      }
      let queryInnerText selector (el:IElementHandle) = async {
        let! priceNode = el.QuerySelectorAsync(selector) |> Async.AwaitTask
        return! priceNode.InnerTextAsync() |> Async.AwaitTask
      }

        // cardElement.QuerySelectorAsync("a").EvaluateAsync<string>("el => el.href") |> Async.AwaitTask
      let! cardElements = page.QuerySelectorAllAsync("li.cardlist__item") |> Async.AwaitTask
      let! data = 
        cardElements 
        |> Seq.map(fun cardElement -> async {            
            let! url = cardElement |> queryAndEvaluate "a" "el => el.href"
            let! imageUrl = cardElement |> queryAndEvaluate "img" "el => el.src"
            let! price = cardElement |> queryInnerText ".card__price"
            let! location = cardElement |> queryInnerText ".card__location"
            let! description = cardElement |> queryInnerText ".card__description"

            // Return the extracted details as a record
            return {
                Link = url
                ImageUrl = imageUrl
                ImageAlt = ""
                Location = location
                // Area = area
                Price = price
                Description = description
            }
        })
        |> Async.Parallel        
      return data
    }

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

    let run argv = async {
      // let url = "https://www.redimoveis.com.br/imoveis/a-venda"
      //let template = sprintf "https://vic.lopes.com.br/busca/venda/br/sp/suzano/pagina/2?estagio=real_estate_parent&estagio=real_estate&placeId=ChIJCUPPpXZxzpQR4uZoo3byeHY&companyId=569"
      let template = sprintf "https://vic.lopes.com.br/busca/venda/br/sp/suzano/pagina/%d"
      let url = template 1
      let browser = Play.openBrowser()
      
      // Initialize Playwright and launch a browser      
      let! page = browser.NewPageAsync() |> Async.AwaitTask
      // Navigate to the URL where the <a> with the details is located
      let! _ = page.GotoAsync(url) |> Async.AwaitTask
      let! links = Lopes.scrapPages page
      let! cards = Lopes.scrapCards page url
      // Run the scraper
      return yield! cards
      // let links = scrapeWebsite url |> Async.RunSynchronously

      // Print the extracted links
      for linkUrl in links do
        
        |> Array.map (fun link -> Lopes.scrapCards link)        
        |> Array.iter (fun link -> printfn "Found link: %s" link)
      Play.closeBrowser browser |> Async.RunSynchronously
      0 // return an integer exit code
    }

    [<EntryPoint>]
    let main args =
        run args
        // let builder = Host.CreateApplicationBuilder(args)
        // builder.Services.AddHostedService<Worker>() |> ignore

        // builder.Build().Run()

        //0 // exit code