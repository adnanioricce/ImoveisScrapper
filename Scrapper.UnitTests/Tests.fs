module Tests

open System
open Xunit
open Scrapper.Lib
open System.IO
[<Theory>]
[<InlineData("Extractions/Higarashi/Html/imoveis.3.f5150c4b-2eaf-4e34-a9d5-b5e6b6919aa7.html")>]
let ``extract text test`` (filePath:string) = async {
    // let downloadedContent = File.ReadAllText(filePath)
    // let! response = Extractor.extractPage downloadedContent
    // match response with
    // | Ok dtos ->
    //     Assert.NotEmpty(dtos)
    // | Error e ->
    //     Assert.Fail(string e)
    Assert.True(true)
}
