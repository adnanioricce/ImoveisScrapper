namespace ImoveisScrapper

open System.Text.Json
open Microsoft.Extensions.Logging

module ExtractorService =
    open PuppeteerSharp
    type ExtractArgs = {        
        Url:string
        Headless: bool
        JsCode: string
        ExecutablePath: string
        LoggerFactory: ILoggerFactory option
    }
    let extractPageWith<'a> (args:ExtractArgs) = async {
        let options = LaunchOptions(Headless = args.Headless,ExecutablePath = args.ExecutablePath,Args = [|"--no-sandbox"|])        
        printfn "Executable Path %s" options.ExecutablePath
        use! browser = Puppeteer.LaunchAsync(options,loggerFactory = null) |> Async.AwaitTask
        use! page = browser.NewPageAsync() |> Async.AwaitTask
        let! response = page.GoToAsync(args.Url) |> Async.AwaitTask
        let! json = page.EvaluateFunctionAsync<string>(args.JsCode) |> Async.AwaitTask
        let cardsDtos = JsonSerializer.Deserialize<'a>(json)
        return cardsDtos
    }
    let extractPage<'a> url jsCode = async {
        let execPath = if not (System.String.IsNullOrWhiteSpace(Env.webEngine)) then Env.webEngine else @"C:\Program Files\Google\Chrome\Application\chrome.exe"
        return! extractPageWith<'a> { Url = url;JsCode = jsCode;Headless = true; ExecutablePath = execPath;LoggerFactory = None }
    }