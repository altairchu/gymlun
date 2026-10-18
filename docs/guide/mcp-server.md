# MCP server

Food Diary can share your food logs and products with Claude, or any other compatible [MCP](https://modelcontextprotocol.io) client, so you can ask things like "what did I eat last week?", "plan my meals for next week" or "I had a plate of dumplings for lunch, log it".

## Tools

| Tool | Scope | What it does |
|------|-------|--------------|
| `get_food_logs` | `food:read` | Food logs over a date range in order of the time eaten, with daily and range totals |
| `list_products` | `food:read` | Products in your catalogue; `favoritesOnly` lists the ones you eat often |
| `log_food` | `food:write` | Logs a product on a date at a time of day or in a meal, in grams or servings; one serving unless told otherwise |
| `delete_food_log` | `food:write` | Deletes a food log, to undo a mistake |
| `create_product` | `food:write` | Adds a product from nutrition values for one serving, in any number of categories, counted in grams or servings, marked as a favorite by default |

A client that asks for no scope is granted both. To keep a client read-only, have it request only `food:read`: it then never sees the write tools.

The server is off by default. It lives at `/mcp` on your Food Diary domain, and you turn it on with the [MCP settings in the configuration reference](deployment.md#mcp-server).

## Connecting an AI app

Claude, ChatGPT and other AI apps that support remote MCP servers connect with nothing but a name and the server URL:

1. Run the app on a public HTTPS domain. Hosted AI apps connect from their own servers rather than from your browser, so they cannot reach `localhost`. For Claude, **your server should be located in a [supported country and region](https://www.anthropic.com/supported-countries)**.
2. Set `Mcp:Enabled` and `Mcp:BaseUrl` (see the [MCP settings](deployment.md#mcp-server)) and restart the app. With the [Docker Compose setup](deployment.md#option-a-vps-with-docker-compose), uncomment `Mcp__Enabled` in `.env`, then run `docker compose up -d`.
3. In the AI app, add a custom connector with any name and the URL `<Mcp:BaseUrl>/mcp`, e.g. in Claude, [add a custom connector](https://support.claude.com/en/articles/11175166-get-started-with-custom-connectors-using-remote-mcp). Leave any client ID and secret fields empty.
4. Connect, and sign in with a Google account whose email is in `Auth:AllowedEmails`.
5. Food Diary shows the app's name and what it will be able to do. Allow it, and you are back in the AI app, connected.

The app registers itself with [OAuth Dynamic Client Registration](https://datatracker.ietf.org/doc/html/rfc7591), so the server needs no settings for it. Since anyone can register an app, Food Diary asks you before every self-registered app gets access: deny any request you did not just start yourself.

## Pre-registered clients

An MCP client that cannot register itself connects with a client ID and secret you choose instead. Give it an entry in `Mcp:Clients`, at the next free index, with its `ClientId`, `ClientSecret` (e.g. `openssl rand -hex 32`) and the `RedirectUri` it uses for its OAuth callback, then enter the ID and secret in its connector settings. A pre-registered client skips the approval page, because you configured it yourself.
