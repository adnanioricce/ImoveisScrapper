namespace ImoveisScrapper

open System.IO
open ImoveisScrapper.Db
open System.Text.Json
open System.Data
open ImoveisScrapper.Extractor

module PersistJob = 
    
    let run<'extraction> (getExtractionsLoaders:GetExtractions<'extraction>) (persistDtos:PersistExtractions<'extraction>) = async {
        return! persistDtos getExtractionsLoaders                                        
    }