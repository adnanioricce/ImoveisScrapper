module SeedDatabase

open ImoveisScrapper.Db
open Xunit
open ImoveisScrapper
open System.IO
let s = ""
//[<Fact>]
//let run () =
//    let data:Imoveis.ImovelDto = {
//        QuantidadeBanheiros = 0
//        QuantidadeQuartos = 0
//        QuantidadeVagas = 0
//        Preco = 0.0m
//        Titulo = ""
//        Endereco = ""
//        Adicionais = ""
//        Status = ""
//        Images = [|""|]
//    }
//    let extractions = 
//        PersistJob.getExtractions 
//            PersistJob.mapper 
//            (fun _ -> Directory.GetFiles("./Extractions"))
//        |> Seq.collect (fun func -> func())
    
//    Database.createPostgresConnectionWith Env.connectionString
//    |> Imoveis.insert extractions |> Async.RunSynchronously
//    |> Result.map (fun r -> if r >= 0 then "Data inserted with success!" else "Data insertion failed!")
//    |> Result.mapError (fun e -> sprintf "Exception throwed when inserting data %A" e)
//    |> (fun r -> 
//        match (r) with
//        | Ok _ -> ()
//        | Error e -> Assert.Fail(e))
//    0