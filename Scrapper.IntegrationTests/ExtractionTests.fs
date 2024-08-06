module ExtractionTests


open System
open ImoveisScrapper.ScrapperConfig
open Scrapper.HigashiImoveisExtractor
open Scrapper.Lib.Domain.Commands
open Scrapper.Lib.Events
open Scrapper.Lib.Infra.ScrapStorage
open Scrapper.Lib.Utils.ErrorHandling
open Xunit
open ImoveisScrapper
open ImoveisScrapper.Db
open System.IO
open Scrapper.Lib.DAL.ImoveisRepository
open Scrapper.Lib.Domain.VivaRealExtractor
open Newtonsoft.Json
open Scrapper.Lib.Domain
open Scrapper.Lib.DAL
open Scrapper.Lib.Utils
open ImoveisScrapper.ExtractorService
open Scrapper
open FSharp.Data
open AsyncExtensions
open AsyncResult
open Scrapper.Lib.Infra
let configValue: GetConfigurationValue =
      fun key ->
        match key with
        | "WebEngine:ExePath" -> "/nix/store/yw09kd2wvfd15mnllj1khxpyg4xc8c1v-chromium-126.0.6478.126/bin/chromium"
        | "WebEngine:Headless" -> "false"
        | "Mongo:ConnectionUrl" -> "mongodb://root:example@localhost:27018/"
  
let jsCode = """() => {
function parsePropertyCard(doc) {
  const propertyCard = {};

  // Extract property title
  const titleElement = doc.querySelector('.property-card__title');
  propertyCard.title = titleElement.textContent.trim();

  // Extract property address
  const addressElement = doc.querySelector('.property-card__address');
  propertyCard.address = addressElement.textContent.trim();

  // Extract property details (area, rooms, bathrooms, garages)
  const detailsElements = doc.querySelectorAll('.property-card__detail-item');
  detailsElements.forEach((detailElement) => {
    const key = detailElement.querySelector('.property-card__detail-text').textContent.trim();
    const value = detailElement.querySelector('.property-card__detail-value').textContent.trim();
    propertyCard[key.toLowerCase()] = value;
  });

  // Extract property amenities
  const amenitiesElements = doc.querySelectorAll('.amenities__item');
  propertyCard.amenities = Array.from(amenitiesElements).map((amenityElement) =>
    amenityElement.textContent.trim()
  );

  // Extract property price
  const priceElement = doc.querySelector('.property-card__price');
  propertyCard.price = priceElement.textContent.trim();

  // Extract property image URLs
  const imageElements = doc.querySelectorAll('.carousel__item-wrapper .carousel__image');
  propertyCard.images = Array.from(imageElements).map((imageElement) =>
    imageElement.getAttribute('src')
  );

  // Extract property status
  const statusElement = doc.querySelector('.property-card__inactive-listing');
  propertyCard.status = statusElement ? statusElement.textContent.trim() : 'Disponível';
  propertyCard.quartos = parseInt(propertyCard.quartos);
  propertyCard.vagas = parseInt(propertyCard.vagas);
  
  return propertyCard;
}
        return JSON.stringify(Array.from(document.querySelectorAll('.property-card__container')).map(e => parsePropertyCard(e)))
}
        """
[<Theory>]
[<InlineData("")>]
let ``the extraction is expected to receive a url and a js code string to manipulate the page and return a list of objects `` () = async {
    let! dtos = ExtractorService.extractPage<VivaRealCardDto array> "https://www.vivareal.com.br/venda/" jsCode
    Assert.NotEmpty(dtos)
}

