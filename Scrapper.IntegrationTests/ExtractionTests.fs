module ExtractionTests


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