namespace Scrapper.Lib.DAL

open System.Data
open Dapper
open Scrapper
open Scrapper.Lib.Utils.ErrorHandling

module ImoveisRepository =
    [<Struct>]
    [<CLIMutable>]
    type ImovelFeatureDto = {
        Name: string
        Value: obj        
    }
    
    [<Struct>]
    [<CLIMutable>]
    type ImovelDto = {
        QuantidadeBanheiros: int
        QuantidadeQuartos: int
        QuantidadeVagas: int
        Preco: decimal
        Titulo: string
        Endereco: string
        Adicionais: string
        Status: string
        Images: string array
        Features: ImovelFeatureDto array
    }
    
    [<CLIMutable>]
    type NewImovelDto = {
        Id: string
        Price: decimal
        Title: string
        Description: string
        Address: string
        Size: string
        Url: string
        Images: string array
        Features: ImovelFeatureDto array
    }
// 	, address VARCHAR(512)    
//     , url NVARCHAR(MAX)
    
    type InsertImovel = ImovelDto seq -> Async<Result<int,AppError>>
    type GetImoveis = int -> int -> Async<Result<ImovelDto seq,AppError>>
    let insert (conn:IDbConnection) : InsertImovel =
        fun dto -> async {
            try
                // Logger.info "inserting imovel entries to the database ..." [||]
                let query = @"
                INSERT INTO Imoveis(QuantidadeBanheiros,QuantidadeQuartos,QuantidadeVagas,Preco,Titulo,Endereco,Adicionais,Status)
                VALUES (@QuantidadeBanheiros,@QuantidadeQuartos,@QuantidadeVagas,@Preco,@Titulo,@Endereco,@Adicionais,@Status)"
                let! changeCount = conn.ExecuteAsync(query,dto) |> Async.AwaitTask
                // Logger.info "{count} changes were wrote to the database" [|changeCount|]
                return Result.Ok changeCount
            with
                | ex -> return Result.Error (AppError.DatabaseFailure ex)
        }
    let get (conn:IDbConnection) : GetImoveis =
        fun (offset:int) (limit:int) -> async {
            try
                let query = @"
                SELECT * FROM Imoveis
                LIMIT @Limit
                OFFSET @Offset"
                let! imoveis = conn.QueryAsync<ImovelDto>(query,{| Offset = offset;Limit = limit |}) |> Async.AwaitTask
                return Result.Ok imoveis
            with
                | ex -> return Result.Error (AppError.DatabaseFailure ex)
        }
    