[<Fact>]
let `` test higarashi extraction from source ``() = async {
    let url = "https://www.higashiimoveis.com.br/alugar"
    let jsCode = """
    () => {
      const listings = document.querySelectorAll('.resultado_lista');
      const results = [];

      listings.forEach(listing => {
          const id = listing.getAttribute('id');
          const images = Array.from(listing.querySelectorAll('.fotorama__stage__frame img')).map(img => img.getAttribute('src'));
          const url = listing.querySelector('a').getAttribute('href');
          const tipo = listing.querySelector('.tipo').innerText;
          const localizacao = listing.querySelector('.localizacao span').innerText;
          const detalhes = listing.querySelector('.detalhes .detalhe span').innerText;
          const descricao = listing.querySelector('.descricao span').innerText;
          const valor = listing.querySelector('.valor h5').innerText;
          const referencia = listing.querySelector('.referencia span').innerText;

          results.push({
              id,
              images,
              url,
              tipo,
              localizacao,
              detalhes,
              descricao,
              valor,
              referencia
          });
      });

      const data = JSON.stringify(results, null, 2);
      console.log('data:',data);
      return data;
    }
    """
    let savePageMock: SavePage =
      fun (response:string) -> async {
        Directory.CreateDirectory("./out") |> ignore
        File.WriteAllText(sprintf "./out/%s.json" (System.Guid.NewGuid().ToString()),response)
        let data: PageDownloadedData = {
          Id = "0"
          Url = ""
          SavedAt = DateTime.Now
          Content = response 
        }
        return Result.Ok data
      }
    let args:ExtractArgs = {
      Url = url
      JsCode = jsCode
      Headless = true
      ExecutablePath = Env.webEngine
      SavePage = Some savePageMock
      LoggerFactory = None 
    }
    let! response = ExtractorService.extractPageWith<obj array> args
    File.WriteAllText("./out.json",JsonConvert.SerializeObject(response))
  }
[<Fact>]
let `` test higarashi is downloadable ``() = async {
    let url = "https://www.higashiimoveis.com.br/alugar"
    let configValue: GetConfigurationValue =
      fun key ->
        match key with
        | "WebEngine:ExePath" -> "/nix/store/yw09kd2wvfd15mnllj1khxpyg4xc8c1v-chromium-126.0.6478.126/bin/chromium"
        | "WebEngine:Headless" -> "false"            
    let savePage: SavePage =
      fun htmlPage -> async {        
        let data: PageDownloadedData = {
          Id = "0"
          Url = ""
          SavedAt = DateTime.Now
          Content = htmlPage 
        }
        return Ok data
      }
    let cmd = HigashiImoveisExtractor.downloadPage configValue (savePage) None
    let! response = cmd url
    match response with
    | Ok _event ->
      match _event with
      | PageDownloaded data -> 
        Assert.NotEmpty(data.Content)
      | _ -> Assert.Fail("A PageDownloaded was expected to be returned from HigashiImoveisExtractor.downloadPage")
    | Error ex ->
      Assert.Fail(ErrorHandling.stringfy ex)
  }
[<Fact>]
let `` test higarashi extraction from saved html ``() = async {
    let url = "https://www.higashiimoveis.com.br/alugar"
    let jsCode = """
    () => {
      const listings = document.querySelectorAll('.resultado_lista');
      const results = [];

      listings.forEach(listing => {
          const id = listing.getAttribute('id');
          const images = Array.from(listing.querySelectorAll('.fotorama__stage__frame img')).map(img => img.getAttribute('src'));
          const url = listing.querySelector('a').getAttribute('href');
          const tipo = listing.querySelector('.tipo').innerText;
          const localizacao = listing.querySelector('.localizacao span').innerText;
          const detalhes = listing.querySelector('.detalhes .detalhe span').innerText;
          const descricao = listing.querySelector('.descricao span').innerText;
          const valor = listing.querySelector('.valor h5').innerText;
          const referencia = listing.querySelector('.referencia span').innerText;

          results.push({
              id,
              images,
              url,
              tipo,
              localizacao,
              detalhes,
              descricao,
              valor,
              referencia
          });
      });

      const data = JSON.stringify(results, null, 2);
      console.log('data:',data);
      return data;
    }
    """
    let savePageMock: SavePage =
      fun (response:string) -> async {
        Directory.CreateDirectory("./out") |> ignore
        File.WriteAllText(sprintf "./out/%s.json" (System.Guid.NewGuid().ToString()),response)
        let data: PageDownloadedData = {
          Id = "0"
          Url = ""
          SavedAt = DateTime.Now 
          Content = response 
        }
        return Result.Ok (data)
      }
    let args:ExtractArgs = {
      Url = url
      JsCode = jsCode
      Headless = true
      ExecutablePath = Env.webEngine
      SavePage = Some savePageMock
      LoggerFactory = None 
    }       
    let! response = ExtractorService.extractPageWith<obj array> args
    File.WriteAllText("./out.json",JsonConvert.SerializeObject(response))
  } 

