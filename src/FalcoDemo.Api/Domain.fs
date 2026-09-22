module FalcoDemo.Api.Domain

open System

/// An item of office stock the demo API serves.
type Product =
    {
        Id: Guid
        Sku: string
        Name: string
        UnitPriceCents: int
        QuantityOnHand: int
    }

/// Payload accepted when creating a product.
type NewProduct =
    {
        Sku: string
        Name: string
        UnitPriceCents: int
        QuantityOnHand: int
    }

type ValidationError = { Field: string; Message: string }

module Validation =
    let private require field message predicate =
        if predicate then
            None
        else
            Some { Field = field; Message = message }

    /// Validates an inbound product payload, returning every problem found
    /// rather than failing on the first one.
    let validateNewProduct (candidate: NewProduct) : Result<NewProduct, ValidationError list> =
        let errors =
            [
                require "sku" "sku is required" (not (String.IsNullOrWhiteSpace candidate.Sku))
                require "name" "name is required" (not (String.IsNullOrWhiteSpace candidate.Name))
                require "unitPriceCents" "unitPriceCents must be greater than zero" (candidate.UnitPriceCents > 0)
                require "quantityOnHand" "quantityOnHand cannot be negative" (candidate.QuantityOnHand >= 0)
            ]
            |> List.choose id

        match errors with
        | [] -> Ok candidate
        | problems -> Error problems

    /// Total value of stock held, in cents.
    let inventoryValueCents (products: Product list) =
        products
        |> List.sumBy (fun p -> int64 p.UnitPriceCents * int64 p.QuantityOnHand)

    /// Products at or below the given quantity threshold, sorted by SKU.
    let lowStock (threshold: int) (products: Product list) =
        products
        |> List.filter (fun p -> p.QuantityOnHand <= threshold)
        |> List.sortBy (fun p -> p.Sku)
