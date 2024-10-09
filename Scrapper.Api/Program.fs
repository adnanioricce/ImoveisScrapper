let connectionString = "Host=localhost;Username=postgres;Password=yourpassword;Database=yourdb"
open System
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.RequestErrors
open Npgsql
open Dapper
open FSharp.Control.Tasks.V2.ContextInsensitive

// Define a record to map the 'imoveis' table
type Imovel = {
    id: int
    codigo: string
    url: string
    imagem_url: string
    descricao: string
    endereco: string
    preco: string
}

// PostgreSQL connection string
let connectionString = "Host=localhost;Username=postgres;Password=yourpassword;Database=yourdb"
// Function to generate Excel file
let generateExcelFile (imoveis: Imovel list) : byte[] =
    use workbook = new XLWorkbook()
    let worksheet = workbook.Worksheets.Add("Imoveis")

    // Add headers
    worksheet.Cell(1, 1).Value <- "ID"
    worksheet.Cell(1, 2).Value <- "Código"
    worksheet.Cell(1, 3).Value <- "URL"
    worksheet.Cell(1, 4).Value <- "Imagem URL"
    worksheet.Cell(1, 5).Value <- "Descrição"
    worksheet.Cell(1, 6).Value <- "Endereço"
    worksheet.Cell(1, 7).Value <- "Preço"

    // Add data
    for (i, imovel) in List.indexed imoveis do
        let row = i + 2
        worksheet.Cell(row, 1).Value <- imovel.id
        worksheet.Cell(row, 2).Value <- imovel.codigo
        worksheet.Cell(row, 3).Value <- imovel.url
        worksheet.Cell(row, 4).Value <- imovel.imagem_url
        worksheet.Cell(row, 5).Value <- imovel.descricao
        worksheet.Cell(row, 6).Value <- imovel.endereco
        worksheet.Cell(row, 7).Value <- imovel.preco

    // Save the workbook to a memory stream
    use stream = new MemoryStream()
    workbook.SaveAs(stream)
    stream.ToArray()

// Route to handle Excel download
let downloadExcelHandler =
    fun ctx ->
        task {
            let! imoveis = getImoveis()

            let excelFile = generateExcelFile imoveis

            ctx.response.headers <- [("Content-Disposition", "attachment; filename=imoveis.xlsx")]
            return! OK (Bytes excelFile) ctx
        }

// Function to retrieve data from the 'imoveis' table using Dapper
let getImoveis () : Task<Imovel list> =
    task {
        use conn = new NpgsqlConnection(connectionString)
        let! imoveis = conn.QueryAsync<Imovel>("SELECT * FROM imoveis")
        return List.ofSeq imoveis
    }

// Suave routes
let app =
    choose [
        path "/imoveis" >=> GET >=> fun ctx ->
            task {
                let! imoveis = getImoveis()
                let jsonResponse = Newtonsoft.Json.JsonConvert.SerializeObject(imoveis)
                return! OK jsonResponse ctx
            }
        path "/imoveis/excel" >=> GET >=> downloadExcelHandler
        NOT_FOUND "Route not found"
    ]

// Start the Suave server
[<EntryPoint>]
let main argv =
    let config = { defaultConfig with bindings = [ HttpBinding.create HTTP IPAddress.Loopback 8080us ] }
    startWebServer config app
    0

