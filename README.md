# SicBo Live — بازی آنلاین تاس با پخش زنده

پلتفرم بازی آنلاین **سیکبو (Sic Bo)** به‌صورت زنده و چندنفره؛ با پخش ویدیو/صدای دیلر، چت زنده و شرط‌گذاری بلادرنگ روی میز.

> ⚠️ **ژتون‌ها کاملاً مجازی و امتیازی هستند** — هیچ پرداخت واقعی، برداشت وجه یا تبدیل به ارز واقعی در این پروژه وجود ندارد و نخواهد داشت.

## معماری

پروژه با **Clean Architecture** و الگوی **CQRS** پیاده‌سازی شده است:

```
src/
├── Core/
│   ├── SicBoLive.Domain          # موجودیت‌ها، قواعد بازی، منطق تاس و تسویه شرط‌ها
│   └── SicBoLive.Application     # Command/Query handlers با MediatR
├── Infrastructure/
│   └── SicBoLive.Infrastructure  # EF Core، Repositoryها، سرویس‌های بیرونی
└── Presentation/
    └── SicBoLive.WebApi          # REST API + SignalR Hub + احراز هویت

client/                           # اپ React + TypeScript + Vite
tests/SicBoLive.Domain.Tests      # تست‌های واحد xUnit روی منطق دامنه
```

## تکنولوژی‌ها

| لایه | ابزار |
| --- | --- |
| بک‌اند | ASP.NET Core 9، MediatR، EF Core، SQLite |
| بلادرنگ | SignalR (`GameHub`) برای وضعیت میز، شرط‌ها و چت |
| ویدیو/صدا | LiveKit (SFU) — سلف‌هاست |
| احراز هویت | ASP.NET Core Identity + JWT |
| فرانت‌اند | React 18، TypeScript، Vite، PWA |
| تست | xUnit |

## قابلیت‌ها

- **موتور بازی سمت سرور**: چرخهٔ زمان‌بندی‌شدهٔ دور بازی (باز شدن شرط‌ها → بسته شدن → پرتاب تاس → تسویه)، به‌طوری‌که نتیجه هرگز از سمت کلاینت تعیین نمی‌شود.
- **انواع شرط سیکبو**: Big/Small، مجموع مشخص، جفت، سه‌تایی، ترکیب دوتایی و تک‌عدد — هرکدام با ضریب پرداخت مخصوص خود.
- **هم‌زمانی امن**: کسر و واریز ژتون به‌صورت اتمیک، بدون امکان شرط‌گذاری پس از بسته شدن پنجرهٔ شرط.
- **پخش زندهٔ دیلر** از طریق LiveKit با اتاق اختصاصی هر میز.
- **چت زندهٔ میز** روی همان کانال SignalR.
- **پنل مدیریت**: مدیریت کاربران، ژتون‌ها، میزها و مشاهدهٔ تاریخچهٔ دورها.
- **ربات تلگرام** برای اطلاع‌رسانی و درخواست شارژ ژتون توسط کاربر و تأیید مدیر.
- **PWA**: نصب روی موبایل، آیکون‌ها و رفتار آفلاین.

## اجرای محلی

```bash
# ۱) بک‌اند
cd src/Presentation/SicBoLive.WebApi
cp appsettings.example.json appsettings.json   # مقادیر را پر کنید
dotnet run                                     # http://localhost:5299

# ۲) LiveKit (برای ویدیو)
livekit-server --dev                           # ws://localhost:7880

# ۳) فرانت‌اند
cd client
npm install
npm run dev                                    # http://localhost:5173
```

> فایل `appsettings.json` و `.env` در `.gitignore` قرار دارند و هرگز نباید commit شوند. برای شروع از `appsettings.example.json` و `.env.example` استفاده کنید.

## تست

```bash
dotnet test
```
