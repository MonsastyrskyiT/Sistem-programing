namespace LocalHttpServer;

internal static class PageTemplates
{
    private const string Styles = """
        body { font-family: system-ui, sans-serif; max-width: 850px; margin: 50px auto;
               padding: 0 24px; color: #172033; background: #f4f7fb; }
        main { background: white; padding: 32px; border-radius: 16px;
               box-shadow: 0 8px 30px rgba(23, 32, 51, .08); }
        h1 { color: #2457c5; }
        nav { display: flex; gap: 12px; flex-wrap: wrap; margin: 24px 0; }
        a { color: #2457c5; }
        nav a { padding: 10px 14px; background: #eaf0ff; border-radius: 9px;
                text-decoration: none; }
        li { margin: 9px 0; }
        code { background: #eef1f5; padding: 3px 6px; border-radius: 5px; }
        """;

    public static string Home => Layout(
        "Головна сторінка",
        """
        <h1>Локальний HTTP-сервер</h1>
        <p>Вітаю! Це головна сторінка навчального HTTP-сервера на C#.</p>
        <nav>
          <a href="/autobiography">Автобіографія</a>
          <a href="/fav_countries">Улюблені країни</a>
          <a href="/pc_data">Дані комп’ютера (JSON)</a>
        </nav>
        <p>Оберіть потрібну сторінку за посиланням вище.</p>
        """);

    public static string Autobiography => Layout(
        "Автобіографія",
        """
        <h1>Автобіографія</h1>
        <p>Я — студент, який вивчає системне програмування та розробку застосунків
        мовою C#. Під час навчання я працюю з потоками, мережевими протоколами,
        бібліотеками DLL та вебтехнологіями.</p>
        <p>Моя мета — поглиблювати практичні навички програмування, створювати
        надійні програми й постійно знайомитися з новими технологіями.</p>
        <p><em>Цей текст можна замінити власними біографічними даними у файлі
        <code>PageTemplates.cs</code>.</em></p>
        <p><a href="/">← На головну</a></p>
        """);

    public static string FavoriteCountries => Layout(
        "Улюблені країни",
        """
        <h1>Мої улюблені країни</h1>
        <ul>
          <li><strong>Україна</strong> — культура, історія та мальовнича природа.</li>
          <li><strong>Іспанія</strong> — архітектура, теплий клімат і кухня.</li>
          <li><strong>Японія</strong> — поєднання традицій і сучасних технологій.</li>
          <li><strong>Італія</strong> — мистецтво, історичні міста та краєвиди.</li>
        </ul>
        <p><a href="/">← На головну</a></p>
        """);

    public static string NotFound => Layout(
        "Сторінку не знайдено",
        """
        <h1>404 — сторінку не знайдено</h1>
        <p>Перевірте адресу або поверніться на головну сторінку.</p>
        <p><a href="/">← На головну</a></p>
        """);

    private static string Layout(string title, string content) => $$"""
        <!doctype html>
        <html lang="uk">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>{{title}}</title>
          <style>{{Styles}}</style>
        </head>
        <body><main>{{content}}</main></body>
        </html>
        """;
}
