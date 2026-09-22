module FalcoDemo.Api.Store

open System
open System.Collections.Concurrent
open FalcoDemo.Api.Domain

/// In-memory product store. Deliberately simple: the point of this repo is the
/// GitHub platform surface around the code, not the persistence layer.
type ProductStore() =
    let products = ConcurrentDictionary<Guid, Product>()

    member _.All() =
        products.Values |> Seq.sortBy (fun p -> p.Sku) |> List.ofSeq

    member _.TryFind(id: Guid) =
        match products.TryGetValue id with
        | true, product -> Some product
        | false, _ -> None

    member _.Add(candidate: NewProduct) =
        let product =
            {
                Id = Guid.NewGuid()
                Sku = candidate.Sku.Trim().ToUpperInvariant()
                Name = candidate.Name.Trim()
                UnitPriceCents = candidate.UnitPriceCents
                QuantityOnHand = candidate.QuantityOnHand
            }

        products[product.Id] <- product
        product

    member _.Remove(id: Guid) = products.TryRemove id |> fst

    member this.Seed() =
        [
            {
                Sku = "STP-1001"
                Name = "Copy paper, 8.5x11, case"
                UnitPriceCents = 4599
                QuantityOnHand = 120
            }
            {
                Sku = "STP-2042"
                Name = "Gel pen, black, 12-pack"
                UnitPriceCents = 1299
                QuantityOnHand = 480
            }
            {
                Sku = "STP-3310"
                Name = "Standing desk converter"
                UnitPriceCents = 21999
                QuantityOnHand = 14
            }
        ]
        |> List.iter (this.Add >> ignore)

        this
