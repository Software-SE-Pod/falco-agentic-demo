module FalcoDemo.Api.Program

open Falco
open Falco.Routing
open Microsoft.AspNetCore.Builder
open FalcoDemo.Api
open FalcoDemo.Api.Store

[<EntryPoint>]
let main args =
    let store = ProductStore().Seed()

    let endpoints =
        [
            get "/health" Handlers.health
            get "/products" (Handlers.listProducts store)
            get "/products/{id}" (Handlers.getProduct store)
            get "/products/{id}/quote/{qty}" (Handlers.quote store)
            post "/products" (Handlers.createProduct store)
            delete "/products/{id}" (Handlers.deleteProduct store)
        ]

    let builder = WebApplication.CreateBuilder(args)
    let app = builder.Build()

    app.UseRouting().UseFalco(endpoints).Run()

    0
