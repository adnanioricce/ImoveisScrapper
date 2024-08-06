namespace Scrapper.Lib.Domain

open ImoveisScrapper
open Scrapper.Lib.Events
open Scrapper.Lib.Utils.ErrorHandling

module Commands =
    open Scrapper.Lib.Utils.ErrorHandling
    // HtmlPage -> Async<Result<PageDownloadedData,AppError>>
    type SavePage = (HtmlPage -> Async<Result<PageDownloadedData,AppError>>)
    type DownloadPageCommand = WebPageUrl -> Async<Result<DomainEvent,AppError>>
    type ExtractPageCommand = PageDownloadedData -> Async<Result<DomainEvent,AppError>>
    type TransformExtractionCommand = string -> Async<Result<DomainEvent,AppError>>
    type SaveTransformedExtractionCommand<'a> = 'a -> Async<Result<DomainEvent,AppError>>