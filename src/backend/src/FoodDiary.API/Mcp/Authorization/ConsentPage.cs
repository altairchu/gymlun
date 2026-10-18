using System.Net;

namespace FoodDiary.API.Mcp.Authorization;

internal static class ConsentPage
{
    private static readonly Dictionary<string, string> ScopeDescriptions = new()
    {
        [McpScopes.FoodRead] = "讀取你嘅食物記錄、食物清單、分類同體重",
        [McpScopes.FoodWrite] = "幫你記低、修改或者刪除食物記錄，管理食物同分類，同埋記低體重"
    };

    public static string Render(RegisteredClient client, ConsentRequest consent, string consentTicket)
    {
        var clientName = WebUtility.HtmlEncode(client.ClientName ?? "一個未有名嘅 app");
        var returnTarget = WebUtility.HtmlEncode(ReturnTarget(consent.RedirectUri));
        var email = WebUtility.HtmlEncode(consent.Access.Email);
        var ticket = WebUtility.HtmlEncode(consentTicket);
        var permissions = string.Concat(consent.Access.Scope
            .Split(' ')
            .Select(scope => $"<li>{WebUtility.HtmlEncode(ScopeDescriptions.GetValueOrDefault(scope, scope))}</li>"));

        return $$"""
            <!doctype html>
            <html lang="zh-HK">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>連接 gymlun</title>
              <style>
                body { margin: 0; font-family: system-ui, sans-serif; background: #f4f6fb; color: #1b2440; }
                main { max-width: 440px; margin: 0 auto; padding: 32px 20px; }
                h1 { font-size: 1.35rem; line-height: 1.4; margin: 0 0 16px; }
                p, li { line-height: 1.6; }
                .note { color: #5a6380; font-size: 0.95rem; }
                form { display: flex; gap: 12px; margin-top: 24px; }
                button { flex: 1; min-height: 48px; font-size: 1rem; border-radius: 10px; cursor: pointer; }
                .allow { background: #1f3a8a; color: #fff; border: none; }
                .deny { background: #fff; color: #1f3a8a; border: 1px solid #1f3a8a; }
              </style>
            </head>
            <body>
              <main>
                <h1>允許「{{clientName}}」連接你嘅 gymlun？</h1>
                <p>佢可以：</p>
                <ul>{{permissions}}</ul>
                <p class="note">允許之後會返去 <strong>{{returnTarget}}</strong>。如果你啱啱冇喺 AI app 度撳過連接，請撳「唔允許」。</p>
                <p class="note">登入帳戶：{{email}}</p>
                <form method="post" action="/authorize">
                  <input type="hidden" name="consent_ticket" value="{{ticket}}">
                  <button class="deny" type="submit" name="decision" value="deny">唔允許</button>
                  <button class="allow" type="submit" name="decision" value="allow">允許</button>
                </form>
              </main>
            </body>
            </html>
            """;
    }

    private static string ReturnTarget(string redirectUri)
    {
        var uri = new Uri(redirectUri);
        return string.IsNullOrEmpty(uri.Host) ? $"{uri.Scheme}:" : uri.Host;
    }
}
