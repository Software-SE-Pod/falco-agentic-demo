module FalcoDemo.Api.Handlers

open System
open Falco
open FalcoDemo.Api.Domain
open FalcoDemo.Api.Store

let private problem status title detail =
    Response.withStatusCode status
    >> Response.ofJson
        {|
            status = status
            title = title
            detail = detail
        |}

/// GET /health - liveness probe used by the CI smoke test.
let health: HttpHandler =
    Response.ofJson
        {|
            status = "ok"
            service = "FalcoDemo.Api"
        |}

/// GET /products - every product, sorted by SKU.
let listProducts (store: ProductStore) : HttpHandler =
    fun ctx ->
        let products = store.All()

        let payload =
            {|
                count = List.length products
                inventoryValueCents = Validation.inventoryValueCents products
                items = products
            |}

        Response.ofJson payload ctx

/// GET /products/low-stock - products at or below a quantity threshold.
/// Accepts an optional `threshold` query parameter (defaults to 20).
let lowStockProducts (store: ProductStore) : HttpHandler =
    fun ctx ->
        let query = Request.getQuery ctx
        let threshold = query.GetInt("threshold", 20)
        let items = Validation.lowStock threshold (store.All())

        let payload =
            {|
                threshold = threshold
                count = List.length items
                items = items
            |}

        Response.ofJson payload ctx

/// GET /products/{id} - a single product, or 404.
let getProduct (store: ProductStore) : HttpHandler =
    fun ctx ->
        let route = Request.getRoute ctx

        match Guid.TryParse(route.GetString "id") with
        | false, _ -> problem 400 "Invalid identifier" "The id segment must be a GUID." ctx
        | true, id ->
            match store.TryFind id with
            | Some product -> Response.ofJson product ctx
            | None -> problem 404 "Not found" $"No product with id {id}." ctx

/// POST /products - validate then create.
let createProduct (store: ProductStore) : HttpHandler =
    Request.mapJson (fun (candidate: NewProduct) ->
        match Validation.validateNewProduct candidate with
        | Ok valid ->
            let created = store.Add valid

            Response.withStatusCode 201
            >> Response.withHeaders [ "Location", $"/products/{created.Id}" ]
            >> Response.ofJson created
        | Error problems ->
            Response.withStatusCode 422
            >> Response.ofJson
                {|
                    status = 422
                    title = "Validation failed"
                    errors = problems
                |}
    )

/// DELETE /products/{id} - idempotent removal.
let deleteProduct (store: ProductStore) : HttpHandler =
    fun ctx ->
        let route = Request.getRoute ctx

        match Guid.TryParse(route.GetString "id") with
        | false, _ -> problem 400 "Invalid identifier" "The id segment must be a GUID." ctx
        | true, id ->
            store.Remove id |> ignore
            Response.withStatusCode 204 >> Response.ofEmpty <| ctx
