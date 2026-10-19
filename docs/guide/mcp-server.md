# MCP server

Food Diary can share your food logs, products, weight and workouts with Claude, or any other compatible [MCP](https://modelcontextprotocol.io) client, so you can ask things like "what did I eat last week?", "plan my meals for next week" or "I had a plate of dumplings for lunch, log it". The tools cover what the app itself does, apart from photo recognition and nutrition suggestions, which an AI app does by itself.

## Tools

| Tool | Scope | What it does |
|------|-------|--------------|
| `get_food_logs` | `food:read` | Food logs over a date range in order of the time eaten, with daily and range totals |
| `list_products` | `food:read` | Products in your catalogue; `favoritesOnly` lists the ones you eat often, `categoryId` the ones in a category; `hasPhoto` tells which ones have a photo |
| `get_product_photo` | `food:read` | Shows a product's photo |
| `list_categories` | `food:read` | Categories, with how many products each one has |
| `get_weight_logs` | `food:read` | Your weight over a date range |
| `get_workouts` | `food:read` | Your workouts over a date range: each day's sets, grouped by exercise in the order you did them |
| `list_exercises` | `food:read` | Exercises you have logged, most recently done first, with when you last did each one and on how many days |
| `log_food` | `food:write` | Logs a product on a date at a time of day or in a meal, in grams or servings; one serving unless told otherwise |
| `log_foods` | `food:write` | Logs up to 20 products eaten together, at one time of day, each in grams or servings; logs none of them if any product or amount is wrong |
| `update_food_log` | `food:write` | Changes a food log's date, time, meal, amount or product, keeping whatever is not passed |
| `delete_food_logs` | `food:write` | Deletes up to 50 food logs, to undo mistakes; deletes none of them if any is not found |
| `create_product` | `food:write` | Adds a product from nutrition values for one serving, in any number of categories, counted in grams or servings, marked as a favorite by default |
| `update_product` | `food:write` | Changes a product's name, serving, nutrition values per 100 g, categories or favorite mark, keeping whatever is not passed |
| `delete_products` | `food:write` | Deletes up to 50 products together with all their food logs; deletes none of them if any is not found |
| `set_product_photo` | `food:write` | Sets or replaces a product's photo, from a photo attached in the chat or a public `https` image URL; it is shrunk to 640 px on its longest side |
| `remove_product_photo` | `food:write` | Removes a product's photo |
| `create_category` | `food:write` | Adds a category |
| `rename_category` | `food:write` | Renames a category |
| `delete_category` | `food:write` | Deletes a category; its products stay |
| `log_weight` | `food:write` | Logs your weight on a date; a date holds one weight |
| `update_weight` | `food:write` | Changes the weight logged on a date |
| `delete_weight` | `food:write` | Deletes the weight logged on a date |
| `log_workout` | `food:write` | Logs a whole workout on a date: up to 20 exercises and 100 sets in total, each set with reps and added weight in kg (0 for bodyweight); logs none of them if any is wrong. An exercise name that matches one already logged, ignoring case, keeps the logged spelling |
| `update_workout_set` | `food:write` | Changes a set's date, exercise, reps or weight, keeping whatever is not passed |
| `delete_workout_sets` | `food:write` | Deletes up to 50 sets, to undo mistakes; deletes none of them if any is not found |

AI apps such as ChatGPT ask you to approve each call that changes something, so `log_foods`, `log_workout`, `delete_food_logs`, `delete_products` and `delete_workout_sets` take a whole meal, a whole workout or a whole clean-up in one call, and one approval.

ChatGPT hands `set_product_photo` the photos you attach in the chat. Where an app cannot pass an attached photo, give it the photo's URL instead, or set the photo on the product page.

Both scopes also cover categories, weight and workouts. A client that asks for no scope is granted both. To keep a client read-only, have it request only `food:read`: it then never sees the write tools.

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
