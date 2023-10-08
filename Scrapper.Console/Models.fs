namespace ImoveisScrapper
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

