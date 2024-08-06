namespace Scrapper.Lib.Domain

open ImoveisScrapper
open Scrapper.Lib.DAL

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
      return! ExtractorService.extractPage<VivaRealCardDto[]> url jsCode
    }
    let mapper (d:VivaRealCardDto):ImoveisRepository.ImovelDto =
        let parseInt (value:string) = 
            match System.Int32.TryParse(value) with
            | true,parsedValue -> parsedValue
            | false,_ -> 0
        { 
            QuantidadeBanheiros = parseInt d.banheiros
            QuantidadeQuartos = parseInt (string d.quartos)
            QuantidadeVagas = parseInt (string d.vagas)
            Preco = Money.parsePrice d.price
            Titulo = d.title
            Status = d.status
            Endereco = d.address
            Adicionais = d.amenities |> String.concat ";"
            Images = d.images
            Features = [|
                { Name = "Banheiros"; Value = {| Quantidade = (parseInt d.banheiros)  |} }
                { Name = "Quartos"; Value = {| Quantidade = (parseInt (string d.quartos))  |} }
                { Name = "Vagas"; Value = {| Quantidade = (parseInt (string d.vagas))  |} }
            |] 
        }
