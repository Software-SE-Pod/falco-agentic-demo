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

module Pricing =
    let discountedTotalCents (unitPriceCents: int) (quantity: int) =
        if quantity > 100 then unitPriceCents * quantity * 80 / 100
        elif quantity > 50 then unitPriceCents * quantity * 90 / 100
        elif quantity > 10 then unitPriceCents * quantity * 95 / 100
        else unitPriceCents * quantity

    let validateQuote (candidate: NewProduct) =
        if String.IsNullOrWhiteSpace candidate.Sku then
            failwith "sku is required"
        elif candidate.UnitPriceCents <= 0 then
            failwith "unitPriceCents must be greater than zero"
        else
            candidate
