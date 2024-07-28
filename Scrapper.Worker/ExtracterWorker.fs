namespace Scrapper.Worker

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Scrapper
type ExtracterWorker(logger: ILogger<ExtracterWorker>) =
    inherit BackgroundService()

    override _.ExecuteAsync(ct: CancellationToken) =
        task {            
            let mutable index = 1
            let rng = Random()
            let randomMinutes offset limit = TimeSpan.FromMinutes(rng.Next(offset,limit))
            let randomSeconds offset limit = TimeSpan.FromSeconds(rng.Next(offset,limit))
            let delay state =
                match state with
                | Jobs.Empty -> randomSeconds 30 360
                | Jobs.NonEmpty -> randomSeconds 1 3
            while not ct.IsCancellationRequested do
                let template index = sprintf "https://www.vivareal.com.br/venda/?pagina=%d" index
                let url = template index
                try 
                    logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now)                    
                    let! state = (Jobs.extractPage url)

                    match state with
                    | Jobs.Empty ->                        
                        logger.LogInformation("Url {url} was empty, a longer delay will be taken before the next extraction", url)        
                    | Jobs.NonEmpty ->
                        logger.LogInformation("Url {url} was non-empty",url)
                    do! Task.Delay(delay state)
                with
                    | ex -> logger.LogError("Url {url} throwed an exception during page extraction -> {ex}", url,ex)
                do! Task.Delay(randomSeconds 30 60)
                
        }
