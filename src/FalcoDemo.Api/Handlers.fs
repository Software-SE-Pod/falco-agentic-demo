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

/// GET /products/{id}/quote/{qty} - volume-priced quote for a product.
let quote (store: ProductStore) : HttpHandler =
    fun ctx ->
        let route = Request.getRoute ctx

        match Guid.TryParse(route.GetString "id"), Int32.TryParse(route.GetString "qty") with
        | (false, _), _ -> problem 400 "Invalid identifier" "The id segment must be a GUID." ctx
        | _, (false, _) -> problem 400 "Invalid quantity" "The qty segment must be an integer." ctx
        | (true, id), (true, rawQty) ->
            match Pricing.validateQuantity rawQty with
            | Error problems ->
                (Response.withStatusCode 422
                 >> Response.ofJson
                     {|
                         status = 422
                         title = "Validation failed"
                         errors = problems
                     |})
                    ctx
            | Ok qty ->
                match store.TryFind id with
                | None -> problem 404 "Not found" $"No product with id {id}." ctx
                | Some product ->
                    let listTotal = Pricing.listTotalCents product.UnitPriceCents qty
                    let total = Pricing.discountedTotalCents product.UnitPriceCents qty

                    Response.ofJson
                        {|
                            sku = product.Sku
                            quantity = qty
                            listTotalCents = listTotal
                            totalCents = total
                            savingsCents = listTotal - total
                            discountPercent = Pricing.discountPercentFor qty
                        |}
                        ctx
