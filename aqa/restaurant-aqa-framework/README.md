# Restaurant AQA Framework — End-to-End Test Automation

## Обраний напрям та архітектурний підхід

- **Обраний напрям:** **Напрям 1 (Груповий pre-order)** — всі **3 рівні складності (Level 1, Level 2, Level 3)** для розгорнутого інтегрованого застосунку (**Backend + Frontend**).
- **Архітектурний підхід:** **BDD (Gherkin `.feature` специфікації) + Screenplay Pattern + Microsoft Playwright + xUnit (.NET 10)**.
  - **`Features/`** — BDD-специфікації сценаріїв у форматі Gherkin для всіх 3 рівнів (`Level1_GroupPreOrderParticipants.feature`, `Level2_ConsolidatedPreOrderSubmit.feature`, `Level3_RemoveGuestFromGroup.feature`).
  - **`Screenplay/Core/`** — ядро патерну Screenplay (`Actor`, `IAbility`, `ITask`, `IQuestion<T>`). Кожен учасник сценарію (`Owner`, `Andrii`, `Olena`, `Waiter`, `OtherCustomer`, `Unauthenticated`) представлений окремим ізольованим актором зі своєю пам'яттю та здібностями.
  - **`Screenplay/Abilities/`**:
    - `BrowseTheWeb` — керування ізольованими сесіями браузера Playwright (`IBrowserContext`, `IPage`), автоматичний запис скриншотів, відео та Playwright Trace при падінні тесту.
    - `CallRestApi` — типізований HTTP-клієнт для роботи з REST API бекенду (підтримує Cognito `Bearer` токени та гостьові `X-Anonymous-Token` заголовки).
  - **`Screenplay/UI/`** — локатори сторінок та компонентів (`GroupOrderPage`, `GuestJoinPage`, `WaiterPreOrderPage`, `LoginPage`), побудовані за пріоритетом `Role + Accessible Name` -> `Label` -> семантичні селектори компонентів Angular.
  - **`Screenplay/Tasks/`** — бізнес-дії акторів через UI (`UiTasks.cs`) та через API для швидкої підготовки даних і гарантованого очищення середовища (`ApiTasks.cs`).
  - **`Screenplay/Questions/`** — перевірки стану інтерфейсу (`UiQuestions.cs`) та відповідей API (`ApiQuestions.cs`).
  - **`Tests/`** — виконувані тестові набори для Рівня 1, Рівня 2 та Рівня 3 з автоматичним прибиранням створених бронювань у `DisposeAsync()`.

---

## Зафіксована поведінка для випадків, не визначених у специфікації ДЗ

Згідно з вимогами завдання, нижче зафіксовано поведінку нашої реалізації для крайових сценаріїв, які тестуються фреймворком:

| Сценарій / Випадок | Ендпоінт / Дія | Очікуваний HTTP-код | Очікувана відповідь / Поведінка |
| :--- | :--- | :--- | :--- |
| **Група без учасників** (ніхто ще не приєднався) | `GET /guest/groups/{groupReservationId}/sessions` | `200 OK` | `{"participants": [], "total": 0}` (у UI: `"Participants haven't added any dishes yet"`, Grand Total `$0.00`) |
| **Учасник без страв у кошику** | `GET /guest/groups/{groupReservationId}/sessions` | `200 OK` | Учасник присутній у `participants` з `items: []`, `total: 0`, `isSubmitted: false` (у UI: `"Still choosing"`, `"No dishes yet"`) |
| **Доступ не від Owner** (інший клієнт або офіціант з валідним JWT) | `GET .../sessions`, `GET .../guest-session-id`, `POST .../submit`, `DELETE .../sessions/{id}` | `403 Forbidden` | `{"message": "Only the reservation owner can perform this action."}` |
| **Доступ без авторизації або з гостьовим токеном** | Усі ендпоінти Owner (`cognito_authorizer`) | `401 Unauthorized` | Запит відхиляється API Gateway Cognito Authorizer / Lambda перевіркою `Claims["sub"]` |
| **Повторний submit** вже підтвердженої групи | `POST /group-reservations/{groupReservationId}/submit` | `400 Bad Request` | `{"message": "Group pre-order has already been submitted."}` (у UI кнопка `Submit Pre-Order` стає `disabled`, статус — `Sent to Restaurant`) |
| **Видалення останнього учасника з групи** | `DELETE /guest/groups/{groupReservationId}/sessions/{guestSessionId}` | `200 OK` | Повертається оновлений знімок групи: `{"participants": [], "total": 0}` |
| **Видалення учасника, який вже підтвердив кошик (`isSubmitted: true`)** | `DELETE /guest/groups/{groupReservationId}/sessions/{guestSessionId}` | `200 OK` | Сесія видаляється (доки група в статусі `Open`), повертається оновлений список `participants` та перерахований `total` |
| **Видалення учасника з іншої групи** (переплутані `groupReservationId` та `guestSessionId`) | `DELETE /guest/groups/{groupA}/sessions/{sessionFromGroupB}` | `400 Bad Request` | `{"message": "Guest session does not belong to this group reservation."}` |

