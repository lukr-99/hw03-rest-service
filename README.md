# HW03 Render Repo

Standalone repository for the PA053 Homework 3 REST service.

## Contents

- `RestHW03.csproj`
- `Program.cs`
- `Configuration/`
- `Formatting/`
- `Services/`

## Local Run

```bash
dotnet run
```

The service accepts exactly one query parameter:

- `queryAirportTemp=PRG`
- `queryStockPrice=IBM`
- `queryEval=%281%2B2%29*3`

By default it returns JSON. If the client sends `Accept: application/xml` or `Accept: text/xml`, it returns XML.

## Configuration

Set the stock API key with environment variable:

```bash
MARKETSTACK_API_KEY=your-key
```

The app also auto-binds to Render's `PORT` variable.

## Render

This repo contains `render.yaml`, so Render can import the service configuration automatically.

If you configure the service manually, use:

```bash
dotnet publish RestHW03.csproj -c Release -o out
```

as the build command and:

```bash
dotnet out/RestHW03.dll
```

as the start command.
