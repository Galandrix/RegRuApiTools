namespace RegRuApiTools;

internal enum AppLanguage
{
    En,
    Ru,
}

internal static class Lang
{
    public static bool Ru => AppSettings.Language == AppLanguage.Ru;

    private static string S(string en, string ru) => Ru ? ru : en;

    public static string Subtitle => S(
        "DNS zones on REG.RU via the API.",
        "Зоны DNS на REG.RU через API.");

    public static string AddRecord => S("+ Add record", "+ Добавить запись");
    public static string EditRecord => S("Edit record", "Изменить запись");
    public static string Refresh => S("Refresh", "Обновить");
    public static string Soa => S("SOA", "SOA");
    public static string Settings => S("Settings", "Настройки");
    public static string Domain => S("Domain", "Домен");

    public static string Records => S("Records", "Записи");
    public static string SelectedRecord => S("Selected record", "Выбранная запись");

    public static string ColName => S("Name", "Имя");
    public static string ColType => S("Type", "Тип");
    public static string ColValue => S("Value", "Значение");
    public static string ColPriority => S("Priority", "Приоритет");
    public static string ColState => S("State", "Состояние");

    public static string Name => S("Name:", "Имя:");
    public static string Type => S("Type:", "Тип:");
    public static string Value => S("Value:", "Значение:");
    public static string Priority => S("Priority:", "Приоритет:");
    public static string State => S("State:", "Состояние:");
    public static string SoaTtl => S("SOA TTL:", "SOA TTL:");

    public static string RecordsFound(int count) =>
        Ru ? $"Записей: {count}" : $"Records: {count}";

    public static string NoDomain => S("Domain: —", "Домен: —");
    public static string DomainStatus(string domain) =>
        Ru ? $"Домен: {domain}" : $"Domain: {domain}";

    public static string NeedCredentials => S(
        "Enter the REG.RU login and API password in Settings.",
        "Укажите логин REG.RU и пароль API в настройках.");

    public static string CopyValue => S("Copy value", "Копировать значение");
    public static string CopyName => S("Copy name", "Копировать имя");
    public static string DeleteRecord => S("Delete record", "Удалить запись");

    public static string Active => S("Active", "Активна");
    public static string Dash => "—";

    public static string DeleteConfirm(string name, string type, string value) =>
        Ru
            ? $"Удалить {type} «{name}»?\n{value}\n\nЗапись будет удалена в зоне REG.RU."
            : $"Delete {type} “{name}”?\n{value}\n\nThe record will be removed from the REG.RU zone.";

    public static string Saved => S("Saved.", "Сохранено.");
    public static string ReplaceFailed(string message) =>
        Ru
            ? $"Старая запись удалена, новая не создана.\n{message}"
            : $"The old record was removed, and the new one was not created.\n{message}";

    public static string NetworkError(string message) =>
        Ru ? $"Нет ответа от API REG.RU.\n{message}" : $"REG.RU API did not respond.\n{message}";

    public static string BadResponse => S(
        "REG.RU returned a response that is not JSON.",
        "REG.RU вернул ответ не в формате JSON.");

    public static string ApiFailed => S("REG.RU API returned an error.", "API REG.RU вернул ошибку.");

    public static string ErrAuth => S(
        "Login or API password was rejected.",
        "Логин или пароль API не принят.");

    public static string ErrIp => S(
        "This IP is not allowed in the REG.RU API settings.",
        "Этот IP не разрешён в настройках API REG.RU.");

    public static string ErrNs => S(
        "The domain is not using REG.RU nameservers, so the zone cannot be edited.",
        "Домен не на NS REG.RU, зону нельзя редактировать.");

    public static string ErrDomain => S("Domain was not found in the account.", "Домен не найден в аккаунте.");

    public static string ErrDomainName => S("Domain name is not valid.", "Имя домена некорректно.");

    public static string Language => S("Language", "Язык");
    public static string LanguageEnglish => "English";
    public static string LanguageRussian => "Русский";
    public static string Username => S("REG.RU login", "Логин REG.RU");
    public static string ApiPassword => S("API password", "Пароль API");
    public static string ShowPassword => S("Show password", "Показать пароль");
    public static string HidePassword => S("Hide password", "Скрыть пароль");
    public static string SettingsHint => S(
        "Use the API password from the REG.RU account settings, and allow this computer’s IP. The password is stored on this Windows user with DPAPI.",
        "Пароль берётся из настроек API в кабинете REG.RU, а IP этого компьютера должен быть разрешён. Пароль хранится у текущего пользователя Windows через DPAPI.");

