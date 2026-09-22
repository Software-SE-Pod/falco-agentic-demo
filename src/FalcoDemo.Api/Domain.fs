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

/// Volume pricing. Tiers are expressed as named thresholds rather than magic
/// numbers, and all arithmetic is done in int64 because unit price multiplied
/// by a large quantity overflows a 32-bit int.
module Pricing =

    [<Literal>]
    let private BulkThreshold = 100

    [<Literal>]
    let private VolumeThreshold = 50

    [<Literal>]
    let private SmallLotThreshold = 10

    [<Literal>]
    let private BulkDiscountPercent = 20

    [<Literal>]
    let private VolumeDiscountPercent = 10

    [<Literal>]
    let private SmallLotDiscountPercent = 5

    /// The discount percentage that applies at a given quantity.
    let discountPercentFor (quantity: int) =
        if quantity > BulkThreshold then
            BulkDiscountPercent
        elif quantity > VolumeThreshold then
            VolumeDiscountPercent
        elif quantity > SmallLotThreshold then
            SmallLotDiscountPercent
        else
            0

    /// List price before any discount, in cents.
    let listTotalCents (unitPriceCents: int) (quantity: int) = int64 unitPriceCents * int64 quantity

    /// Discounted total in cents, rounded to the nearest cent rather than
    /// truncated, so the customer is never charged a fraction more than quoted.
    let discountedTotalCents (unitPriceCents: int) (quantity: int) =
        let list = listTotalCents unitPriceCents quantity
        let discount = int64 (discountPercentFor quantity)
        let reduction = (list * discount + 50L) / 100L
        list - reduction

    /// A quote is only meaningful for a positive quantity.
    let validateQuantity (quantity: int) : Result<int, ValidationError list> =
        if quantity > 0 then
            Ok quantity
        else
            Error
                [
                    {
                        Field = "qty"
                        Message = "qty must be greater than zero"
                    }
                ]
