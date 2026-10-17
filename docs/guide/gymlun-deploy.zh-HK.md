# gymlun 免費部署教學

gymlun 由三部分組成，全部用免費方案：

| 部分 | 放喺邊 | 免費方案 |
|------|--------|----------|
| 前端（你用嘅網頁、PWA） | Vercel，網址 `https://gymlun.vercel.app` | Hobby |
| 後端（.NET API、MCP） | Render，網址 `https://gymlun-api.onrender.com` | Free web service |
| 資料庫（PostgreSQL） | Neon | Free |

Vercel 會將 `/api`、`/signin-google`、`/mcp`、`/authorize`、`/token`、`/.well-known` 轉去 Render，所以對你同 AI 嚟講，成個 app 都喺 `gymlun.vercel.app` 一個網址度。

> 如果 `gymlun` 或者 `gymlun-api` 個名俾人用咗，Vercel / Render 會俾你另一個網址。咁就要將下面所有 `gymlun.vercel.app`、`gymlun-api.onrender.com` 換成你實際嘅網址，包括 `src/frontend/vercel.json` 同 `render.yaml`。

## 1. Neon：開資料庫

1. 去 <https://neon.com> 註冊（唔使信用卡）。
2. 開一個 project，名叫 `gymlun`，Region 揀 **AWS Asia Pacific (Singapore)**，離香港最近。
3. 喺 **Connect** 撳開連線資料，記低 host、database、user 同 password。
4. 將佢砌成呢個格式（呢個就係之後要填嘅 `ConnectionStrings__Default`）：

   ```text
   Host=<host>;Database=<database>;Username=<user>;Password=<password>;SSL Mode=Require
   ```

   注意：Neon 預設俾你嘅係 `postgresql://...` 網址格式，app 唔識讀，一定要轉成上面咁。

## 2. Google：開登入用嘅 OAuth client

1. 去 <https://console.cloud.google.com>，開一個新 project，例如 `gymlun`。
2. **APIs & Services → OAuth consent screen**：User type 揀 External，填 app 名同你嘅 email。Test users 加返你自己個 Gmail。
3. **APIs & Services → Credentials → Create credentials → OAuth client ID**：
   - Application type：Web application
   - Authorized redirect URIs：`https://gymlun.vercel.app/signin-google`
4. 記低 Client ID 同 Client secret。
5. 返去 **OAuth consent screen**，撳 **Publish app**。只用登入同 email 呢啲基本權限，唔使 Google 審核；如果停留喺 Testing，Google 每 7 日就會要你重新登入。

## 3. Render：開後端

1. 去 <https://render.com> 用 GitHub 註冊。
2. **New → Blueprint**，揀你 fork 咗嘅 `food-diary` repo。Render 會讀 `render.yaml`，開一個叫 `gymlun-api` 嘅免費 web service。Region 揀 Singapore。
3. 佢會叫你填以下幾個值：

   | 變數 | 填乜 |
   |------|------|
   | `ConnectionStrings__Default` | 第 1 步砌好嘅連線字串 |
   | `Auth__AllowedEmails__0` | 你登入用嘅 Gmail，只有呢個 email 先入到 |
   | `GoogleAuth__ClientId` | 第 2 步嘅 Client ID |
   | `GoogleAuth__ClientSecret` | 第 2 步嘅 Client secret |
   | `Mcp__Clients__0__ClientId` | 自己改一個名，例如 `claude` |
   | `Mcp__Clients__0__ClientSecret` | 一串長嘅隨機字，例如喺 terminal 打 `openssl rand -hex 32` |

4. 撳 Apply，等佢 build 完（第一次要幾分鐘）。

## 4. Vercel：開前端

1. 去 <https://vercel.com> 用 GitHub 註冊。
2. **Add New → Project**，揀你個 fork。
3. **Project Name** 填 `gymlun`，**Root Directory** 揀 `src/frontend`，其他唔使改，撳 Deploy。
4. 完成後打開 `https://gymlun.vercel.app`，用你個 Gmail 登入。
5. 喺手機用瀏覽器打開，揀「加入主畫面」，就可以當 app 咁用。

## 5. 唔俾後端瞓覺（建議）

Render 免費 web service 15 分鐘冇人用就會瞓，再用要等一分鐘左右先醒。可以用 GitHub Actions 每 10 分鐘叫醒佢一次：

1. 喺你 fork 嘅 GitHub 頁面，去 **Actions** 撳 enable。
2. **Settings → Secrets and variables → Actions → Variables**，加一個 variable：
   - Name：`KEEP_AWAKE_URL`
   - Value：`https://gymlun-api.onrender.com/healthz/live`

一個 service 成個月長開大約用 744 小時，喺 Render 每月 750 小時免費額度之內。

## 6. 連接 AI

### Claude

1. Claude → Settings → Connectors → **Add custom connector**。
2. URL：`https://gymlun.vercel.app/mcp`
3. Advanced settings 填返第 3 步嘅 `Mcp__Clients__0__ClientId` 同 `Mcp__Clients__0__ClientSecret`。
4. 撳 Connect，用你個 Gmail 登入。

之後可以同 Claude 講「我晏晝食咗一碟豬肉韭菜餃」，佢會搵你嘅常食食物，冇就幫你新增，再記錄落去。

### ChatGPT 同其他 AI

只要支援「OAuth + 預先登記 client ID 同 secret」嘅遠端 MCP，都可以用同一個方法接。每個 AI 要喺 Render 加多一組設定，用下一個號碼：

| 變數 | 填乜 |
|------|------|
| `Mcp__Clients__1__ClientId` | 例如 `chatgpt` |
| `Mcp__Clients__1__ClientSecret` | 另一串隨機字 |
| `Mcp__Clients__1__RedirectUri` | 嗰個 AI 加 connector 時顯示嘅 callback URL |

ChatGPT 要開 developer mode 先加到自訂 connector。Grok 等其他 AI 有冇得加自訂 MCP、用唔用到 OAuth，要睇佢哋當時嘅功能。

## 唔會收錢嘅提醒

- 影相認食物要 OpenAI API key，會收費，所以預設唔開。唔填 `Integrations__OpenAI__ApiKey` 就唔會用到。
- Render 免費 web service 如果超過每月 5GB 流量，又冇登記付款方式，就會停到下個月。一個人用唔會用到咁多。
