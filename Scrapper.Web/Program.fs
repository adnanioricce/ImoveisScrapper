module Scrapper.Web.App

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open ImoveisScrapper
open System.Data
open ImoveisScrapper.Db.Imoveis
open Npgsql
open Dapper
module Db =        
    type ConnectionFactory = unit -> IDbConnection
    type ConnectionHandler<'a> = IDbConnection -> 'a
    let createConnection () :IDbConnection = new NpgsqlConnection(Env.connStr)
    let onConnectionWith (connectionFactory:ConnectionFactory) (func:ConnectionHandler<'a>)  =
        connectionFactory()
        |> func
    let get offset limit = async {
        use conn = createConnection ()
        let! response = ImoveisScrapper.Db.Imoveis.get offset limit conn        
        //TODO:Log
        return 
            response
            |> Result.defaultValue Seq.empty
    }

        
        
        
// ---------------------------------
// Models
// ---------------------------------

type Message =
    {
        Text: string
        Imoveis : ImovelDto seq
    } 

// ---------------------------------
// Views
// ---------------------------------

module Views =
    open Giraffe.ViewEngine

    let layout (content: XmlNode list) =
        let bootstrapJsLink = 
            script [_src "https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js" 
                    _integrity "sha384-C6RzsynM9kWDrMNeT87bh95OGNyZPhcTNXj1NW7RuBCsyN/o0jlpcV8Qyq46cDfL"
                    _crossorigin"anonymous"]
                    []
        html [] [
            head [] [
                title []  [ encodedText "Scrapper.Web" ]
                link [ _rel  "stylesheet"
                       _type "text/css"
                       _href "/main.css" ]
                link [ 
                    _href "https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css"; 
                    _rel "stylesheet"; 
                    _integrity "sha384-T3c6CoIi6uLrA9TneNEoa7RxnatzjcDSCmG1MXxSR1GAsXEV/Dwwykc2MPK8M2HN"; 
                    _crossorigin "anonymous" ]
                
            ]
            body [] (bootstrapJsLink :: content)            
        ]

    let partial () =
        h1 [] [ encodedText "Scrapper.Web" ]

    let index (model : Message) =
        let renderImages imovel =
            imovel.Images 
            |> Seq.map (fun image -> img [ _src image; _width "286";_height "200" ]) 
            |> Seq.toList
        let renderImoveis imoveis =            
            imoveis 
            |> Seq.map (fun imovel ->
                div [ _class "card"] [
                    h1 [ _class "card-title"] [ encodedText imovel.Titulo ]
                    p [_class "card-text"] [ encodedText imovel.Endereco ]
                    p [_class "card-text"] [ encodedText (imovel.Preco.ToString())]
                    p [_class "card-text"] [ encodedText (imovel.Status)]
                    p [_class "card-text"] [ encodedText (sprintf "Quantidade de Banheiros: %d" imovel.QuantidadeBanheiros)]
                    p [_class "card-text"] [ encodedText (sprintf "Quantidade de Quartos: %d" imovel.QuantidadeQuartos)]
                    p [_class "card-text"] [ encodedText (sprintf "Quantidade de Vagas: %d" imovel.QuantidadeVagas)]
                    div [] (renderImages imovel)

                ] )
            |> Seq.toList
        let renderTable imoveis =            
            let rows = 
                imoveis
                |> Seq.mapi (fun i imovel ->
                    tr [] [
                        td [_scope "row"] [ encodedText (string i) ]
                        td [ _class "card-title"] [ encodedText imovel.Titulo ]
                        td [_class "card-text"] [ encodedText imovel.Endereco ]
                        td [_class "card-text"] [ encodedText (imovel.Preco.ToString())]
                        td [_class "card-text"] [ encodedText (imovel.Status)]
                        td [_class "card-text"] [ encodedText (string imovel.QuantidadeBanheiros)]
                        td [_class "card-text"] [ encodedText (string imovel.QuantidadeQuartos)]
                        td [_class "card-text"] [ encodedText (string imovel.QuantidadeVagas)]
                        //div [] (renderImages imovel)
                    ])
            let headers () =
                thead [_class "thead-dark"] [
                    th [_scope "col"] [ encodedText "Id" ]
                    th [_scope "col"] [ encodedText "Titulo" ]
                    th [_scope "col"] [ encodedText "Endereco" ]
                    th [_scope "col"] [ encodedText "Preco" ]
                    th [_scope "col"] [ encodedText "Status"]
                    th [_scope "col"] [ encodedText "Quantidade de Banheiros"]
                    th [_scope "col"] [ encodedText "Quantidade de Quartos"]
                    th [_scope "col"] [ encodedText "Quantidade de Vagas"]
                ]
                
            table [ _class "table"] [
                headers ()
                tbody [] (rows |> Seq.toList)
            ]        
        
        [
            partial()
            p [] [ encodedText model.Text ]
            //cardBody ()
            renderTable model.Imoveis
            
        ] |> layout

// ---------------------------------
// Web app
// ---------------------------------

let indexHandler (name : string) =
    let extractions = (Db.get 0 100) |> Async.RunSynchronously
    let greetings = sprintf "Olá %s! esses são todos os imoveis extraidos nos ultimos dias." name
    let model     = { Text = greetings; Imoveis = extractions }
    let view      = Views.index model
    htmlView view

let webApp =
    choose [
        GET >=>
            choose [
                route "/" >=> indexHandler "world"
                routef "/hello/%s" indexHandler
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