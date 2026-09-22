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

// --- Pricing tiers -------------------------------------------------------
// Boundaries matter more than midpoints: an off-by-one in a tier test passes
// at qty=75 and fails a customer at qty=51.

[<Theory>]
[<InlineData(1, 0)>]
[<InlineData(10, 0)>]
[<InlineData(11, 5)>]
[<InlineData(50, 5)>]
[<InlineData(51, 10)>]
[<InlineData(100, 10)>]
[<InlineData(101, 20)>]
let ``discount percent is correct at every tier boundary`` (quantity: int) (expected: int) =
    Assert.Equal(expected, Pricing.discountPercentFor quantity)

[<Fact>]
let ``no discount applies at or below the small lot threshold`` () =
    Assert.Equal(10_000L, Pricing.discountedTotalCents 1000 10)

[<Fact>]
let ``small lot tier takes five percent off`` () =
    Assert.Equal(10_450L, Pricing.discountedTotalCents 1000 11)

[<Fact>]
let ``volume tier takes ten percent off`` () =
    Assert.Equal(45_900L, Pricing.discountedTotalCents 1000 51)

[<Fact>]
let ``bulk tier takes twenty percent off`` () =
    Assert.Equal(80_800L, Pricing.discountedTotalCents 1000 101)

[<Fact>]
let ``discounted total never exceeds list total`` () =
    for qty in [ 1; 10; 11; 50; 51; 100; 101; 5000 ] do
        let list = Pricing.listTotalCents 2499 qty
        Assert.True(Pricing.discountedTotalCents 2499 qty <= list)

[<Fact>]
let ``large quantities do not overflow`` () =
    // 21999 * 1_000_000 overflows a 32-bit int; the int64 path must not.
    let total = Pricing.discountedTotalCents 21999 1_000_000
    Assert.True(total > 0L)
    Assert.Equal(17_599_200_000L, total)

[<Fact>]
let ``discount rounds to the nearest cent rather than truncating`` () =
    // 333 * 11 = 3663; 5% = 183.15, which rounds to 183, leaving 3480.
    Assert.Equal(3_480L, Pricing.discountedTotalCents 333 11)

[<Fact>]
let ``zero quantity is rejected`` () =
    match Pricing.validateQuantity 0 with
    | Ok _ -> failwith "expected validation to fail"
    | Error errors -> Assert.Contains(errors, fun e -> e.Field = "qty")

[<Fact>]
let ``negative quantity is rejected`` () =
    match Pricing.validateQuantity -5 with
    | Ok _ -> failwith "expected validation to fail"
    | Error errors -> Assert.Contains(errors, fun e -> e.Field = "qty")

[<Fact>]
let ``positive quantity is accepted`` () =
    match Pricing.validateQuantity 12 with
    | Ok qty -> Assert.Equal(12, qty)
    | Error errors -> failwithf "expected success, got %A" errors
