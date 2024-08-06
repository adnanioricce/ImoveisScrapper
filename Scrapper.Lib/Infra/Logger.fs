namespace Scrapper

//open Microsoft.Extensions.Logging
open Serilog
open Microsoft.Extensions.Logging
open Serilog.Extensions.Logging

module Logger =
    open System
    open Microsoft.Extensions.Logging

    type LoggerFactory() =
        let providers = ResizeArray<ILoggerProvider>()
        
        interface ILoggerFactory with
            member _.CreateLogger(categoryName: string) : ILogger =
                let loggers = 
                    providers 
                    |> Seq.map (fun provider -> provider.CreateLogger(categoryName))
                    |> Seq.toArray
                { new ILogger with
                    member _.Log(logLevel, eventId, state, ex, formatter) =
                        for logger in loggers do
                            logger.Log(logLevel, eventId, state, ex, formatter)

                    member _.IsEnabled(logLevel) =
                        loggers |> Array.exists (fun logger -> logger.IsEnabled(logLevel))

                    member _.BeginScope(state) =
                        let scopes = loggers |> Array.map (fun logger -> logger.BeginScope(state))
                        { new IDisposable with
                            member _.Dispose() =
                                for scope in scopes do
                                    scope.Dispose() } }

            member _.AddProvider(provider: ILoggerProvider) =
                providers.Add(provider)

            member _.Dispose() =
                for provider in providers do
                    provider.Dispose()
                providers.Clear()
    let standardLoggerFactory () =        
        let factory = new LoggerFactory()
        factory.AddSerilog() |> ignore
        factory
    type StandardLog = string -> obj array -> unit
    
    let createWith<'a> (fact:ILoggerFactory) = fact.CreateLogger<'a>()
    let private Instance = LoggerConfiguration().WriteTo.Console().CreateLogger()
    let debug: StandardLog = fun template args -> Instance.Debug(template,args)
    let info: StandardLog = fun template args -> Instance.Information(template,args)
    let warn: StandardLog = fun template args -> Instance.Warning(template,args)
    let error: StandardLog = fun template args -> Instance.Error(template,args)     
        