[<Fact>]  
let `` test FSharp.Data html parser``() = 
  let htmlString = File.ReadAllText("Extractions/Higarashi/Html/imoveis.3.f5150c4b-2eaf-4e34-a9d5-b5e6b6919aa7.html")
  let properties = Parser.parseHtml htmlString
  let json = JsonConvert.SerializeObject(properties)
  let expectedJson = File.ReadAllText("Extractions/Higarashi/Json/imovel.json")
  Logger.info "json extracted {data}" [|json|]
  Assert.Equal(expectedJson,json)

[<Theory>]
[<InlineData("http://localhost:8182/imoveis.html")>]
[<InlineData("https://www.higashiimoveis.com.br/alugar")>]
let `` Test content download ``(url:string) = async {  
  let mongoUrl = configValue "Mongo:ConnectionUrl"
  let htmlStorage = HtmlStorage.create(mongoUrl)
  let savePage: SavePage = htmlStorage.SaveHtml
  let logger = Logger.standardLoggerFactory ()
  let downloadPageCmd = HigashiImoveisExtractor.downloadPage configValue savePage (Some logger)  
  let! response = downloadPageCmd url
  Assert.True(response |> Result.isOk)
}
[<Theory>]
// [<InlineData("http://localhost:8182/imoveis.html")>]
[<InlineData("https://www.higashiimoveis.com.br/alugar")>]
let `` Test download and extraction ``(url:string) = async {  
  let mongoUrl = configValue "Mongo:ConnectionUrl"
  let htmlStorage = HtmlStorage.create(mongoUrl)
  let scrapStorage = ScrapStorage.create(mongoUrl)
  let serialize (o:'a) = JsonConvert.SerializeObject(o)
  let parse = (Parser.parseListHtml >> serialize)
  let savePage = htmlStorage.SaveHtml  
  let saveExtraction = scrapStorage.SaveScrap
  let logger = Logger.standardLoggerFactory ()
  let downloadPageCmd = HigashiImoveisExtractor.downloadPage configValue savePage (Some logger)
  let extractPageCmd = HigashiImoveisExtractor.extractPageCmd parse saveExtraction
  // let! response = downloadPageCmd url
  let downloadedEvent = downloadPageCmd url
  let extractPageDownloaded =
    fun _event ->
      match _event with
      | PageDownloaded data ->
         extractPageCmd data
  let! res =    
    downloadedEvent
    >>= extractPageDownloaded
  Assert.True(res |> Result.isOk)
}
[<Theory>]
// [<InlineData("http://localhost:8182/imoveis.html")>]
[<InlineData("https://www.higashiimoveis.com.br/alugar")>]
let `` Test extraction is loadable from doc storage ``(url:string) = async {  
  let mongoUrl = configValue "Mongo:ConnectionUrl"
  let htmlStorage = HtmlStorage.create(mongoUrl)
  let scrapStorage = ScrapStorage.create(mongoUrl)
  let serialize (o:'a) = JsonConvert.SerializeObject(o)
  let parse = (Parser.parseListHtml >> serialize)
  let savePage = htmlStorage.SaveHtml  
  let saveExtraction = scrapStorage.SaveScrap
  let logger = Logger.standardLoggerFactory ()
  let downloadPageCmd = HigashiImoveisExtractor.downloadPage configValue savePage (Some logger)
  let extractPageCmd = HigashiImoveisExtractor.extractPageCmd parse saveExtraction
  // let! response = downloadPageCmd url
  let downloadedEvent = downloadPageCmd url
  let extractPageDownloaded =
    fun _event ->
      match _event with
      | PageDownloaded data ->
         extractPageCmd data
           
  let! res =    
    downloadedEvent
    >>= extractPageDownloaded
  let extractionId =    
    match res with
    | Ok (PageExtracted extractionId) -> extractionId   
    | _ -> ""  
  let! savedExtractionBoxed = scrapStorage.LoadProperty(extractionId)
  let savedExtraction = savedExtractionBoxed |> Option.map (fun e -> e :> ScrappedDocument)
  match savedExtraction with
  | Some extraction -> 
    Assert.NotEmpty(extraction.Id.ToString())
    Assert.Equal(extractionId,extraction.Id.ToString())
    
  | None ->
    Assert.Fail($"extraction with Id = {extractionId} was not found")
}
[<Fact>]
let `` Test extraction transformation ``() = async {
  let mongoUrl = configValue "Mongo:ConnectionUrl"  
  let scrapStorage = ScrapStorage.create(mongoUrl)
  let scrapId = "66b18591ef09e672b2cb6599"
  let! scrap = scrapStorage.LoadProperty(scrapId)
  let doc:ScrappedDocument = scrap.Value
  let json = doc.Content
  let dtos = HigashiImoveisExtractor.transformExtraction json
  let ids = dtos |> Array.map (fun dto -> dto.Id)
  Assert.NotEmpty(dtos)
  Assert.DoesNotContain("",ids)
}
[<Fact>]
let `` Test whole process ``() = async {
  //let htmlString = File.ReadAllText("Extractions/Higarashi/Html/imoveis.3.f5150c4b-2eaf-4e34-a9d5-b5e6b6919aa7.html")          
  let htmlStorage = HtmlStorage.create(configValue "Mongo:ConnectionUrl")
  let scrapStorage = ScrapStorage.create(configValue "Mongo:ConnectionUrl")
  let savePage: SavePage = htmlStorage.SaveHtml
  let serialize (o:'a) = JsonConvert.SerializeObject(o)
  let parse = (Parser.parseHtml >> serialize)
  // let saveExtraction: 'a -> Async<Result<int,AppError>> =
  //   fun data -> async { return Result.Ok 0 }
  let saveExtraction = scrapStorage.SaveScrap
  let downloadPageCmd = HigashiImoveisExtractor.downloadPage configValue savePage None
  let extractPageCmd = HigashiImoveisExtractor.extractPageCmd parse saveExtraction
  let downloadedEvent = downloadPageCmd "https://www.higashiimoveis.com.br/alugar"
  let extractPageDownloaded =
    fun _event ->
      match _event with
      | PageDownloaded data ->
         extractPageCmd data
  let loadProperty = scrapStorage.LoadProperty
  let getExtractionById = loadProperty
  
  // let getExtractionById: string -> Async<JsonExtraction option> = fun (extractionId:string) -> async {
  //   return Some (File.ReadAllText("Extractions/Higarashi/Json/imovel.json"))
  // }
  let transformExtraction: JsonExtraction -> ImovelDto =
    fun jsonExtraction ->
      let data = JsonConvert.DeserializeObject<HigashiProperty>(jsonExtraction)      
      let targetData: ImovelDto = {
        QuantidadeBanheiros = 0
        QuantidadeQuartos = 0
        QuantidadeVagas = 0
        Preco = decimal (data.Rent)
        Titulo = data.Title
        Endereco = data.Location
        Adicionais = data.Description
        Status = ""
        Images = data.Images 
        Features = [|
          {Name = "Area";Value = data.Area}
          {Name = "Reference";Value = data.Reference}
        |] 
      }
      targetData
  
  let saveExtraction: 'a -> Async<Result<DomainEvent,AppError>> =
    fun data -> async {
      return Result.Ok (DomainEvent.ExtractionSaved 0)
    }
  let raiseEvent: DomainEvent -> Async<Result<unit,AppError>> =
    fun _event -> async {
      return Result.Ok ()
    }
  let transformExtraction = transformExtractionCmd getExtractionById transformExtraction saveExtraction raiseEvent
  let! res =    
    downloadedEvent
    >>= extractPageDownloaded
    >>= (fun (PageExtracted e) -> transformExtraction e)
  // let parser = HigashiImoveisExtractor.parseHtml
  // let saveExtraction (properties) = async {
  //   return Ok 0
  // }
  // let cmd = HigashiImoveisExtractor.extractPageCmd parser saveExtraction 
  // let res =
  //   htmlString   
  //   |> cmd
  return ()
}