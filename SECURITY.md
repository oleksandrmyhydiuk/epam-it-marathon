# Security Policy

## Підтримувані версії (Supported Versions)

Нижче наведено версії платформи **Green & Tasty (EPAM IT Marathon 2026)**, які наразі підтримуються оновленнями безпеки:

| Версія | Компоненти | Підтримка |
| :--- | :--- | :--- |
| `1.1.x` (`main`) | Backend (.NET 10), Frontend (Angular 22), Terraform Infra, AQA Framework | :white_check_mark: Активна підтримка |
| `< 1.1.0` | Попередні ревізії | :x: Не підтримується |

---

## Архітектура безпеки проєкту (Security Architecture)

### 1. Автентифікація та Авторизація
- **Amazon Cognito User Pool (`restaurant-backend-user-pool`):**
  - Автентифікація зареєстрованих користувачів (Customer, Waiter, Admin) виконується через JWT-токени (`idToken` / `accessToken`).
  - Політика паролів вимагає щонайменше 8 символів, включаючи великі та малі літери, цифри та спеціальні символи.
  - Захищені маршрути API Gateway використовують `COGNITO_USER_POOLS` Authorizer (`cognito_authorizer`).
- **Контроль доступу на рівні ресурсів (Resource-Level Authorization):**
  - Ендпоінти керування груповим передзамовленням (`GET /guest/groups/{id}/sessions`, `GET /reservations/{id}/guest-session-id`, `POST /group-reservations/{id}/submit`, `DELETE /guest/groups/{id}/sessions/{sessionId}`) додатково перевіряють ідентифікатор користувача з JWT (`Claims["sub"]`) на відповідність власнику бронювання (`reservation.CustomerId`). Будь-які запити від інших клієнтів або офіціантів блокуються зі статусом `403 Forbidden`.
- **Гостьові сесії (Guest Sessions):**
  - Неавторизовані гості, які приєднуються за інвайт-токеном, отримують криптографічно стійкий 32-байтовий випадковий токен (`RandomNumberGenerator.GetBytes(32)`).
  - У таблиці DynamoDB `GuestSessions` зберігається **лише SHA-256 хеш** токена (`SessionTokenHash`), а не сам токен у відкритому вигляді.

### 2. Захист хмарної інфраструктури (AWS & Terraform)
- **Приватний хостинг фронтенду через CloudFront OAC:**
  - S3-бакет фронтенду (`restaurant-frontend-<account_id>`) має повністю увімкнений `BlockPublicAccess`.
  - Доступ до статичних файлів дозволений виключно сервісному принципалу CloudFront через підписані запити **Origin Access Control (SigV4)** із примусовим перенаправленням трафіку на HTTPS (`redirect-to-https`).
- **Шифрування даних (Encryption at Rest & In Transit):**
  - Усі S3-бакети (стейт Terraform, фронтенд, зображення страв/локацій, фото профілів, Swagger UI) мають увімкнене серверне шифрування за замовчуванням **SSE-S3 (`AES256`)**.
  - Бакет фотографій профілів (`restaurant-backend-dev-profile-images-<account_id>`) повністю приватний і доступний лише через тимчасові presigned URLs з AWS Lambda.
  - Черги повідомлень Amazon SQS (`restaurant-reports-queue` та DLQ) використовують кероване шифрування `sqs_managed_sse_enabled = true`.
  - Стейт Terraform у S3 зберігається з увімкненим версіонуванням, блокуванням публічного доступу та `encrypt = true`.

### 3. Керування секретами та конфіденційними даними (Secrets Management)
- Файли локальної конфігурації (`.env`, `.env.local`, `*.tfvars`, `*.tfstate`, `environment.production.ts`) внесені до `.gitignore` і **ніколи не зберігаються у системі контролю версій**.
- Для локального налаштування тестового фреймворку використовується шаблон [`aqa/restaurant-aqa-framework/.env.example`](aqa/restaurant-aqa-framework/.env.example).
- У CI/CD пайплайнах GitHub Actions ([`.github/workflows/aqa-tests.yml`](.github/workflows/aqa-tests.yml)) облікові дані передаються виключно через зашифровані **GitHub Actions Secrets** (`secrets.OWNER_PASSWORD`, `secrets.WAITER_PASSWORD` тощо).

---

## Повідомлення про вразливості (Reporting a Vulnerability)

Якщо ви виявили вразливість безпеки у коді бекенду, фронтенду, конфігураціях інфраструктури або тестовому фреймворку:

1. **Не створюйте публічний GitHub Issue** з деталями експлойту чи чутливими даними.
2. Надішліть опис знахідки через вкладку **Security → Report a vulnerability** (GitHub Private Vulnerability Reporting) у цьому репозиторії або зв'яжіться безпосередньо з автором репозиторію.
3. У повідомленні зазначте:
   - компонент та файл/ендпоінт, де виявлено проблему;
   - кроки для відтворення (Proof of Concept);
   - потенційний вплив та можливий варіант виправлення.

Звернення розглядаються протягом **48 годин**, після чого виправлення вноситься окремим патчем у гілку `main`.
