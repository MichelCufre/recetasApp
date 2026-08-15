# BarFlow

Aplicación web para gestionar ingredientes, recetas, nutrición, costos, precios y producción de barritas proteicas artesanales.

## Arquitectura

- `backend/src/BarFlow.Domain`: entidades y motores de cálculo puros (`decimal`).
- `backend/src/BarFlow.Application`: contratos, DTOs y casos de uso.
- `backend/src/BarFlow.Infrastructure`: EF Core, PostgreSQL, servicios, seed y migraciones.
- `backend/src/BarFlow.Api`: REST API, Swagger, validación y manejo de errores.
- `backend/tests/BarFlow.Domain.Tests`: tests de conversiones, costo, nutrición, merma, margen, markup y objetivo proteico.
- `frontend`: Angular 16 standalone, Angular Material y diseño responsive.

Los lotes de producción guardan snapshots del nombre, cantidad, costo unitario y costo total de cada ingrediente. Por eso un cambio de precio actualiza recetas vigentes sin modificar lotes históricos.

## Fórmulas centrales

- Precio con IVA: `neto × (1 + IVA / 100)`.
- Costo unidad base: `precio con IVA / cantidad convertida a unidad base`.
- Costo de línea: `cantidad usada × costo unidad base`.
- Nutriente de línea: `gramos usados / 100 × nutriente por 100 g`.
- Por barra: `total / barras producidas`.
- Por 100 g: `total / peso final × 100`; usa peso real cuando existe y teórico en caso contrario.
- Margen: `(precio − costo) / precio × 100`.
- Markup: `(precio − costo) / costo × 100`.
- Merma real: `(peso teórico − peso real) / peso teórico × 100`.

## Puesta en marcha

Requisitos: Docker Desktop, .NET 8 SDK y Node 16+.

```powershell
docker compose up -d
dotnet tool restore
dotnet run --project backend/src/BarFlow.Api --launch-profile http
```

En otra terminal:

```powershell
cd frontend
npm install
npm start
```

- Aplicación: http://localhost:4200
- Swagger: http://localhost:5212/swagger
- Health check: http://localhost:5212/health

La API aplica la migración inicial y carga 24 ingredientes y 4 recetas editables la primera vez.

## Verificación

```powershell
dotnet test backend/tests/BarFlow.Domain.Tests
dotnet build BarFlow.sln
cd frontend
npm run build
```

Los catálogos de ingredientes y recetas ofrecen exportación CSV compatible con Excel. La estructura de servicios permite incorporar después fichas técnicas PDF e importadores sin acoplarlos a los controladores.
