﻿[<AutoOpen>]
module Freya.Machine.Types

open Aether
open Aether.Operators
open Freya.Core
open Freya.Core.Operators
open Freya.Types.Http
open Freya.Types.Language

(* Representations *)

type FreyaMachineNegotiation =
    { Charsets: FreyaMachineNegotiationResult<Charset>
      Encodings: FreyaMachineNegotiationResult<ContentCoding>
      MediaTypes: FreyaMachineNegotiationResult<MediaType>
      Languages: FreyaMachineNegotiationResult<LanguageTag> }

and FreyaMachineNegotiationResult<'a> =
    | Negotiated of 'a list
    | Free

type FreyaMachineRepresentation =
    { Metadata: FreyaMachineRepresentationMetadata
      Data: byte [] }

and FreyaMachineRepresentationMetadata =
    { Charset: Charset option
      Encodings: ContentCoding list option
      MediaType: MediaType option
      Languages: LanguageTag list option }

(* Signatures

    Common monadic signatures for the building blocks of Machine
    Definitions. Represent functions that the user of Machine should implement
    when overriding the defaults. *)

type FreyaMachineAction = 
    Freya<unit>

type FreyaMachineDecision = 
    Freya<bool>

type FreyaMachineHandler = 
    FreyaMachineNegotiation -> Freya<FreyaMachineRepresentation>

type FreyaMachineOperation =
    Freya<unit>

(* Definition

    A Definition of a Machine, encoded as the defaults to override
    and the functions (given the previously defined Signatures) provided
    to override them. *)

type FreyaMachineDefinition =
    Map<string, FreyaMachineOverride>

and FreyaMachineOverride =
    | Action of FreyaMachineAction
    | Configuration of obj
    | Decision of FreyaMachineDecision
    | Handler of FreyaMachineHandler

(* Patterns

    Active patterns for discriminating between varying kinds of 
    Override within a Machine Definition. *)

let internal (|Action|) =
    function | Action x -> Some x
             | _ -> None

let internal (|Configuration|) =
    function | Configuration x -> Some x 
             | _ -> None
        
let internal (|Decision|) =
    function | Decision x -> Some x
             | _ -> None

let internal (|Handler|) =
    function | Handler x -> Some x
             | _ -> None

(* Lenses

    Partial lenses (Aether form - see https://github.com/xyncro/aether) 
    to the Machine Definition within an OWIN monad (see Freya.Core),
    and to aspects of the machine definition. *)

let internal definitionPLens =
    dictPLens "freya.machine.definition" <?-> boxIso<FreyaMachineDefinition>

let internal actionPLens k =
    mapPLens k <??> ((|Action|), Action)
    
let internal configurationPLens<'T> (k: string) =
    mapPLens k <??> ((|Configuration|), Configuration) <?-> boxIso<'T>
        
let internal decisionPLens k =
    mapPLens k <??> ((|Decision|), Decision)

let internal handlerPLens k =
    mapPLens k <??> ((|Handler|), Handler) 

(* Configuration

   Typed access to dynamic configuration values at runtime. These are
   evaluated on machine execution, and so may be varied based on the
   specific resource in question (they are a general core Freya<'T>
   expression). *)

let internal config key =
    freya {
        let! value = getPLM (definitionPLens >??> configurationPLens key)

        match value with
        | Some value -> return! Some <!> value
        | _ -> return None }