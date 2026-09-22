module FalcoDemo.Api.Store

open System
open System.Collections.Concurrent
open FalcoDemo.Api.Domain

/// In-memory product store. Deliberately simple: the point of this repo is the
/// GitHub platform surface around the code, not the persistence layer.
type ProductStore() =
    let products = ConcurrentDictionary<Guid, Product>()

    /// Returns every stored product sorted by SKU.
    member _.All() =
        products.Values |> Seq.sortBy (fun p -> p.Sku) |> List.ofSeq

    /// Returns the product with the supplied identifier when it exists.
    member _.TryFind(id: Guid) =
        match products.TryGetValue id with
        | true, product -> Some product
        | false, _ -> None

    /// Stores a validated product and returns the persisted value.
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

    /// Removes the product with the supplied identifier and reports whether one was present.
    member _.Remove(id: Guid) = products.TryRemove id |> fst

    /// Populates the store with the demo catalogue and returns the same store instance.
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
