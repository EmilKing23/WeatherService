# WeatherService Submission

## Repository

Paste the GitHub/GitLab repository link here after pushing:

```text
https://github.com/<your-user>/<your-repo>
```

## Required Files

- `README.md` is in the project root.
- Swagger screenshot: `submission/swagger.png`.
- Example response screenshots:
  - `submission/response-weather-day.png`
  - `submission/response-stats-requests.png`
- Example response JSON files:
  - `submission/weather-sochi-day.json`
  - `submission/weather-sochi-day-cached.json`
  - `submission/weather-sochi-week.json`
  - `submission/stats-requests.json`
  - `submission/stats-top-cities.json`

## Demo URLs

Use these while the project is running with Docker Compose on `http://localhost:8080`:

```text
http://localhost:8080/swagger
http://localhost:8080/api/weather/Sochi?date=2025-09-19
http://localhost:8080/api/weather/Sochi/week
http://localhost:8080/api/stats/requests?from=2025-09-01&to=2026-04-30&page=1&pageSize=20
http://localhost:8080/api/stats/top-cities?from=2025-09-01&to=2026-04-30&limit=10
http://localhost:8080/static/icons/clear.png
```
