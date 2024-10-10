module Scrapper.Giraffe.Api.App

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Dapper
open Npgsql
open System.IO
open ClosedXML.Excel
open Microsoft.AspNetCore.Http

type Imovel = {
    link: string
    imagem: string
    descricao: string
    endereco: string
    preco: string
}

// Define connection string
let connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")

// Function to get all Imoveis from the database
let getImoveis (next: HttpFunc) (ctx: HttpContext) =
    task {
        use conn = new NpgsqlConnection(connectionString)
        let query = "SELECT link, imagem, descricao, endereco, preco FROM imoveis"
        let! imoveis = conn.QueryAsync<Imovel>(query) |> Async.AwaitTask
        return! json imoveis next ctx
    }

// Function to generate and download the Excel file with Imoveis data
let downloadImoveis (next: HttpFunc) (ctx: HttpContext) =
    task {
        use conn = new NpgsqlConnection(connectionString)
        let query = "SELECT link, imagem, descricao, endereco, preco FROM imoveis"
        let! imoveis = conn.QueryAsync<Imovel>(query) |> Async.AwaitTask

        // Create Excel file using ClosedXML
        use workbook = new XLWorkbook()
        let worksheet = workbook.Worksheets.Add("Imoveis")

        // Define column headers
        worksheet.Cell(1, 1).Value <- "Link"
        worksheet.Cell(1, 2).Value <- "Imagem"
        worksheet.Cell(1, 3).Value <- "Descricao"
        worksheet.Cell(1, 4).Value <- "Endereco"
        worksheet.Cell(1, 5).Value <- "Preco"

        // Add data to the worksheet
        imoveis |> Seq.iteri (fun i imovel ->
            worksheet.Cell(i + 2, 1).Value <- imovel.link
            worksheet.Cell(i + 2, 2).Value <- imovel.imagem
            worksheet.Cell(i + 2, 3).Value <- imovel.descricao
            worksheet.Cell(i + 2, 4).Value <- imovel.endereco
            worksheet.Cell(i + 2, 5).Value <- imovel.preco
        )

        // Write the Excel file to a memory stream
        use memoryStream = new MemoryStream()
        workbook.SaveAs(memoryStream)
        memoryStream.Seek(0L, SeekOrigin.Begin) |> ignore

        // Send the Excel file as a response
        ctx.Response.ContentType <- "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        ctx.Response.Headers.["Content-Disposition"] <- "attachment; filename=imoveis.xlsx"
        return! ctx.WriteStreamAsync(true,memoryStream,None,None) |> Async.AwaitTask
        // return! memoryStream.CopyToAsync(ctx.Response.Body) |> Async.AwaitTask
    }

// ---------------------------------
// Models
// ---------------------------------

type Message =
    {
        Text : string
    }

// ---------------------------------
// Views
// ---------------------------------

module Views =
    open Giraffe.ViewEngine

    let layout (content: XmlNode list) =
        html [] [
            head [] [
                title []  [ encodedText "Scrapper.Giraffe.Api" ]
                link [ _rel  "stylesheet"
                       _type "text/css"
                       _href "/main.css" ]
            ]
            body [] content
        ]

    let partial () =
        h1 [] [ encodedText "Scrapper.Giraffe.Api" ]

    let index (model : Message) =
        [
            partial()
            p [] [ encodedText model.Text ]
        ] |> layout

// ---------------------------------
// Web app
// ---------------------------------

let indexHandler (name : string) =
    let greetings = sprintf "Hello %s, from Giraffe!" name
    let model     = { Text = greetings }
    let view      = Views.index model
    htmlView view
let webApp =
    choose [
        GET >=>
            choose [
                route "/" >=> indexHandler "world"
                routef "/hello/%s" indexHandler
                route "/api/imoveis" >=> getImoveis
                route "/api/imoveis/download" >=> downloadImoveis
            ]
            
        setStatusCode 404 >=> text "Not Found" ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder : CorsPolicyBuilder) =
    builder
        .WithOrigins(
            "http://localhost:5000",
            "https://localhost:5001")
       .AllowAnyMethod()
       .AllowAnyHeader()
       |> ignore

let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
    (match env.IsDevelopment() with
    | true  ->
        app.UseDeveloperExceptionPage()
    | false ->
        app .UseGiraffeErrorHandler(errorHandler)
            .UseHttpsRedirection())
        .UseCors(configureCors)
        .UseStaticFiles()
        .UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    services.AddCors()    |> ignore
    services.AddGiraffe() |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder.AddConsole()
           .AddDebug() |> ignore

[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .UseContentRoot(contentRoot)
                    .UseWebRoot(webRoot)
                    .Configure(Action<IApplicationBuilder> configureApp)
                    .ConfigureServices(configureServices)
                    .ConfigureLogging(configureLogging)
                    |> ignore)
        .Build()
        .Run()
    0