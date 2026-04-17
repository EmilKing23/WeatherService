# WeatherService

ASP.NET Core Web API для погоды: сервис получает данные из Open-Meteo, кэширует частые запросы в памяти, сохраняет статистику обращений в SQLite и отдает локальные иконки погоды.

## Требования

- .NET SDK 8.0
- SQLite
- Docker Desktop, если нужен запуск через Docker

## Конфигурация

Настройки лежат в `appsettings.json` и могут переопределяться переменными окружения:

- `ConnectionStrings__WeatherDatabase`
- `WeatherProvider__ForecastBaseUrl`
- `WeatherProvider__ArchiveBaseUrl`
- `WeatherProvider__GeocodingBaseUrl`
- `WeatherProvider__TimeoutSeconds`
- `WeatherProvider__RetryCount`
- `WeatherCache__TtlMinutes`

По умолчанию используется Open-Meteo:

- прогноз: `https://api.open-meteo.com/v1`
- история: `https://archive-api.open-meteo.com/v1`
- геокодинг: `https://geocoding-api.open-meteo.com/v1`

## Локальный запуск

```bash
dotnet restore
dotnet build
dotnet run
```

Swagger UI в development-режиме:

- `http://localhost:5282/swagger`
- `https://localhost:7198/swagger`

## Основные эндпоинты

- `GET /api/weather/{city}?date=YYYY-MM-DD`
- `GET /api/weather/{city}/week`
- `GET /api/stats/top-cities?from=YYYY-MM-DD&to=YYYY-MM-DD&limit=10`
- `GET /api/stats/requests?from=YYYY-MM-DD&to=YYYY-MM-DD&page=1&pageSize=20`
- `GET /static/icons/{code}.png`

Примеры:

```bash
curl "http://localhost:5282/api/weather/Malaga?date=2025-09-19"
curl "http://localhost:5282/api/weather/Malaga/week"
curl "http://localhost:5282/api/stats/top-cities?from=2026-04-01&to=2026-04-30&limit=5"
curl "http://localhost:5282/static/icons/clear.png"
```

Повторный запрос той же погоды в течение TTL должен прийти из кэша. Это видно в логах и в статистике по полю `cacheHit`.

## Тесты

```bash
dotnet test
```

Тесты проверяют:

- маппинг WMO-кодов Open-Meteo в локальные коды иконок;
- кэш-логику: первый запрос идет к провайдеру, второй такой же запрос берется из `IMemoryCache`, а в БД пишется `cacheHit=true`.

## Smoke tests

После сборки можно запустить быстрый HTTP-прогон:

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\smoke-tests.ps1
```

Скрипт стартует API на `http://127.0.0.1:5090`, проверяет дневную погоду, недельный прогноз, статистику и локальную иконку.

## Docker

Сборка и запуск:

```bash
docker compose up --build
```

После старта API будет доступен на `http://localhost:8080`. SQLite-файл хранится в docker volume `weather-data`.

## Архитектура

- `Controllers/WeatherControllers.cs` принимает HTTP-запросы погоды, валидирует входные параметры и переводит доменные ответы в JSON DTO.
- `Controllers/StatsController.cs` читает статистику из SQLite и отдает топ городов или постраничный список запросов.
- `Services/WeatherServiceLogic.cs` нормализует город, строит ключи кэша, обращается к провайдеру, сохраняет успешные и неуспешные попытки в БД.
- `Providers/OpenMeteoProvider.cs` инкапсулирует Open-Meteo: геокодинг города, недельный прогноз и историческую/прогнозную погоду на конкретную дату.
- `Mapping/WeatherCodeMapper.cs` переводит WMO-коды Open-Meteo в локальные коды иконок: `clear`, `cloudy`, `rain`, `snow`, `thunder`, `fog`.
- `wwwroot/icons` содержит локальные PNG-иконки, которые отдаются через `/static/icons/{code}.png`.
