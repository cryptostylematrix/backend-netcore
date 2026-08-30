# Scheduled Tasks Benefits

## English

The Scheduled Tasks module provides a shared mechanism for executing deferred
and recurring operations across the entire system.

- **System-wide scheduling:** Tasks are not limited to Referral Program. UI,
  Marketing, and future modules can use the same scheduler.
- **Module independence:** Each target module registers its own command-request
  factory and MassTransit consumers. New command types do not require changes to
  the scheduler.
- **Sequential execution:** Commands are executed in their declared order, and
  processing stops after the first failure.
- **Synchronous results:** MassTransit request/response allows the scheduler to
  determine whether each target operation completed successfully.
- **Explicit lifecycle:** The `active`, `error`, and `completed` statuses make
  task state and failures visible.
- **Flexible schedules:** The module supports one-time execution, fixed UTC
  intervals, and calendar-month schedules.
- **Safe retries:** Deterministic correlation IDs remain unchanged when the same
  task occurrence is retried.
- **Idempotency support:** Retries and parallel workers can safely send the same
  command when the target consumer uses its correlation ID correctly.
- **Parallel-worker safety:** PostgreSQL `xmin` optimistic concurrency ensures
  that only one worker advances a task row.
- **Operational control:** Clearing `execute_at_utc` stops a task without
  deleting it. An errored task can be returned to `active` for retry.
- **Selective Program coordination:** Program processing continues normally by
  default. A workflow explicitly adds disable and enable commands only when it
  needs exclusive maintenance for the affected Program. If such a workflow
  fails after disabling processing, the Program remains safely paused until
  recovery.
- **Dedicated storage:** Tasks are stored in a separate PostgreSQL database and
  are not coupled to the persistence lifecycle of a business module.
- **Failure diagnostics:** The command document, execution number, scheduled
  time, status, and error remain available for investigation.
- **Simple administration:** Tasks can currently be created and maintained
  directly in the database without an unnecessary CRUD API.
- **Extensibility:** New modules participate through dependency-injection and
  consumer registration rather than changes to the scheduler engine.

## Русский

Модуль Scheduled Tasks предоставляет единый механизм выполнения отложенных и
периодических операций во всей системе.

- **Общесистемное планирование:** Задачи не ограничены Referral Program. Тот же
  планировщик смогут использовать UI, Marketing и будущие модули.
- **Независимость модулей:** Каждый целевой модуль регистрирует собственную
  фабрику запросов и MassTransit consumers. Добавление новых команд не требует
  изменения планировщика.
- **Последовательное выполнение:** Команды выполняются в объявленном порядке, а
  после первой ошибки обработка прекращается.
- **Синхронный результат:** MassTransit request/response позволяет планировщику
  определить, успешно ли завершилась каждая операция целевого модуля.
- **Явный жизненный цикл:** Статусы `active`, `error` и `completed` делают
  состояние задачи и ошибки видимыми.
- **Гибкие расписания:** Поддерживаются одноразовые задачи, фиксированные
  UTC-интервалы и календарные расписания по месяцам.
- **Безопасные повторные попытки:** При повторном запуске той же итерации задачи
  используются те же детерминированные correlation ID.
- **Поддержка идемпотентности:** Повторные попытки и параллельные обработчики
  могут безопасно отправлять одну команду, если целевой consumer корректно
  использует correlation ID.
- **Безопасность параллельных обработчиков:** Оптимистичная конкуренция через
  PostgreSQL `xmin` гарантирует, что состояние строки изменит только один
  обработчик.
- **Операционное управление:** Очистка `execute_at_utc` останавливает задачу без
  удаления. Задачу со статусом `error` можно вернуть в `active` для повтора.
- **Избирательная координация программ:** По умолчанию обработка программы
  продолжается в обычном режиме. Команды выключения и включения добавляются явно
  только в те процессы, которым требуется эксклюзивное обслуживание затронутой
  программы. Если такой процесс завершается ошибкой после выключения, программа
  остаётся безопасно приостановленной до восстановления.
- **Отдельное хранилище:** Задачи находятся в собственной PostgreSQL базе и не
  связаны с жизненным циклом хранилища конкретного бизнес-модуля.
- **Диагностика ошибок:** Команды, номер выполнения, запланированное время,
  статус и текст ошибки остаются доступными для расследования.
- **Простое администрирование:** Сейчас задачи можно создавать и изменять
  непосредственно в базе данных без лишнего CRUD API.
- **Расширяемость:** Новые модули подключаются через DI-регистрацию фабрик и
  consumers без изменения ядра планировщика.
