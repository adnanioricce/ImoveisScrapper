namespace Scrapper.Lib.Infra

open System
open ImoveisScrapper
open ImoveisScrapper.Extractor
open MongoDB.Bson
open MongoDB.Driver
open MongoDB.Driver.Linq
open Scrapper.Lib.Domain.Commands
open Scrapper.Lib.Events
open Scrapper.Lib.Utils.ErrorHandling

module HtmlStorage =
    // [<CLIMutable>]
    // type RawDocument<'a> = {
    //     Id: BsonObjectId
    //     SavedAt: DateTime
    //     Content: 'a        
    // }
    [<CLIMutable>]
    type HtmlDocument = RawDocument<BsonObjectId,string>
    type LoadHtml<'a> = string -> Async<'a option>
    type StandardHtmlStorage(client: IMongoClient) =
        let database = client.GetDatabase("imoveis")
        let htmlCollection = database.GetCollection<HtmlDocument>("htmlDocuments")
        member this.SaveHtml: SavePage = fun (content: HtmlPage) -> async {
            try 
                let htmlDoc:HtmlDocument = { Id = BsonObjectId(ObjectId.GenerateNewId()); SavedAt = DateTime.Now; Content = content }
                do! htmlCollection.InsertOneAsync(htmlDoc) |> Async.AwaitTask
                let dto:PageDownloadedData = { Id = htmlDoc.Id.ToString(); Url = ""; SavedAt = DateTime.Now; Content = htmlDoc.Content }
                return Ok dto
            with
            | ex -> return Error (AppError.DatabaseFailure ex)
        }
        member this.LoadHtml(id: ObjectId) = async {        
            let pred = fun (doc:HtmlDocument) -> doc.Id
            let filter = Builders<HtmlDocument>.Filter.Eq(pred, id)
            use! cursor = htmlCollection.FindAsync(filter) |> Async.AwaitTask
            match cursor.Any() with
            | true -> 
                let! result = cursor.FirstOrDefaultAsync() |> Async.AwaitTask
                return Some result
            | false ->
                return None
        }
    let create (url:string) =
        let settings = MongoClientSettings.FromConnectionString(url)
        settings.LinqProvider <- LinqProvider.V3
        let client = MongoClient(settings)
        StandardHtmlStorage(client)
module ScrapStorage =    
    [<CLIMutable>]
    type ScrappedDocument = {
        Id: BsonObjectId
        SavedAt: DateTime
        Content: string        
    }
    type StandardScrapStorage(client:IMongoClient) =
        let database = client.GetDatabase("imoveis")
        let propertyCollection = database.GetCollection<ScrappedDocument>("properties")
        member this.SaveScrap: SaveExtraction<JsonExtraction,string> =
            fun (content) -> async {
                try
                    let doc = { Id = BsonObjectId(ObjectId.GenerateNewId());Content = content; SavedAt = DateTime.Now }
                    do! propertyCollection.InsertOneAsync(doc) |> Async.AwaitTask
                    Scrapper.Logger.info "Property data saved to MongoDB" [||] |> ignore
                    return Result.Ok (doc.Id.ToString())
                with
                | ex -> return Result.Error (AppError.DatabaseFailure ex)
            }
        member this.LoadProperty(id: string) = async {            
            let objId = ObjectId(id)
            let bsonId = BsonObjectId(objId)
            let filter = Builders<ScrappedDocument>.Filter.Eq("_id", bsonId)
            let! cursor = propertyCollection.FindAsync(filter) |> Async.AwaitTask
            let! result = cursor.FirstOrDefaultAsync() |> Async.AwaitTask
            return Some result
        }
    //let private client = new MongoClient("mongodb://localhost:27017")
    let create (url:string) = StandardScrapStorage(new MongoClient(url))