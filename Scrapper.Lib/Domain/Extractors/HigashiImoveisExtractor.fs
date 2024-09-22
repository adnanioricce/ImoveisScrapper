namespace Scrapper

open System
open System.IO
open FSharp.Data
open ImoveisScrapper
open ImoveisScrapper.ExtractorService
open ImoveisScrapper.ScrapperConfig
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Microsoft.FSharp.Core
open Newtonsoft.Json
open Scrapper.Lib.DAL.ImoveisRepository
open Scrapper.Lib.Domain.Commands
open Scrapper.Lib.Events
open Scrapper.Lib.Infra.ScrapStorage
open Scrapper.Lib.Utils
open Scrapper.Lib.Utils.ErrorHandling
open Scrapper.Logger
open AsyncExtensions
open AsyncResult

module HigashiImoveisExtractor =
    open Scrapper.Lib.Infra
    
    [<CLIMutable>]
    type HigashiProperty = {
        Id: string
        Title: string
        Location: string
        Area: string
        Description: string
        Rent: string
        Reference: string
        LinkUrl: string
        Images: string array
    }
    type PropertyInfo = {
        Title: string
        Reference: string
        Location: string
        Rent: string
        CondominioFee: string
        IPTU: string
        Area: string
        Description: string                
        Photos: string list
        
    }
    let transformExtraction (json:JsonExtraction):NewImovelDto array =
        let rows = JsonConvert.DeserializeObject<HigashiProperty array>(json)
        rows
        |> Array.map (fun data -> 
        {
          Id = data.Id
          Price = ( Money.parsePrice data.Rent)
          Title = data.Title
          Description = data.Description
          Address = data.Location      
          Url = ""
          Size = data.Area
          Images = data.Images
          Features = [|
            { Name = "Reference"; Value = data.Reference }
            //{ Name = "Reference"; Value = data.Reference }
          |] 
        })        
    module Parser =
        let parseListHtml (html: string) =
            let doc = HtmlDocument.Parse(html)    
            let html = doc.Body()
            let links = html.Elements() |> Seq.filter(fun n -> n.HasClass("link_resultado"))            

            printfn "%A" (links)
            failwith "stop"
            let parseProperty (linkNode:HtmlNode) =
                let nodeOpt = linkNode.Descendants("div") |> Seq.tryHead                
                match nodeOpt with 
                | Some node -> 
                    printfn "%s" (linkNode.InnerText())
                    let getText (node:HtmlNode seq) = node |> Seq.head |> (fun n -> n.InnerText().Trim())
                    let id = node.AttributeValue("id")
                    printfn "on properties id"
                    let title = node.Descendants("h3") |> Seq.find (fun n -> n.HasClass "tipo") |> fun n -> n.InnerText().Trim()
                    printfn "on properties title"
                    let location = node.Descendants("h4") |> Seq.find (fun n -> n.HasClass "localizacao") |> fun n -> n.Descendants("span") |> getText
                    printfn "on properties location"
                    let area = node.Descendants("div") |> Seq.find (fun n -> n.HasClass "detalhe") |> fun n -> n.Descendants("span") |> getText
                    printfn "on properties area"
                    let description = node.Descendants("div") |> Seq.find (fun n -> n.HasClass "descricao") |> fun n -> n.Descendants("span") |> getText
                    printfn "on properties description"
                    let rent = node.Descendants("div") |> Seq.find (fun n -> n.HasClass "valor") |> fun n -> n.Descendants("h5") |> getText
                    printfn "on properties rent"
                    let reference = node.Descendants("div") |> Seq.find (fun n -> n.HasClass "referencia") |> fun n -> n.Descendants("span") |> getText
                    printfn "on properties reference"
                    let linkUrl = node.TryGetAttribute("href") |> Option.map(fun attr -> attr.Value()) |> Option.defaultValue ""
                    printfn "on properties linkUrl"
                    let images = 
                        node.Descendants("div")
                        |> Seq.filter (fun n -> n.HasClass "fotorama__html")
                        |> Seq.collect (fun n -> n.Descendants("div"))
                        |> Seq.filter (fun n -> n.Attributes() |> Seq.map HtmlAttribute.name |> Seq.contains "data-img")
                        |> Seq.map (fun imgNode -> imgNode.AttributeValue("data-img"))
                        |> Seq.toArray
                    
                    Some { Id = id; Title = title; Location = location; Area = area; Description = description; Rent = rent; Reference = reference;LinkUrl = linkUrl;Images = images }
                | None ->
                    None
            
            let properties =
              printfn "on properties"
              doc.Descendants("a")
              |> Seq.filter (fun n -> n.HasClass "link_resultado")
              |> Seq.map (fun node -> parseProperty node)
              |> Seq.filter (Option.isSome)
              |> Seq.map (Option.defaultValue {Id = ""; Title = ""; Location = ""; Area = ""; Description = ""; Rent = ""; Reference = "";LinkUrl = "";Images = [||]})
            
            properties
        let parseHtml (html: string) =            
            let document = HtmlDocument.Parse(html)    
            let title = document.CssSelect("h1.titulo") |> Seq.map (fun n -> n.InnerText()) |> Seq.tryHead |> Option.defaultValue ""
            let reference = document.CssSelect("div.referencia span") |> Seq.map (fun n -> n.InnerText()) |> Seq.tryHead |> Option.defaultValue ""
            let location = 
                document.CssSelect("h2.localizacao span")
                |> Seq.map (fun n -> n.InnerText())
                |> Seq.reduce (fun acc s -> acc + s)
            
            let rent = document.CssSelect("div.valor h4") |> Seq.map (fun n -> n.InnerText()) |> Seq.tryHead |> Option.defaultValue ""
            let condoFee = document.CssSelect("div.valor span") |> Seq.map (fun n -> n.InnerText()) |> Seq.tryHead |> Option.defaultValue ""
            let iptu = document.CssSelect("div.valor span") |> Seq.nth 1 |> fun n -> n.InnerText()
            let area = document.CssSelect("div.detalhe span") |> Seq.map (fun n -> n.InnerText()) |> Seq.tryHead |> Option.defaultValue ""

            let description = document.CssSelect("div.descricao_imovel div.texto") |> Seq.map (fun n -> n.InnerText()) |> Seq.tryHead |> Option.defaultValue ""
            
            let photos = 
                document.CssSelect("div.fotorama__stage__frame img.fotorama__img")
                |> Seq.map (fun n -> n.AttributeValue("src"))
                |> Seq.toList

            let r = { 
                Title = title
                Reference = reference
                Location = location
                Rent = rent
                CondominioFee = condoFee
                IPTU = iptu
                Area = area
                Description = description
                Photos = photos
            }
            r
    
    let downloadPage
        (getConfigValue: GetConfigurationValue)
        (savePage: SavePage)
        (logger:ILoggerFactory option)
        : DownloadPageCommand =
        let cmd:DownloadPageCommand = fun url -> async {
            let args:ExtractArgs = {
                Headless = (Convert.ToBoolean (getConfigValue "WebEngine:Headless"))
                Url = url
                ExecutablePath = getConfigValue "WebEngine:ExePath"
                JsCode = ""
                SavePage = Some savePage
                LoggerFactory = logger 
            }            
            let res =
                (ExtractorService.downloadPage args)
                >>= (savePage)
                <!!> PageDownloaded
            
            return! res
        }
        cmd
    
    let extractPageCmd
        (parser:HtmlPage -> JsonExtraction)        
        // (saveExtraction: 'a -> Async<Result<ScrappedDocument,AppError>>)
        (saveExtraction: Extractor.SaveExtraction<JsonExtraction,string>)
        : ExtractPageCommand =
        
        let innerFn (data:PageDownloadedData) =
            //File.WriteAllText("./out_sample.json",JsonConvert.SerializeObject(data))
            (parser data.Content)
            |> saveExtraction
            <!!> DomainEvent.PageExtracted                 
        
        innerFn
    let transformExtractionCmd
        (getExtractionById: string -> Async<JsonExtraction option>)
        (transformExtraction: JsonExtraction -> 'a)        
        (saveTransformation: 'a -> Async<Result<DomainEvent,AppError>>)
        (raiseEvent: DomainEvent -> Async<Result<unit,AppError>>)
        :TransformExtractionCommand =
        let innerFn (extractionId:string) = async {
            let! extractionOpt = getExtractionById extractionId
            let transform = 
                match extractionOpt with
                | Some data -> 
                    data
                    |> transformExtraction
                    |> saveTransformation
                    >>= raiseEvent                    
                    // <!> (fun _ -> DomainEvent.ExtractionTransformed extractionId)
                | None ->
                    async { return Result.Error (AppError.ExtractionNotFound extractionId)}
            let! res = 
                transform
                <!!> (fun _ -> DomainEvent.ExtractionTransformed extractionId)
            return res
        }
        innerFn
    let simpleExtraction 
        (listUrl:string) 
        savePage 
        saveExtraction = async {
        // let headless = (getConfigValue "WebEngine:Headless") |> Convert.ToBoolean
        printfn "scrapping page %s" listUrl
        let! listHtml = ExtractorService.downloadPageByUrl listUrl
        
        listHtml
        |> Result.bind (fun listPage -> savePage listPage)        
        |> ignore
        match listHtml with
        | Ok page ->
            File.AppendAllText("log.txt",page)
            let pagesToExtract = Parser.parseListHtml page
            printfn "pages to extract -> %A" pagesToExtract
            return 
                pagesToExtract
                |> Seq.map (fun pageExtract ->  async {
                    let pageUrl = sprintf "https://www.higashiimoveis.com.br/comprar/sp/suzano/sitio-sao-jose/casa/%s" (pageExtract.Id)
                    // printfn
                    let! propertyHtml = downloadPageByUrl pageUrl 
                    propertyHtml
                    |> Result.bind (fun page -> savePage page)        
                    |> ignore
                    return 
                        propertyHtml
                        |> Result.map (Parser.parseHtml)
                        |> Result.map (saveExtraction)                        
                })
                |> Async.Sequential
                |> Async.RunSynchronously
                |> Seq.fold (fun (oks,errs) result ->
                    match result with
                    | Ok value -> value::oks,errs
                    | Error err -> oks,err :: errs
                ) ([],[])
                |> (fun (successes,errors) ->
                    printfn "%s -> failed scraps: %A \n succeed scraps: %A" listUrl successes errors
                    {| Successes = successes;Errors = errors|}
                )
        | Error e ->
            printfn "failed to extract url %s -> %A" listUrl e
            return {|Successes = []; Errors = [e]|}
    }
    type ExtractMessage =
    | SavePage of HtmlPage
    | SaveExtraction of PropertyInfo
    | NextPage of int
    let start (getConfigValue:GetConfigurationValue) =
        let docDbUrl = getConfigValue "ConnectionStrings:DocStorage"
        let page = sprintf "https://www.higashiimoveis.com.br/comprar/pagina-%d/"                      
        let extractStorage = ExtractionStorage.create docDbUrl
        let docStorage = HtmlStorage.create docDbUrl
        let savePage (listPage:HtmlPage) =
            let res = docStorage.SaveHtml listPage |> Async.RunSynchronously
            res
        let saveScrap (prop:PropertyInfo) =
            let res = extractStorage.SaveExtraction prop |> Async.RunSynchronously
            res
        let messageProcessing pageNumber = async {
            let listUrl = page pageNumber
            // let saveScrap (scrap)
            let! result = 
                simpleExtraction 
                    listUrl
                    savePage
                    saveScrap
            return result
        }
        let rng = Random(1248413521)
        let sleep () =
            let seconds = rng.Next(5,30) |> TimeSpan.FromSeconds
            Async.Sleep(seconds)
        let agent = MailboxProcessor.Start(
            fun inbox ->
                let rec messageLoop currentPageNumber = async {                    
                    let! _ = messageProcessing currentPageNumber
                    sleep () |> Async.RunSynchronously                                            

                    return! messageLoop (currentPageNumber + 1L)
                }
                messageLoop 1L)
        agent