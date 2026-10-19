# MCP server

Food Diary can share your food logs, products and weight with Claude, or any other compatible [MCP](https://modelcontextprotocol.io) client, so you can ask things like "what did I eat last week?", "plan my meals for next week" or "I had a plate of dumplings for lunch, log it". The tools cover what the app itself does, apart from photo recognition and nutrition suggestions, which an AI app does by itself.

## Tools

| Tool | Scope | What it does |
|------|-------|--------------|
| `get_food_logs` | `food:read` | Food logs over a date range in order of the time eaten, with daily and range totals |
| `list_products` | `food:read` | Products in your catalogue; `favoritesOnly` lists the ones you eat often, `categoryId` the ones in a category |
| `list_categories` | `food:read` | Categories, with how many products each one has |
| `get_weight_logs` | `food:read` | Your weight over a date range |
| `log_food` | `food:write` | Logs a product on a date at a time of day or in a meal, in grams or servings; one serving unless told otherwise |
| `log_foods` | `food:write` | Logs up to 20 products eaten together, at one time of day, each in grams or servings; logs none of them if any product or amount is wrong |
| `update_food_log` | `food:write` | Changes a food log's date, time, meal, amount or product, keeping whatever is not passed |
| `delete_food_logs` | `food:write` | Deletes up to 50 food logs, to undo mistakes; deletes none of them if any is not found |
| `create_product` | `food:write` | Adds a product from nutrition values for one serving, in any number of categories, counted in grams or servings, marked as a favorite by default |
| `update_product` | `food:write` | Changes a product's name, serving, nutrition values per 100 g, categories or favorite mark, keeping whatever is not passed |
| `delete_products` | `food:write` | Deletes up to 50 products together with all their food logs; deletes none of them if any is not found |
| `create_category` | `food:write` | Adds a category |
| `rename_category` | `food:write` | Renames a category |
| `delete_category` | `food:write` | Deletes a category; its products stay |
| `log_weight` | `food:write` | Logs your weight on a date; a date holds one weight |
| `update_weight` | `food:write` | Changes the weight logged on a date |
| `delete_weight` | `food:write` | Deletes the weight logged on a date |

AI apps such as ChatGPT ask you to approve each call that changes something, so `log_foods`, `delete_food_logs` and `delete_products` take a whole meal or a whole clean-up in one call, and one approval.

Both scopes also cover categories and weight. A client that asks for no scope is granted both. To keep a client read-only, have it request only `food:read`: it then never sees the write tools.

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
