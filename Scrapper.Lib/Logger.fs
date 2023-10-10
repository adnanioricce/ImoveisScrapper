namespace Scrapper

//open Microsoft.Extensions.Logging
open Serilog
open Microsoft.Extensions.Logging

module Logger =
    let createWith<'a> (fact:ILoggerFactory) = fact.CreateLogger<'a>()
    let Instance = LoggerConfiguration().WriteTo.Console().CreateLogger()         

