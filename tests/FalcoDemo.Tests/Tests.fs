module FalcoDemo.Tests.DomainTests

open System
open Xunit
open FalcoDemo.Api.Domain
open FalcoDemo.Api.Store

let private validProduct =
    {
        Sku = "STP-9001"
        Name = "Heavy duty stapler"
        UnitPriceCents = 2499
        QuantityOnHand = 36
    }

[<Fact>]
let ``valid product passes validation`` () =
    match Validation.validateNewProduct validProduct with
    | Ok result -> Assert.Equal("STP-9001", result.Sku)
    | Error errors -> failwithf "expected success, got %A" errors

[<Fact>]
let ``blank sku is rejected`` () =
    match Validation.validateNewProduct { validProduct with Sku = "  " } with
    | Ok _ -> failwith "expected validation to fail"
    | Error errors -> Assert.Contains(errors, fun e -> e.Field = "sku")

[<Fact>]
let ``validation reports every problem at once`` () =
    let bad =
        {
            Sku = ""
            Name = ""
            UnitPriceCents = 0
            QuantityOnHand = -1
        }

    match Validation.validateNewProduct bad with
    | Ok _ -> failwith "expected validation to fail"
    | Error errors -> Assert.Equal(4, List.length errors)

[<Fact>]
let ``zero price is rejected`` () =
    match Validation.validateNewProduct { validProduct with UnitPriceCents = 0 } with
    | Ok _ -> failwith "expected validation to fail"
    | Error errors -> Assert.Contains(errors, fun e -> e.Field = "unitPriceCents")

[<Fact>]
let ``inventory value multiplies price by quantity`` () =
    let products =
        [
            {
                Id = Guid.NewGuid()
                Sku = "A"
                Name = "A"
                UnitPriceCents = 100
                QuantityOnHand = 3
            }
            {
                Id = Guid.NewGuid()
                Sku = "B"
                Name = "B"
                UnitPriceCents = 250
                QuantityOnHand = 2
            }
        ]

    Assert.Equal(800L, Validation.inventoryValueCents products)

[<Fact>]
let ``store normalises sku to upper case and trims name`` () =
    let store = ProductStore()

    let created =
        store.Add
            { validProduct with
                Sku = "stp-7"
                Name = "  Padded envelope  "
            }

    Assert.Equal("STP-7", created.Sku)
    Assert.Equal("Padded envelope", created.Name)

[<Fact>]
let ``seeded store returns products sorted by sku`` () =
    let store = ProductStore().Seed()
    let skus = store.All() |> List.map (fun p -> p.Sku)

    Assert.Equal<string list>(List.sort skus, skus)

[<Fact>]
let ``removing a product makes it unfindable`` () =
    let store = ProductStore()
    let created = store.Add validProduct

    Assert.True(store.Remove created.Id)
    Assert.True((store.TryFind created.Id).IsNone)
