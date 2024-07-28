namespace Scrapper.Lib

module Events =
    type ExtractionUrl = ExtractionUrl of string
    type PageContent = string 
    type DomainEvent =
    // first we extract the page...
    | PageExtracted of (ExtractionUrl * PageContent)
    // then we save in a database, with info about the extraction...
    | ExtractedPageSaved of ExtractionUrl
    // then, with another process, we mark the extraction for parsing, or cleaning if you prefer 
    | ExtractedPageMarkedForCleaning of ExtractionUrl
    // maybe, we already scrapped the page, but it's been updated since our last visit
    | ExtractedPageUpdated of ExtractionUrl
    // then we parse/clean the data 
    | ExtractionCleaned of ExtractionUrl
    // then we save the cleaned data...
    | FinalDataSaved of ExtractionUrl
    // and finally, we cache it!
    | FinalDataCached of ExtractionUrl
    