    public static string Cancel => S("Cancel", "Отмена");
    public static string Ok => S("OK", "ОК");
    public static string RecordTitleNew => S("Add DNS record", "Добавить запись DNS");
    public static string RecordTitleEdit => S("Edit DNS record", "Изменить запись DNS");

    public static string FieldName => S("Name", "Имя");
    public static string FieldService => S("Service", "Служба");
    public static string FieldIp => S("IPv4 address", "Адрес IPv4");
    public static string FieldIpv6 => S("IPv6 address", "Адрес IPv6");
    public static string FieldCanonical => S("Canonical name", "Каноническое имя");
    public static string FieldMail => S("Mail server", "Почтовый сервер");
    public static string FieldText => S("Text", "Текст");
    public static string FieldNs => S("DNS server", "DNS-сервер");
    public static string FieldTarget => S("Target", "Назначение");
    public static string FieldPriority => S("Priority", "Приоритет");
    public static string FieldNumber => S("Record number", "Номер записи");
    public static string FieldWeight => S("Weight", "Вес");
    public static string FieldPort => S("Port", "Порт");

    public static string HintA => S("Use @ for the domain itself, or * for the rest.", "«@» — сам домен, «*» — остальные имена.");
    public static string HintCname => S("Canonical name, for example mail.example.com.", "Каноническое имя, например mail.example.com.");
    public static string HintMx => S("Mail host, for example mx.example.com. Lower priority is preferred.", "Хост почты, например mx.example.com. Меньший приоритет предпочтительнее.");
    public static string HintTxt => S("TXT value as REG.RU should store it.", "Текст TXT так, как его должен хранить REG.RU.");
    public static string HintNs => S("DNS host and a record number from 0 to 99.", "Хост DNS и номер записи от 0 до 99.");
    public static string HintSrv => S("Service like _sip._udp, then priority, weight, port, and target.", "Служба вида _sip._udp, затем приоритет, вес, порт и назначение.");
    public static string HintSpf => S("SPF text must start with v=spf1.", "Текст SPF должен начинаться с v=spf1.");

    public static string NeedName => S("Enter a name. Use @ for the domain itself.", "Введите имя. Для самого домена укажите @.");
    public static string NeedService => S("Enter a service, for example _sip._udp.", "Введите службу, например _sip._udp.");
    public static string BadName => S("The name contains characters DNS labels cannot use.", "В имени есть символы, недопустимые в DNS.");
    public static string NeedValue => S("Enter a value.", "Введите значение.");
    public static string BadIpv4 => S("Enter an IPv4 address.", "Введите адрес IPv4.");
    public static string BadIpv6 => S("Enter an IPv6 address.", "Введите адрес IPv6.");
    public static string BadHost => S("Enter a host name.", "Введите имя хоста.");
    public static string BadSpf => S("SPF text must start with v=spf1.", "Текст SPF должен начинаться с v=spf1.");
    public static string BadPort => S("Port must be from 1 to 65535.", "Порт должен быть от 1 до 65535.");
    public static string BadNsNumber => S("NS record number must be from 0 to 99.", "Номер NS-записи должен быть от 0 до 99.");

    public static string SoaTitle => S("Zone TTL", "TTL зоны");
    public static string Ttl => S("TTL", "TTL");
    public static string MinimumTtl => S("Negative TTL", "Отрицательный TTL");
    public static string SoaHint => S(
        "A number of seconds, or a number with m, w, d, or h. Example: 1d and 4h.",
        "Число секунд или число с суффиксом m, w, d, h. Пример: 1d и 4h.");

    public static string NeedTtl => S("Enter both TTL values.", "Введите оба значения TTL.");
    public static string SelectDomain => S("Choose a domain first.", "Сначала выберите домен.");
    public static string SelectRecord => S("Choose a record first.", "Сначала выберите запись.");
    public static string NothingToCopy => S("There is nothing to copy.", "Нечего копировать.");
    public static string AppTitle => "REG.RU API Tools";
}
