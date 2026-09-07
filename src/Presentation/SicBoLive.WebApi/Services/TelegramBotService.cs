using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Domain.Entities;
using SicBoLive.Domain.Enums;
using SicBoLive.Infrastructure.Identity;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SicBoLive.WebApi.Services;

/// <summary>
/// Long-polling Telegram bot, hosted in-process alongside the WebApi (no separate service/deployment,
/// consistent with how LiveKit/JWT are configured - see appsettings.json "Telegram" section).
///
/// Flow (payment-gated registration - the account itself does NOT exist until the admin approves):
///   1. /start shows an inline "pick your game" keyboard. Only Sic Bo is wired up on this bot - the
///      other buttons (Poker/Hokm/Pasur/Ludo/SnakesAndLadders/Backgammon/Chess) exist as a preview of
///      the planned lineup and reply "coming soon", since each of those games is its own separate,
///      still-in-progress project/deployment (own solution, own database, own copy of this same bot) -
///      see CLAUDE.md's multi-game hosting notes. There is deliberately no shared account model across
///      games: whichever bot a player registers through IS their one account for that one game.
///   2. Picking Sic Bo shows an inline "pick your plan" keyboard, built live from the active
///      SubscriptionPlan rows (so admin-edited prices are always reflected, nothing hardcoded here).
///   3. Picking a plan creates/updates a <see cref="PendingRegistration"/> row and replies with the
///      admin's payment card number (Telegram:PaymentCardNumber/PaymentCardHolderName) plus an explicit
///      "leave the transfer description blank" warning, and asks for a receipt photo.
///   4. The receipt photo is forwarded to the admin's chat with "✅ تایید / ❌ رد" inline buttons
///      attached, and the player is told to wait for approval. No account exists yet at this point.
///   5. Tapping "تایید" (admin-chat-only, enforced via Telegram:AdminChatId) atomically creates the
///      ApplicationUser + PlayerSubscription together and sends the player their username/password and
///      the site link in one message. Tapping "رد" marks the request rejected and notifies the player.
///
/// PendingRegistration is persisted (not held in memory) specifically so an in-flight signup survives
/// a backend restart between "receipt sent" and "admin approved" - this dev box's watchdog restarts the
/// backend automatically after a crash (see tools/watchdog.ps1), and silently losing someone's payment
/// proof mid-flight would be a real support headache for a friends-and-family group.
///
/// Once approved, /start on the same chat just shows the existing account's username and a
/// /resetpassword pointer - it never re-runs the purchase flow for an already-registered chat.
///
/// AdminChatId bootstrap: Telegram chat IDs aren't known until someone messages the bot, so
/// "Telegram:AdminChatId" starts empty. The admin sends /myid to their own bot once, the bot replies
/// with the numeric chat ID, and that value gets pasted into appsettings.json (same plaintext-config
/// convention already used for Jwt/LiveKit secrets in this repo) followed by a restart.
///
/// This is a Singleton BackgroundService, but account creation needs UserManager&lt;ApplicationUser&gt;
/// and the DbContext, both Scoped - so every incoming update resolves its own DI scope via
/// IServiceScopeFactory rather than the service holding these as fields directly.
/// </summary>
public class TelegramBotService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramBotService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private TelegramBotClient? _botClient;

    // Single easily-changeable "which game is this" tag baked into every bot-issued username
    // (Name@GameCodeNumber, e.g. "علی@SicBo14829"). This bot only ever creates Sic Bo accounts - the
    // other 7 planned games each get their own copy of this bot/project with their own GameCode once
    // they're ready (see the class-level doc comment and CLAUDE.md).
    private const string GameCode = "SicBo";

    // Displayed on the /start game-picker. Only SicBo is live; the rest reply "coming soon" - see the
    // class-level doc comment for why (each is a separate, still-in-progress project/database).
    private static readonly (string Label, string Code, bool IsLive)[] GameMenu =
    {
        ("🎲 سیک‌بو", "sicbo", true),
        ("🃏 پوکر", "poker", false),
        ("♟️ حکم", "hokm", false),
        ("♠️ پاسور", "pasur", false),
        ("🔴 منچ", "ludo", false),
        ("🐍 مار و پله", "snakes", false),
        ("⚫ تخته نرد", "backgammon", false),
        ("♞ شطرنج", "chess", false),
    };

    public TelegramBotService(IConfiguration configuration, ILogger<TelegramBotService> logger, IServiceScopeFactory scopeFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var section = _configuration.GetSection("Telegram");
        var token = section["BotToken"];
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Telegram:BotToken is not configured - Telegram bot will not start.");
            return;
        }

        _botClient = new TelegramBotClient(token);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery },
        };

        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Telegram bot started long-polling.");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // normal shutdown
        }
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (update.CallbackQuery is { } callbackQuery)
            {
                await HandleCallbackQueryAsync(botClient, callbackQuery, cancellationToken);
                return;
            }

            var message = update.Message;
            if (message is null)
                return;

            var chatId = message.Chat.Id;
            var section = _configuration.GetSection("Telegram");
            var adminChatIdRaw = section["AdminChatId"];
            var clientUrl = section["ClientUrl"] ?? "";

            if (message.Text is { } text && text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
            {
                await HandleStartAsync(botClient, chatId, message, clientUrl, cancellationToken);
                return;
            }

            if (message.Text is { } resetCmd && resetCmd.StartsWith("/resetpassword", StringComparison.OrdinalIgnoreCase))
            {
                await HandleResetPasswordAsync(botClient, chatId, cancellationToken);
                return;
            }

            if (message.Text is { } pricesCmd && pricesCmd.StartsWith("/prices", StringComparison.OrdinalIgnoreCase))
            {
                await HandlePricesAsync(botClient, chatId, cancellationToken);
                return;
            }

            if (message.Text is { } idCmd && idCmd.StartsWith("/myid", StringComparison.OrdinalIgnoreCase))
            {
                await botClient.SendMessage(chatId, $"شناسه چت شما: {chatId}", cancellationToken: cancellationToken);
                return;
            }

            if (message.Photo is { Length: > 0 } photos)
            {
                await HandlePaymentPhotoAsync(botClient, chatId, photos, adminChatIdRaw, cancellationToken);
                return;
            }

            // Any other message: gentle nudge back to /start.
            await botClient.SendMessage(chatId, "برای راهنما دستور /start را ارسال کنید.", cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Telegram update.");
        }
    }

    // Entry point. An already-approved chat just gets its existing username shown back (+ resetpassword
    // pointer). A chat with an undecided PendingRegistration is resumed at the right step instead of
    // starting over. A brand-new chat sees the game picker - no account is created here anymore.
    private async Task HandleStartAsync(ITelegramBotClient botClient, long chatId, Message message, string clientUrl, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var existing = await userManager.Users.FirstOrDefaultAsync(u => u.TelegramChatId == chatId, cancellationToken);
        if (existing is not null)
        {
            var welcomeBack =
                $"سلام دوباره! حساب شما از قبل تایید و ساخته شده است.\n\nنام کاربری: {existing.Email}\n\n" +
                "برای ورود به سایت:\n" + (string.IsNullOrWhiteSpace(clientUrl) ? "" : $"{clientUrl}\n") +
                "\nاگر رمز عبورتان را فراموش کرده‌اید، دستور /resetpassword را ارسال کنید.";
            await botClient.SendMessage(chatId, welcomeBack, cancellationToken: cancellationToken);
            return;
        }

        var pending = await db.PendingRegistrations
            .Where(p => p.ChatId == chatId && p.ApprovedAt == null && p.RejectedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (pending is not null)
        {
            if (pending.ReceiptTelegramFileId is not null)
            {
                await botClient.SendMessage(chatId,
                    "درخواست ثبت‌نام شما ثبت شده و منتظر تایید ادمین است. لطفاً صبر کنید.",
                    cancellationToken: cancellationToken);
            }
            else
            {
                await SendCardNumberPromptAsync(botClient, chatId, cancellationToken);
            }
            return;
        }

        await botClient.SendMessage(chatId,
            "سلام! به ربات بازی‌های زنده خوش آمدید 🎲\n\nبازی خود را انتخاب کنید:",
            replyMarkup: BuildGamePickerKeyboard(),
            cancellationToken: cancellationToken);
    }

    private static InlineKeyboardMarkup BuildGamePickerKeyboard()
    {
        var rows = GameMenu
            .Select(g => new[] { InlineKeyboardButton.WithCallbackData(g.Label, $"game:{g.Code}") })
            .ToArray();
        return new InlineKeyboardMarkup(rows);
    }

    private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var data = callbackQuery.Data ?? "";

        if (data.StartsWith("game:", StringComparison.Ordinal))
        {
            await HandleGameChosenAsync(botClient, callbackQuery, data["game:".Length..], cancellationToken);
            return;
        }
        if (data.StartsWith("plan:", StringComparison.Ordinal))
        {
            await HandlePlanChosenAsync(botClient, callbackQuery, data["plan:".Length..], cancellationToken);
            return;
        }
        if (data.StartsWith("approve:", StringComparison.Ordinal))
        {
            await HandleAdminDecisionAsync(botClient, callbackQuery, data["approve:".Length..], approve: true, cancellationToken);
            return;
        }
        if (data.StartsWith("reject:", StringComparison.Ordinal))
        {
            await HandleAdminDecisionAsync(botClient, callbackQuery, data["reject:".Length..], approve: false, cancellationToken);
            return;
        }

        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
    }

    private async Task HandleGameChosenAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, string gameCode, CancellationToken cancellationToken)
    {
        if (callbackQuery.Message is not { } msg)
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            return;
        }
        var chatId = msg.Chat.Id;

        var game = GameMenu.FirstOrDefault(g => g.Code == gameCode);
        if (!game.IsLive)
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, "این بازی به‌زودی راه‌اندازی می‌شود 🙏", showAlert: true, cancellationToken: cancellationToken);
            return;
        }

        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var alreadyLinked = await userManager.Users.AnyAsync(u => u.TelegramChatId == chatId, cancellationToken);
        if (alreadyLinked)
        {
            await botClient.SendMessage(chatId, "شما قبلاً حساب فعالی دارید. برای راهنما دستور /start را دوباره بفرستید.", cancellationToken: cancellationToken);
            return;
        }

        var plans = await db.SubscriptionPlans.Where(p => p.IsActive).OrderBy(p => p.EntryTokenAmount).ToListAsync(cancellationToken);
        if (plans.Count == 0)
        {
            await botClient.SendMessage(chatId, "در حال حاضر هیچ پلن اشتراکی فعال نیست. لطفاً بعداً دوباره تلاش کنید.", cancellationToken: cancellationToken);
            return;
        }

        var rows = plans
            .Select(p => new[] { InlineKeyboardButton.WithCallbackData($"{p.Name} — {p.EffectiveEntryAmount()} ژتون", $"plan:{p.Id}") })
            .ToArray();

        await botClient.SendMessage(chatId, "پلن اشتراک خود را انتخاب کنید:", replyMarkup: new InlineKeyboardMarkup(rows), cancellationToken: cancellationToken);
    }

    private async Task HandlePlanChosenAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, string planIdRaw, CancellationToken cancellationToken)
    {
        if (callbackQuery.Message is not { } msg || !Guid.TryParse(planIdRaw, out var planId))
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            return;
        }
        var chatId = msg.Chat.Id;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var alreadyLinked = await userManager.Users.AnyAsync(u => u.TelegramChatId == chatId, cancellationToken);
        if (alreadyLinked)
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, "شما قبلاً حساب فعالی دارید.", showAlert: true, cancellationToken: cancellationToken);
            return;
        }

        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == planId && p.IsActive, cancellationToken);
        if (plan is null)
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, "این پلن دیگر در دسترس نیست.", showAlert: true, cancellationToken: cancellationToken);
            return;
        }

        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

        var pending = await db.PendingRegistrations
            .Where(p => p.ChatId == chatId && p.ApprovedAt == null && p.RejectedAt == null && p.ReceiptTelegramFileId == null)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (pending is null)
        {
            var displayName = BuildDisplayName(callbackQuery.From);
            pending = new PendingRegistration(chatId, displayName, plan.Id);
            db.PendingRegistrations.Add(pending);
        }
        else
        {
            pending.ChangePlan(plan.Id);
        }
        await db.SaveChangesAsync(cancellationToken);

        await SendCardNumberPromptAsync(botClient, chatId, cancellationToken);
    }

    private async Task SendCardNumberPromptAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        var section = _configuration.GetSection("Telegram");
        var cardNumber = section["PaymentCardNumber"];
        var cardHolder = section["PaymentCardHolderName"];

        var paymentInfo = string.IsNullOrWhiteSpace(cardNumber)
            ? "⚠️ شماره کارت هنوز توسط ادمین تنظیم نشده. لطفاً مبلغ را طبق هماهنگی قبلی پرداخت کنید."
            : $"شماره کارت: {cardNumber}" + (string.IsNullOrWhiteSpace(cardHolder) ? "" : $"\nبه نام: {cardHolder}");

        await botClient.SendMessage(chatId,
            paymentInfo + "\n\n" +
            "❗️لطفاً قسمت «توضیحات / شرح تراکنش» را در واریز خالی بگذارید.\n\n" +
            "پس از پرداخت، عکس رسید را همینجا برای ما ارسال کنید تا درخواست شما برای ادمین ارسال شود.",
            cancellationToken: cancellationToken);
    }

    // Lost-password recovery for approved accounts. Only works for chats that already have a linked
    // (i.e. admin-approved) account - anyone else is told to /start first.
    private async Task HandleResetPasswordAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.Users.FirstOrDefaultAsync(u => u.TelegramChatId == chatId, cancellationToken);
        if (user is null)
        {
            await botClient.SendMessage(chatId,
                "حساب فعالی برای این چت پیدا نشد. ابتدا دستور /start را ارسال کنید و مراحل ثبت‌نام و پرداخت را کامل کنید.",
                cancellationToken: cancellationToken);
            return;
        }

        var newPassword = CredentialGenerator.GenerateRandomPassword();
        var removeResult = await userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            _logger.LogError("Failed to remove old password for Telegram chat {ChatId}: {Errors}",
                chatId, string.Join("; ", removeResult.Errors.Select(e => e.Description)));
            await botClient.SendMessage(chatId, "متاسفانه در تغییر رمز عبور مشکلی پیش آمد. لطفاً بعداً دوباره تلاش کنید.", cancellationToken: cancellationToken);
            return;
        }

        var addResult = await userManager.AddPasswordAsync(user, newPassword);
        if (!addResult.Succeeded)
        {
            _logger.LogError("Failed to set new password for Telegram chat {ChatId}: {Errors}",
                chatId, string.Join("; ", addResult.Errors.Select(e => e.Description)));
            await botClient.SendMessage(chatId, "متاسفانه در تغییر رمز عبور مشکلی پیش آمد. لطفاً بعداً دوباره تلاش کنید.", cancellationToken: cancellationToken);
            return;
        }

        await botClient.SendMessage(chatId,
            $"رمز عبور جدید شما صادر شد.\n\nنام کاربری: {user.Email}\nرمز عبور جدید: {newPassword}\n\n" +
            "این رمز عبور را جایی امن ذخیره کنید - فقط همین یک بار نمایش داده می‌شود.",
            cancellationToken: cancellationToken);
    }

    private async Task HandlePricesAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var plans = await db.SubscriptionPlans.Where(p => p.IsActive).OrderBy(p => p.EntryTokenAmount).ToListAsync(cancellationToken);
        var text = plans.Count == 0
            ? "در حال حاضر هیچ پلن اشتراکی فعال نیست."
            : "پلن‌های اشتراک:\n" + string.Join("\n", plans.Select(p => $"- {p.Name}: {p.EffectiveEntryAmount()} ژتون"));
        await botClient.SendMessage(chatId, text, cancellationToken: cancellationToken);
    }

    // Photo handling splits two cases: an already-approved account sending a later receipt (renewal -
    // simple relay to admin, same as the old behavior, since the Admin Panel's existing "فعال‌سازی
    // اشتراک" button already handles renewals) vs. a brand-new signup with an undecided
    // PendingRegistration awaiting its first receipt (the new payment-gated flow).
    private async Task HandlePaymentPhotoAsync(ITelegramBotClient botClient, long chatId, PhotoSize[] photos, string? adminChatIdRaw, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var largest = photos[^1];
        var linkedUser = await userManager.Users.FirstOrDefaultAsync(u => u.TelegramChatId == chatId, cancellationToken);

        if (linkedUser is not null)
        {
            await RelayRenewalPhotoAsync(botClient, chatId, largest, linkedUser, adminChatIdRaw, cancellationToken);
            return;
        }

        var pending = await db.PendingRegistrations
            .Where(p => p.ChatId == chatId && p.ApprovedAt == null && p.RejectedAt == null && p.ReceiptTelegramFileId == null)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (pending is null)
        {
            await botClient.SendMessage(chatId,
                "ابتدا دستور /start را بفرستید و بازی و پلن خود را انتخاب کنید، سپس عکس رسید را ارسال کنید.",
                cancellationToken: cancellationToken);
            return;
        }

        pending.AttachReceipt(largest.FileId);
        await db.SaveChangesAsync(cancellationToken);

        if (!long.TryParse(adminChatIdRaw, out var adminChatId) || adminChatId == 0)
        {
            _logger.LogWarning("Received a payment-photo but Telegram:AdminChatId is not configured yet.");
            await botClient.SendMessage(chatId, "درخواست شما ثبت شد، اما ادمین هنوز این ربات را راه‌اندازی نکرده است. لطفاً بعداً دوباره تلاش کنید.", cancellationToken: cancellationToken);
            return;
        }

        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == pending.SubscriptionPlanId, cancellationToken);
        var caption =
            "ثبت‌نام جدید در انتظار تایید\n" +
            $"نام: {pending.DisplayName}\n" +
            $"ChatId: {chatId}\n" +
            $"پلن: {(plan is null ? "نامشخص" : $"{plan.Name} ({plan.EffectiveEntryAmount()} ژتون)")}";

        var decisionKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ تایید", $"approve:{pending.Id}"),
                InlineKeyboardButton.WithCallbackData("❌ رد", $"reject:{pending.Id}"),
            },
        });

        await botClient.SendPhoto(adminChatId, InputFile.FromFileId(largest.FileId), caption: caption, replyMarkup: decisionKeyboard, cancellationToken: cancellationToken);
        await botClient.SendMessage(chatId,
            "ثبت‌نام شما با موفقیت انجام شد ✅\nمنتظر تایید ادمین بمانید. به محض تایید، لینک بازی و اطلاعات ورود برای شما ارسال می‌شود.",
            cancellationToken: cancellationToken);
    }

    private async Task RelayRenewalPhotoAsync(ITelegramBotClient botClient, long chatId, PhotoSize largest, ApplicationUser linkedUser, string? adminChatIdRaw, CancellationToken cancellationToken)
    {
        if (long.TryParse(adminChatIdRaw, out var adminChatId) && adminChatId != 0)
        {
            var caption = $"رسید پرداخت (تمدید اشتراک)\nفرستنده: {linkedUser.DisplayName} ({linkedUser.Email})\nChatId فرستنده: {chatId}";
            await botClient.SendPhoto(adminChatId, InputFile.FromFileId(largest.FileId), caption: caption, cancellationToken: cancellationToken);
            await botClient.SendMessage(chatId, "رسید شما دریافت شد و برای ادمین ارسال گردید. پس از تایید، اشتراک شما تمدید خواهد شد.", cancellationToken: cancellationToken);
        }
        else
        {
            _logger.LogWarning("Received a renewal payment-photo but Telegram:AdminChatId is not configured yet.");
            await botClient.SendMessage(chatId, "دریافت شد، اما ادمین هنوز این ربات را راه‌اندازی نکرده است. لطفاً بعداً دوباره تلاش کنید.", cancellationToken: cancellationToken);
        }
    }

    // The admin-only decision handler behind the "✅ تایید / ❌ رد" buttons attached to a forwarded
    // receipt. Restricted to the configured AdminChatId so a random button-tapper can't approve their
    // own (or anyone else's) registration.
    private async Task HandleAdminDecisionAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, string pendingIdRaw, bool approve, CancellationToken cancellationToken)
    {
        var adminChatIdRaw = _configuration.GetSection("Telegram")["AdminChatId"];
        var callerChatId = callbackQuery.Message?.Chat.Id ?? callbackQuery.From.Id;

        if (!long.TryParse(adminChatIdRaw, out var adminChatId) || adminChatId == 0 || callerChatId != adminChatId)
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, "فقط ادمین می‌تواند این کار را انجام دهد.", showAlert: true, cancellationToken: cancellationToken);
            return;
        }

        if (!Guid.TryParse(pendingIdRaw, out var pendingId))
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var pending = await db.PendingRegistrations.FirstOrDefaultAsync(p => p.Id == pendingId, cancellationToken);
        if (pending is null)
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, "این درخواست پیدا نشد.", showAlert: true, cancellationToken: cancellationToken);
            return;
        }
        if (pending.IsDecided)
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, "قبلاً برای این درخواست تصمیم گرفته شده است.", showAlert: true, cancellationToken: cancellationToken);
            return;
        }

        if (!approve)
        {
            pending.Reject();
            await db.SaveChangesAsync(cancellationToken);
            await botClient.AnswerCallbackQuery(callbackQuery.Id, "رد شد.", cancellationToken: cancellationToken);
            await UpdateAdminDecisionMessageAsync(botClient, callbackQuery, "❌ رد شد", cancellationToken);
            await botClient.SendMessage(pending.ChatId,
                "متاسفانه رسید ارسالی شما تایید نشد. لطفاً با ادمین در ارتباط باشید یا دوباره تلاش کنید.",
                cancellationToken: cancellationToken);
            return;
        }

        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == pending.SubscriptionPlanId, cancellationToken);
        if (plan is null)
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, "پلن این درخواست دیگر موجود نیست.", showAlert: true, cancellationToken: cancellationToken);
            return;
        }

        var email = await CredentialGenerator.GenerateUniqueUsernameAsync(userManager, pending.DisplayName, GameCode, cancellationToken);
        var password = CredentialGenerator.GenerateRandomPassword();
        var user = new ApplicationUser
        {
            UserName = $"tg{pending.ChatId}",
            Email = email,
            DisplayName = pending.DisplayName,
            Role = UserRole.Player,
            TelegramChatId = pending.ChatId,
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            _logger.LogError("Failed to create account on approval for chat {ChatId}: {Errors}",
                pending.ChatId, string.Join("; ", createResult.Errors.Select(e => e.Description)));
            await botClient.AnswerCallbackQuery(callbackQuery.Id, "خطا در ساخت حساب. جزئیات در لاگ سرور.", showAlert: true, cancellationToken: cancellationToken);
            return;
        }

        var adminUser = await userManager.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Admin, cancellationToken);
        var subscription = new PlayerSubscription(user.Id, plan.Id, adminUser?.Id ?? user.Id, plan.GameDurationMinutes);
        db.PlayerSubscriptions.Add(subscription);

        pending.Approve(user.Id);
        await db.SaveChangesAsync(cancellationToken);

        var clientUrl = _configuration.GetSection("Telegram")["ClientUrl"] ?? "";
        await botClient.SendMessage(pending.ChatId,
            "🎉 پرداخت شما تایید شد و حساب شما ساخته شد!\n\n" +
            $"نام کاربری: {email}\n" +
            $"رمز عبور: {password}\n\n" +
            "این اطلاعات را جایی امن ذخیره کنید - فقط همین یک بار نمایش داده می‌شود " +
            "(در صورت فراموشی، دستور /resetpassword را بفرستید).\n\n" +
            "لینک بازی:\n" + (string.IsNullOrWhiteSpace(clientUrl) ? "(از ادمین بپرسید)" : clientUrl) + "\n\n" +
            $"اشتراک شما تا {subscription.ExpiresAt:yyyy/MM/dd HH:mm} (به وقت جهانی) فعال است.",
            cancellationToken: cancellationToken);

        await botClient.AnswerCallbackQuery(callbackQuery.Id, "تایید شد ✅", cancellationToken: cancellationToken);
        await UpdateAdminDecisionMessageAsync(botClient, callbackQuery, "✅ تایید شد", cancellationToken);
    }

    private static async Task UpdateAdminDecisionMessageAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, string decisionLabel, CancellationToken cancellationToken)
    {
        var msg = callbackQuery.Message;
        if (msg is null) return;

        try
        {
            var newCaption = (msg.Caption ?? "") + $"\n\n{decisionLabel}";
            await botClient.EditMessageCaption(msg.Chat.Id, msg.MessageId, caption: newCaption, cancellationToken: cancellationToken);
        }
        catch
        {
            // Best-effort only - if Telegram rejects the edit (e.g. message too old), the admin still
            // got the AnswerCallbackQuery toast confirming the action went through.
        }
    }

    // Person's display name from Telegram profile info (FirstName [+ LastName], falling back to
    // @username, falling back to a generic label) - becomes the human-readable half of the
    // Name@GameCodeNumber username.
    private static string BuildDisplayName(User? from)
    {
        if (from is null)
            return "کاربر";

        var name = from.FirstName;
        if (!string.IsNullOrWhiteSpace(from.LastName))
            name = string.IsNullOrWhiteSpace(name) ? from.LastName : $"{name} {from.LastName}";

        if (string.IsNullOrWhiteSpace(name))
            name = from.Username is { Length: > 0 } u ? u : "کاربر";

        return name;
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var message = exception switch
        {
            ApiRequestException apiEx => $"Telegram API error: [{apiEx.ErrorCode}] {apiEx.Message}",
            _ => exception.ToString(),
        };
        _logger.LogError("Telegram polling error: {Message}", message);
        return Task.CompletedTask;
    }
}
