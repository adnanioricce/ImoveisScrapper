namespace Scrapper.Lib.Utils

open System
// [<Open>]
module ErrorHandling =
    type AppError =
    | DatabaseFailure of Exception
    // type DatabaseResult =
    // | Updated of int
    // | Inserted of int 
    
