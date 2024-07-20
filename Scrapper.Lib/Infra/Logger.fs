namespace Scrapper

//open Microsoft.Extensions.Logging
open Serilog
open Microsoft.Extensions.Logging

module Logger =
    type StandardLog = string -> obj array -> unit
    
    let createWith<'a> (fact:ILoggerFactory) = fact.CreateLogger<'a>()
    let private Instance = LoggerConfiguration().WriteTo.Console().CreateLogger()
    let debug: StandardLog = fun template args -> Instance.Debug(template,args)
    let info: StandardLog = fun template args -> Instance.Information(template,args)
    let warn: StandardLog = fun template args -> Instance.Warning(template,args)
    let error: StandardLog = fun template args -> Instance.Error(template,args)     
        