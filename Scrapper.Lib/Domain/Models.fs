namespace ImoveisScrapper

open System
open Microsoft.Extensions.Configuration
open Scrapper.Lib.Utils.ErrorHandling
[<AutoOpen>]
module Base =
    type DirectoryPath = string
    type WebPageUrl = string
    type HtmlPage = string    
    type JsonExtraction = string
    type DatabaseId<'a> = 'a
    [<CLIMutable>]
    type RawDocument<'id,'a> = {
        Id: 'id
        SavedAt: DateTime
        Content: 'a
    }    
module Mapper =
    type SpecificToGeneralDto<'specific,'general> = 'specific -> 'general
module Extractor =
    open Scrapper.Lib.Utils.ErrorHandling
    type FilePath = string
    
    type GetExtractions<'extraction> = unit -> Async<'extraction seq>
    type PersistExtractions<'extraction> = GetExtractions<'extraction> -> Async<Result<int,AppError>>
    type SaveExtraction<'extraction,'id> = 'extraction -> Async<Result<DatabaseId<'id>,AppError>> 
module Money =    
    type Money =
    | CurrencyAmount of string * decimal
    | InvalidMoney of string

    let parseMoneyString (input: string) =
        match input.Split(' ') with
        | [| currency; amount |] when currency = "R$" ->            
            match System.Decimal.TryParse(amount, System.Globalization.NumberStyles.Currency, System.Globalization.CultureInfo.InvariantCulture) with
            | true, amountDecimal -> CurrencyAmount ("R$", amountDecimal)
            | _ -> InvalidMoney input
        | _ -> InvalidMoney input
    let parsePrice value =
        match parseMoneyString value with
        | Money.CurrencyAmount (currency,amount) -> amount
        | _ -> 0.0m
module ScrapperConfig =
    type GetConfigurationValue = string -> string
    //type GetConfigurationValueOrDefault = string -> string -> string