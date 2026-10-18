<!-- markdownlint-disable MD024 -->

# Changelog

All notable changes to Food Diary are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- The diary home is a grid of every food: filter it by several categories at once (only foods in all of them show) or by favourites, pick foods, set each one's amount in servings or grams, and log them together at a time picked from meal shortcuts
- The day is a 0:00–23:59 timeline; drag a food, or a whole meal, to another time
- Foods can be in several categories, and each food is counted either by weight or by servings
- The MCP `log_food` tool takes a time of day and a number of servings; `create_product` takes several categories and how the food is counted
- AI apps such as Claude or ChatGPT connect to the MCP server with just a name and its URL: they register themselves (OAuth Dynamic Client Registration), and you allow each one on an approval page before it gets access
- MCP tools for everything else the app does: change a food log (`update_food_log`), change or delete a product (`update_product`, `delete_product`), list, add, rename and delete categories (`list_categories`, `create_category`, `rename_category`, `delete_category`), and read and log your weight (`get_weight_logs`, `log_weight`); `list_products` lists one category's products with `categoryId`

### Changed

- Food logs have a time of day; existing ones get their meal's usual time
- `Mcp:Clients` is optional; it is needed only for an MCP client that cannot register itself

### Removed

- The per-meal food log lists, and with them the photo recognition dialog; the food grid and the timeline replace them

### Fixed

- Category names of one or two characters, such as Chinese names, are accepted
- Deleting a category no longer deletes its foods and their food logs
- Food logs, foods, categories and weight changed elsewhere, such as through an AI app, show when you open a page or come back to the app, not only after a reload

## [0.9.2] - Router & Zod upgrades, AI suggestion fixes

### Changed

- Default photo recognition model is now `gpt-6-luna`, and recognition logs now include the model used
- Migrated frontend routing to TanStack Router
- Upgraded frontend form validation to Zod v4

### Fixed

- AI nutrition suggestions now fill nutrition fields that were cleared on the product form, instead of treating them as already filled
- The nutrition summary bar no longer covers the update app banner
- Disabled Npgsql GSS encryption in the compose connection strings, which removes a harmless `libgssapi_krb5` error logged at container startup

## [0.9.1] - Security fixes & CI hardening

### Security

- Restricted the build workflow's `GITHUB_TOKEN` permissions to `contents: read`
- Stopped logging user email addresses during auth status checks
- Resolved frontend Dependabot alerts by refreshing stale dependencies and bumping vulnerable transitive packages

### Fixed

- CI and docs now read the .NET SDK version from `global.json` instead of a separately pinned value, so it no longer drifts on SDK bumps

## [0.9.0] - MCP server & self-hosting setup

### Added

- Read-only MCP server at `/mcp` that lets Claude or any other MCP client read your food logs and products, with a built-in OAuth layer. It is off by default and enabled through the `Mcp__*` settings; see `docs/guide/mcp-server.md`
- `deploy/compose.yml`, a ready-made self-hosting setup that runs the released image with PostgreSQL and a Caddy reverse proxy that gets HTTPS certificates automatically

### Changed

- Hosting moved from Amvera Cloud to a self-hosted VPS running Dokploy
- The Docker image now applies database migrations on start, before the API. A container whose `ConnectionStrings__Default` is unset or unreachable now exits non-zero instead of starting and serving
- Reorganized the documentation: the README now focuses on what the app is and how to get started, with deployment, configuration, MCP, development and release guides moved to `docs/` and `CONTRIBUTING.md`
- Backend component tests now use Testcontainers v4

### Removed

- The root `docker-compose.yml`, which built the image from source for `localhost` only. Self-host with `deploy/compose.yml` instead

## [0.8.0] - React 19, MUI v9 & AI photo zoom

### Added

- Pan and zoom the suggested photo when adding a note via AI-based product recognition

### Changed

- Upgraded MUI from v6 to v9
- Selecting a day in the date switcher now requires confirming with OK, due to the MUI v9 upgrade
- Upgraded frontend to React 19
- Migrated frontend lint tooling to ESLint 10's flat config format

## [0.7.0] - Deployment configuration changes

### Changed

- `Dockerfile` now uses `CMD` instead of `ENTRYPOINT` for launching the application

### Removed

- `App__ForwardHttpsSchemeManuallyForAllRequests` configuration option — the HTTPS
  scheme behind a reverse proxy is now handled by ASP.NET Core's
  `ASPNETCORE_FORWARDEDHEADERS_ENABLED`
- `App__Logging__UseYandexCloudLogsFormat` configuration option and its log formatter

## [0.6.1] - Backend dependency cleanup & .NET 10 migration

### Changed

- Backend runtime requirement bumped from .NET 8 to .NET 10
- Backend dependencies centralized via NuGet central package management; compiler warnings are now treated as errors
- Removed MediatR, AutoMapper, and FluentAssertions in favor of plain C# equivalents

## [0.6.0] - Product AI nutrition suggestions

### Added

- AI-powered nutrition suggestions on the product form: suggest calories, protein, fat, and carb values from a product name

### Changed

- Nutrition input measurement units moved from input adornments to field labels, freeing up input space

## [0.5.0] - Node.js 24 & minor packages bump

### Added

- Supply chain attacks protection via `npmMinimalAgeGate`

### Changed

- Updated yarn to 4.16.0
- Switched back from pnp to node_modules for compatibility with latest Node.js
- Updated Node.js runtime requirement to 24+

### Fixed

- Existing dependencies are bumped to the latest minor / patch versions
- Existing GHA workflows no longer show Node.js 20 deprecation warnings

## [0.4.0] - AI recognition improvements and release automation

### Added

- Release automation via GitHub Actions: automated tagging, Docker Hub publishing, and GitHub release creation
- Configurable OpenAI model via `Integrations:OpenAI:Model` setting (defaults to `gpt-5.4-mini`)

### Changed

- Calories now use `decimal` precision to preserve product-label accuracy
- Image content-type validation is now case-insensitive per RFC 2045 (accepts `image/JPEG`, `IMAGE/PNG`, etc.)
- Improved food recognition prompts with explicit per-rule guidelines for handling labels, product photos, and multiple images

### Fixed

- Note recognition no longer crashes when OpenAI returns fractional calorie values
- Note recognition no longer crashes when OpenAI returns string values in numeric fields
- Images with no recognizable food product now return HTTP 400 (user error) instead of HTTP 500 (server error), with a clear validation message
- AI recognition is now ~4x faster
