using System;
using System.Globalization;
using System.Runtime.InteropServices;



//Warning
//Немного поменял код в плагине. В cookies сохраняется только имя-значение и путь "/"
public static class HttpCookie
{
    /// <summary>
    /// Получить все куки
    /// </summary>
    /// <returns></returns>
    [DllImport("__Internal")]
    private static extern string getHttpCookies();
    /// <summary>
    /// Получить куки по имени
    /// </summary>
    /// <param name="name">Имя куки</param>
    /// <returns>Значение куки</returns>
    [DllImport("__Internal")]
    private static extern string getHttpCookie(string name);
    /// <summary>
    /// Установить куки
    /// </summary>
    /// <param name="name">Имя куки</param>
    /// <param name="value">Значение куки</param>
    /// <param name="expires">Дата и время истечения в формате "Нед, ДД Мес ГГГГ ЧЧ:ММ:СС GMT" (например: Fri, 31 Dec 2010 23:59:59 GMT)</param>
    /// <param name="path">Путь в котором доступен куки</param>
    /// <param name="domain">Домен в котором доступен куки</param>
    /// <param name="secure">Куки могут передаваться только чрезе безопасный протокол (https)</param>
    [DllImport("__Internal")]
    private static extern void setHttpCookie(string name, string value, string expires, string path, string domain, string secure);

    /// <summary>
    /// Получить куки
    /// </summary>
    /// <param name="name">Имя куки</param>
    /// <returns>Значение куки</returns>
    public static string GetCookie(string name)
    {
        string cookie = getHttpCookie(name);
        UnityEngine.Debug.Log($"[HttpCookie] Cookie with name '{name}' was get. Value - {cookie}");
        return cookie;
    }

    /// <summary>
    /// Удалить куки
    /// </summary>
    /// <param name="name">Имя куки</param>
    public static void RemoveCookie(string name)
    {
        UnityEngine.Debug.Log($"[HttpCookie] Removing cookie with name '{name}'...");
        setHttpCookie(name, string.Empty, "0", string.Empty, string.Empty, string.Empty);
    }

    /// <summary>
    /// Установить куки
    /// </summary>
    /// <param name="name">Имя куки</param>
    /// <param name="value">Значение куки</param>
    public static void SetCookie(string name, string value)
    {
        UnityEngine.Debug.Log($"[HttpCookie] Setting cookie with name {name} and value - {value}");
        SetCookie(name, value, string.Empty, string.Empty, string.Empty, string.Empty);
    }

    /// <summary>
    /// Установить куки
    /// </summary>
    /// <param name="name">Имя куки</param>
    /// <param name="value">Значение куки</param>
    /// <param name="date">Дата и время истечения в формате "Нед, ДД Мес ГГГГ ЧЧ:ММ:СС GMT" (например: Fri, 31 Dec 2010 23:59:59 GMT)</param>
    public static void SetCookie(string name, string value, DateTime date)
    {
        UnityEngine.Debug.Log($"[HttpCookie] Setting cookie with name {name} and value - {value}. Date - {date}");
        var dateJS = date.ToUniversalTime().ToString("ddd, dd'-'MMM'-'yyyy HH':'mm':'ss 'GMT'", CultureInfo.CreateSpecificCulture("en-US"));
        SetCookie(name, value, dateJS, string.Empty, string.Empty, string.Empty);
    }

    /// <summary>
    /// Установить куки
    /// </summary>
    /// <param name="name">Имя куки</param>
    /// <param name="value">Значение куки</param>
    /// <param name="expires">Дата и время истечения в формате "Нед, ДД Мес ГГГГ ЧЧ:ММ:СС GMT" (например: Fri, 31 Dec 2010 23:59:59 GMT)</param>
    /// <param name="path">Путь в котором доступен куки</param>
    /// <param name="domain">Домен в котором доступен куки</param>
    /// <param name="secure">Куки могут передаваться только чрезе безопасный протокол (https)</param>
    public static void SetCookie(string name, string value, string expires, string path, string domain, string secure)
    {
        setHttpCookie(name, value, expires, path, domain, secure);
    }
}