#r "nuget: Dapper"
#r "nuget: Npgsql"
#r "nuget: CsvHelper"

open System
open System.IO
open Dapper
open Npgsql
open CsvHelper
open CsvHelper.Configuration
open System.Globalization

// Define the Imovel type (matching the database table structure)
type Imovel = {
    link: string
    imagem: string
    descricao: string
    endereco: string
    preco: string
}

// Define database connection string (update credentials accordingly)
let connectionString = "Host=localhost;Username=postgres;Password=mysecretpassword;Database=scrapperdb"

// Function to read CSV file and return a list of Imovel records
let readCsv (filePath: string) : Imovel list =
    use reader = new StreamReader(filePath)
    use csv = new CsvReader(reader, CultureInfo.InvariantCulture)
    let records = csv.GetRecords<Imovel>() |> Seq.toList
    records

// Function to insert Imovel data into PostgreSQL database
let insertToDatabase (imoveis: Imovel list) =
    use conn = new NpgsqlConnection(connectionString)
    conn.Open()
    let insertQuery = """
        INSERT INTO imoveis (link, imagem, descricao, endereco, preco)
        VALUES (@link, @imagem, @descricao, @endereco, @preco)
    """

    for imovel in imoveis do
        conn.Execute(insertQuery, imovel) |> ignore

    conn.Close()
let files = Directory.GetFiles("./Scrapper.Web/out/lopes/")
files 
|> Array.map (fun filePath -> 
  //let filePath = "scraped_data.csv" // Update the path to your CSV file

  try
      let imoveis = readCsv filePath
      insertToDatabase imoveis
      printfn "Data successfully inserted into the database!"
  with
      | ex -> printfn "An error occurred: %O" ex)


