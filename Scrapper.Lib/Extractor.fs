namespace Scrapper.Lib


module Extractor =    
    open System.Net.Http
    open Scrapper
    open ImoveisScrapper.Extractor
    open ImoveisScrapper.Base
    open Newtonsoft.Json
    open Scrapper.Lib.Utils.ErrorHandling
    let download (url:string) = async {
        use client = new HttpClient()
        return! client.GetStringAsync(url) |> Async.AwaitTask        
    }
    let extractPage 
        (content:string) 
        (saveExtraction:HigashiImoveisExtractor.PropertyInfo -> Async<Result<DatabaseId<string>,AppError>>)
        (getPropertyById:DatabaseId<string> -> Async<DatabaseId<string> * HigashiImoveisExtractor.PropertyInfo>) = async {
        let propertyData = HigashiImoveisExtractor.Parser.parseHtml content
        let! res = saveExtraction propertyData             
        return res
    }