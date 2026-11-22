# AtlasApple Gym Lun 接入

AtlasApple 首頁沿用 Gym Lun 入口。GymLun 記錄以 AtlasApple 帳戶嘅固定數字 ID 歸屬。帳戶登入同 MCP 授權共用呢個身份，GymLun 應用同資料庫獨立部署。

## 設定

| 位置 | 設定 | 用途 |
| --- | --- | --- |
| AtlasApple 後端 | `GYM_LUN_SSO_CLIENT_ID`、`GYM_LUN_SSO_CLIENT_SECRET` | 登記 GymLun 服務端客戶端。使用隨機密鑰，存於兩邊服務端。 |
| AtlasApple 後端 | `GYM_LUN_SSO_CALLBACK_URL` | GymLun 公開 HTTPS origin 加 `/signin-atlasapple`，完整網址要同實際回調一致。 |
| AtlasApple 前端 | `VITE_GYM_LUN_URL` | GymLun 公開 HTTPS origin。 |
| GymLun 後端 | `AtlasApple__Enabled=true`、`AtlasApple__Authority` | AtlasApple 授權模式同可信 HTTPS origin。 |
| GymLun 後端 | `AtlasApple__ClientId`、`AtlasApple__ClientSecret` | 與 AtlasApple 服務端設定一致。 |
| GymLun 後端 | `Mcp__Enabled=true`、`Mcp__BaseUrl` | MCP 公開 HTTPS origin，對應 `/mcp` 同 OAuth discovery。 |

先喺兩邊測試環境部署資料庫遷移同登入，再開啟首頁入口。反向代理要將 GymLun 嘅 `/api/*`、`/signin-atlasapple`、`/mcp`、`/authorize`、`/token`、`/register` 同 `/.well-known/*` 轉發到 GymLun API，保留公開 HTTPS scheme 同 host。

## 轉移原有日記

1. 確定原日記主人嘅 AtlasApple 帳戶 ID。同一個帳戶會接收舊食物、分類、食物相、飲食記錄、體重同訓練。
2. 暫停舊介面及舊 MCP 寫入。備份資料庫並用獨立副本做還原同遷移演練。先核對每種記錄嘅數量及關聯。
3. 用部署環境指向目標資料庫嘅 `ConnectionStrings__Default` 執行以下命令。第一次升級會將保留嘅舊記錄標記為 owner 0。第二條命令用單一交易將呢批資料歸到指定 AtlasApple 帳戶，並會檢查目標帳戶已有嘅日記資料。

   ```sh
   cd src/backend
   dotnet run --project src/FoodDiary.Migrator
   dotnet run --project src/FoodDiary.Migrator -- --claim-legacy-owner ACCOUNT_ID
   ```

4. 用原主人及另一個測試帳戶驗收。原主人應見到完整食物庫、歷史數量、相片同記錄。第二個帳戶應從空白日記開始，並可以獨立建立同名食物及同日體重。
5. 切換至新服務後重新連接 AI 客戶端，完成 AtlasApple 完整讀寫同意。舊電郵授權屬於原登入模式。

切換前保存舊部署同遷移前備份。切換後如需復原，先停止寫入並保存新資料，再為還原同記錄對帳訂定操作窗口。

## MCP 驗收

每次初始化、重新發現服務或更新 access token 後，第一個資料工具係 `get_gym_context`。回應提供目前目標、飲食方式、最新體重同每日所需估算。可以透過 `update_gym_context` 改目標，提交當前版本。原有飲食、食物、體重同訓練工具繼續按登入帳戶運作。體組成工具包括查詢、新增、修改同刪除，回傳實測值同可推算值。

用兩個 AtlasApple 帳戶同實際使用嘅 AI 客戶端驗收。由 A 嘅 MCP 新增一項測試記錄，再喺 A 嘅網站核對。用 B 查詢同嘗試改該記錄，驗證兩邊獨立。由 A 撤銷授權後，確認舊連接嘅 access 同 refresh token 都失效。對外提供部署版本嘅對應原始碼連結。
