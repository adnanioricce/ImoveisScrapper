namespace Scrapper.Lib.Utils

open System
// [<Open>]
module ErrorHandling =
    type AppError =
    | DatabaseFailure of Exception

    let unwrap error =
        match error with
        | DatabaseFailure e -> e
    let stringfy error =
        match error with
        | DatabaseFailure e -> string e
    // type DatabaseResult =
    // | Updated of int
    // | Inserted of int 
    
