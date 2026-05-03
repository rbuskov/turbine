# Turbine

Turbine is an experimental ASP.NET Core library for Minimal APIs that eliminates the need for dedicated request/response DTO 
classes, provides runtime request validation, and helps with accurate OpenAPI spec generation. This simpifies REST API 
development significantly and prevents drift between model shapes, validation logic and published specification.

API models are built from your internal domain models, using a fluent API that offers rich customization options 
including static validation rules. 

Requests and responses are stored as `JsonElement` objects internally at runtime, eliminating the need for strongly typed 
DTOs. Turbine handles binding to your internal domain models and performs static validation of incoming requests 
according to configured rules. 

Turbine also adds all configured API models to your OpenAPI spec automatically, and gives you extension methods to 
reference models when mapping Minimal API endpoints - similar to `Produces<T>` known from ASP.NET Core. 

Turbine builds on the ideas behind the [SpecGuard](https://github.com/rbuskov/SpecGuard) validation library and is 
work in progress.
