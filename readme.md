# REG.RU API Tools

Редактор зон DNS REG.RU через REG.API 2. Список доменов аккаунта, записи зоны, создание, правка, удаление и TTL зоны.

## Требования

- Windows 10/11
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)
- Логин REG.RU, пароль API и разрешённый IP этого компьютера в настройках API
- Домен на NS REG.RU (`ns1.reg.ru` / `ns2.reg.ru`)

## Запуск

`bin/RegRuApiTools.exe`

Сборка — один файл без рантайма внутри, Desktop Runtime всё равно нужен.

Настройки: `%AppData%\RegRuApiTools\settings.json`. Пароль API записан через DPAPI текущего пользователя Windows.

## Сборка

```
dotnet publish src -c Release -r win-x64 -o bin
```

Получается один `bin/RegRuApiTools.exe` (`PublishSingleFile`, framework-dependent).

## Возможности

- домены аккаунта (`service/get_list`) и ручной ввод имени
- записи зоны: имя, тип, значение, приоритет, состояние
- A, AAAA, CNAME, MX, TXT, NS, SRV, SPF
- правка через один запрос `zone/update_records` (удаление и добавление)
- TTL зоны (`zone/update_soa`)
- язык: английский / русский (английский по умолчанию)
