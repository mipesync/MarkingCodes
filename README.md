# MarkingCodes

Решение состоит из двух независимых .NET 10 приложений для генерации и передачи кодов маркировки по TCP.

## Состав решения

- **MarkingCodes.Server** — генерирует коды маркировки с интервалом 500 мс и отправляет их TCP-клиенту.
- **MarkingCodes.Client** — принимает коды, сохраняет их в файл и отправляет серверу подтверждение (ACK).
- **MarkingCodes.Shared** — общие модели и протокол обмена сообщениями.
- **MarkingCodes.Tests** — модульные и интеграционные тесты.

## Технологии

- .NET 10 / C# 14
- TCP (TcpListener / TcpClient)
- Dependency Injection
- BackgroundService
- xUnit

## Запуск

Выполнить из корневой директории решения:

```bash
dotnet restore
dotnet build
```

Запустить сервер:

```bash
dotnet run --project MarkingCodes.Server
```

В отдельном терминале запустить клиент:

```bash
dotnet run --project MarkingCodes.Client
```

## Конфигурация

Настройки сервера находятся в `MarkingCodes.Server/appsettings.json`:

```json
{
  "TcpServerOptions": {
    "Address": "0.0.0.0",
    "Port": 5000,
    "GenerationIntervalMs": 500
  }
}
```

Настройки клиента находятся в `MarkingCodes.Client/appsettings.json`:

```json
{
  "TcpClientOptions": {
    "Host": "127.0.0.1",
    "Port": 5000,
    "OutputFilePath": "data/marking-codes.txt",
    "ReconnectInitialDelayMs": 1000,
    "ReconnectMaxDelayMs": 10000
  }
}
```

## Протокол обмена

Сообщения передаются в формате UTF-8, разделитель сообщений — перевод строки (`\n`).

- `DATA|{id}|{code}` — код маркировки.
- `ACK|{id}` — подтверждение получения и сохранения кода.

При отсутствии подтверждения сервер повторно отправляет сообщение. При переподключении клиента неподтверждённое сообщение отправляется повторно с тем же идентификатором.

Клиент предотвращает повторную запись уже обработанных сообщений.

## Результат

Полученные коды сохраняются в файл `data/marking-codes.txt` в формате:

```text
{ id }|{ code }
```

## Тестирование

Запуск всех тестов:

```bash
dotnet test
```

## Ограничения

Состояние неподтверждённого сообщения хранится в памяти сервера. При аварийном завершении сервера оно теряется.