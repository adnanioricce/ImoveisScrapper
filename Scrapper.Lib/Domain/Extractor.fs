namespace ImoveisScrapper

open System.Text.Json
open Microsoft.Extensions.Logging
open Scrapper.Lib.Utils.ErrorHandling

module ExtractorService =
    open PuppeteerSharp    
    open Scrapper.Lib.Domain.Commands
    type ExtractArgs = {
        Url:string
        Headless: bool
        JsCode: string
        ExecutablePath: string
        SavePage: SavePage option
        LoggerFactory: ILoggerFactory option
    }
    // let goToPage (args: ExtractArgs) =
    //     let innerFn (browser:IBrowser) = async {
    //         use! page = browser.NewPageAsync() |> Async.AwaitTask
    //         return! page.GoToAsync(args.Url) |> Async.AwaitTask
    //     }
    //     innerFn    
    let downloadPage (args: ExtractArgs) = async {
        try 
            let options = LaunchOptions(Headless = args.Headless,ExecutablePath = args.ExecutablePath,Args = [|"disable-gpu";"--no-sandbox"|])        
            printfn "Executable Path %s" options.ExecutablePath
            use! browser = Puppeteer.LaunchAsync(options,loggerFactory = (args.LoggerFactory |> Option.defaultValue (new Scrapper.Logger.LoggerFactory()))) |> Async.AwaitTask
            use! page = browser.NewPageAsync() |> Async.AwaitTask
            let! response = page.GoToAsync(args.Url) |> Async.AwaitTask
            let! responseText = response.TextAsync() |> Async.AwaitTask
            return Result.Ok (HtmlPage responseText)
        with
        | ex -> return Result.Error (AppError.Unknown ex)
    }
    let evaluateJsWith (args: ExtractArgs) = async {
        let saveResponse (r:IResponse) = async {            
            match args.SavePage with
            | Some savePageAction -> 
                let! responseText = r.TextAsync() |> Async.AwaitTask
                return! (savePageAction responseText)
                
                // let outfilePath = sprintf "%s/%s" targetPath (System.Guid.NewGuid().ToString())
                // File.WriteAllText(outfilePath,responseText)
            | None ->
                return Result.Error (AppError.Unknown (exn (sprintf "empty page at url %s" args.Url)))
        }
        let savePage (r:HtmlPage) = async {            
            match args.SavePage with
            | Some savePageAction ->                 
                return! (savePageAction r)
                
                // let outfilePath = sprintf "%s/%s" targetPath (System.Guid.NewGuid().ToString())
                // File.WriteAllText(outfilePath,responseText)
            | None ->
                return Result.Error (AppError.Unknown (exn (sprintf "empty page at url %s" args.Url)))
        }
        let options = LaunchOptions(Headless = args.Headless,ExecutablePath = args.ExecutablePath,Args = [|"disable-gpu";"--no-sandbox"|])        
        Scrapper.Logger.info "Executable Path {exePath}" [|options.ExecutablePath|]
        use! browser = Puppeteer.LaunchAsync(options,loggerFactory = (args.LoggerFactory |> Option.defaultValue (new Scrapper.Logger.LoggerFactory()))) |> Async.AwaitTask
        use! page = browser.NewPageAsync() |> Async.AwaitTask
        let! response = page.GoToAsync(args.Url) |> Async.AwaitTask     
        let! pageContent = page.GetContentAsync() |> Async.AwaitTask
        let! _ = savePage pageContent
        // let! _ = (saveResponse pageContent)
        // let! responseText = response.TextAsync() |> Async.AwaitTask
        Scrapper.Logger.info "Request {url} -> {responseText}" [|args.Url|]
        
        let! json = page.EvaluateFunctionAsync<string>(args.JsCode) |> Async.AwaitTask
        Scrapper.Logger.info "Scrapped Data -> {data} " [|json|]
        return json
    }
    let extractPageWith<'a> (args:ExtractArgs) = async {
        let! json = evaluateJsWith args
        let cardsDtos = JsonSerializer.Deserialize<'a>(json)
        return cardsDtos
    }
    let extractPage<'a> url jsCode = async {
        let execPath = Env.webEngine
        return! extractPageWith<'a> { Url = url;JsCode = jsCode; Headless = false; ExecutablePath = execPath; SavePage = None; LoggerFactory = Some (new Scrapper.Logger.LoggerFactory()) }
    }