---

## Налаштування перед запуском

1. Скопіюйте файл `.env.example` у `.env` (файл `.env` додано в `.gitignore` і не комітиться в репозиторій):
   ```powershell
   cd d:\Programs\marathon2026\aqa\restaurant-aqa-framework
   Copy-Item .env.example .env
   ```
2. Заповніть `.env` адресами вашого розгорнутого середовища:
   ```dotenv
   FRONTEND_BASE_URL=https://<ваш-cloudfront-домен>.cloudfront.net
   API_BASE_URL=https://<api-id>.execute-api.eu-north-1.amazonaws.com/develop
   HEADLESS=true
   BROWSER=chromium
   ```
   *(Облікові записи клієнтів генеруються динамічно з унікальним `GUID` для кожного запуску, якщо `OWNER_EMAIL` / `OTHER_CUSTOMER_EMAIL` залишені порожніми).*

3. Зберіть проєкт та встановіть браузери Playwright:
   ```powershell
   cd d:\Programs\marathon2026\aqa\restaurant-aqa-framework
   dotnet build
   pwsh bin/Debug/net10.0/playwright.ps1 install chromium
   ```

---

## Запуск тестів

### Запустити всі тести (всі 3 рівні)
```powershell
dotnet test --logger "trx;LogFileName=aqa-results.trx" --logger "html;LogFileName=aqa-report.html"
```

### Запустити тести конкретного рівня складності (за тегом `Level`)
- **1-й рівень складності** (Перегляд учасників групового pre-order):
  ```powershell
  dotnet test --filter "Level=1"
  ```
- **2-й рівень складності** (Сесія власника та підтвердження консолідованого pre-order):
  ```powershell
  dotnet test --filter "Level=2"
  ```
- **3-й рівень складності** (Видалення учасника з групи та наскрізний сценарій):
  ```powershell
  dotnet test --filter "Level=3"
  ```

### Запустити лише UI або лише API тести (за тегом `Category`)
```powershell
dotnet test --filter "Category=UI"
dotnet test --filter "Category=API"
```

---

## Звіти та артефакти виконання

- **Звіти про виконання (`TRX` та `HTML`):** зберігаються у папці `aqa/restaurant-aqa-framework/TestResults/` (`aqa-results.trx`, `aqa-report.html`).
- **Матеріали для розбору впалих тестів (скриншоти, відео, Playwright Trace):** автоматично зберігаються у `aqa/restaurant-aqa-framework/TestResults/Artifacts/`:
  - Скриншоти: `TestResults/Artifacts/<ScenarioName>_<Timestamp>.png`
  - Playwright Traces: `TestResults/Artifacts/<ScenarioName>_<Timestamp>_trace.zip` (перегляд через `pwsh bin/Debug/net10.0/playwright.ps1 show-trace <path-to-trace.zip>`)
  - Відеозаписи сесій: `TestResults/Artifacts/videos/`
- **CI/CD пайплайн:** описаний у [`.github/workflows/aqa-tests.yml`](file:///d:/Programs/marathon2026/.github/workflows/aqa-tests.yml), підтримує вибір рівня тестування при ручному запуску (`workflow_dispatch`), не зупиняє пайплайн при падінні окремого тесту (`continue-on-error: true`) та публікує звіти й артефакти у GitHub Actions Artifacts.

