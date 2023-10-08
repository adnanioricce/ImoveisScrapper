namespace ImoveisScrapper

open System.Text.Json

module Extractor =
    open PuppeteerSharp
    
    let extractPage<'a> url jsCode = async {
        let options = LaunchOptions(Headless = true,ExecutablePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe")
        use! browser = Puppeteer.LaunchAsync(options,loggerFactory = null) |> Async.AwaitTask
        use! page = browser.NewPageAsync() |> Async.AwaitTask
        let! response = page.GoToAsync(url) |> Async.AwaitTask
        let! json = page.EvaluateFunctionAsync<string>(jsCode) |> Async.AwaitTask
        let cardsDtos = JsonSerializer.Deserialize<'a>(json)
        return cardsDtos
    }
module VivaRealExtractor =
    type VivaRealCardDto = {        
        title: string
        address: string
        amenities: string array
        banheiros: string
        quartos: obj
        vagas: obj
        images: string array
        price: string
        status: string        
     }
    [<Literal>]
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
    let extractImoveis url = async {
        return! Extractor.extractPage<VivaRealCardDto[]> url jsCode        
    }