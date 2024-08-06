namespace Scrapper.Lib.Domain

open System
open ImoveisScrapper
open ImoveisScrapper.ExtractorService
open ImoveisScrapper.ScrapperConfig
open Scrapper.Lib.Domain.Commands
open Scrapper.Lib.Events
open Scrapper.Lib.Utils
open Scrapper.Lib.Utils.ErrorHandling

module HtmlPageDownloader =
    let savePageToStorage (saveHtml: SavePage) (content: string) = async {        
        let! res = saveHtml content
        //printfn "Page saved to storage"
        return res
    }

    let firePageDownloadedEvent (content: string) = async {
        // Assuming you have an event handling system
        let eventData = { Id = "1"; Url = ""; SavedAt = DateTime.Now; Content = content } // ID should be generated or retrieved
        let event = PageDownloaded eventData
        // Here you would normally push the event to an event bus or process it
        printfn "Event fired: %A" event
        return ()
    }

    let processPage
        (logger:Serilog.ILogger)
        (getConfigValue:GetConfigurationValue)
        (saveHtml: SavePage)
        =
        fun url -> async {
            let args:ExtractArgs = {
                Url = url
                Headless = getConfigValue "WebEngine:Headless" |> Convert.ToBoolean
                ExecutablePath = getConfigValue "WebEngine:ExePath"
                JsCode = "" 
                SavePage = Some (savePageToStorage saveHtml)
                LoggerFactory = None
            }
            let! downloadResult = ExtractorService.downloadPage args
            match downloadResult with
            | Result.Ok content -> 
                do! firePageDownloadedEvent content
            | Result.Error errorMsg -> 
                //printfn "Error downloading page: {ex}" (errorMsg |> ErrorHandling.stringfy)
                logger.Information("Error downloading page: {ex}",(errorMsg |> ErrorHandling.stringfy))
        }
