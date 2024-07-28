namespace ImoveisScrapper
module Mapper =
    type SpecificToGeneralDto<'specific,'general> = 'specific -> 'general
module Extractor =
    open Scrapper.Lib.Utils.ErrorHandling
    type FilePath = string
    type GetExtractions<'extraction> = unit -> Async<'extraction seq>
    type PersistExtractions<'extraction> = GetExtractions<'extraction> -> Async<Result<int,AppError>>
    
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
// module PersistJob =
//     