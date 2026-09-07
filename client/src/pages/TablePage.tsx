import { useCallback, useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { api } from "../api/client";
import { useAuthStore } from "../state/authStore";
import { useGameHub } from "../hooks/useGameHub";
import { SicBoBoard } from "../components/SicBoBoard";
import { DiceTray } from "../components/DiceTray";
import { DiceRollOverlay } from "../components/DiceRollOverlay";
import { WalletBadge } from "../components/WalletBadge";
import { ChipSelector } from "../components/ChipSelector";
import { DealerPanel } from "../components/DealerPanel";
import { DealerRoundControls } from "../components/DealerRoundControls";
import { ChatBox } from "../components/ChatBox";
import { VideoPanel } from "../components/VideoPanel";
import { TokenRequestBox } from "../components/TokenRequestBox";
import { SideDrawer } from "../components/SideDrawer";
import { LeaveConfirmDialog } from "../components/LeaveConfirmDialog";
import { RoundHistoryPanel } from "../components/RoundHistoryPanel";
import { AllBetsPanel } from "../components/AllBetsPanel";
import { RoundTimer } from "../components/RoundTimer";
import { MembersPanel } from "../components/MembersPanel";
import { PrivateChatModal } from "../components/PrivateChatModal";
import { DealerPlayerBalances } from "../components/DealerPlayerBalances";
import { TokenRequestAlertToast } from "../components/TokenRequestAlertToast";
import { BetLimitsToast } from "../components/BetLimitsToast";
import { playTokenRequestAlarm } from "../lib/notificationSound";
import type {
  BetTypeDto,
  ChatMessagePosted,
  DiceResultDto,
  GroupMemberDto,
  GroupPlayerDto,
  GroupStateDto,
  PrivateChatMessageDto,
  PrivateChatMessagePosted,
  RoundResultDto,
  TokenLimitsUpdatedDto,
  TokenRequestDto,
} from "../api/types";

const ROLL_OVERLAY_SECONDS = 5;
const ROLL_OVERLAY_MS = ROLL_OVERLAY_SECONDS * 1000;

export function TablePage() {
  const { groupId = "" } = useParams();
  const navigate = useNavigate();
  const { displayName, role } = useAuthStore();
  const isDealer = role === 1;

  const [groupState, setGroupState] = useState<GroupStateDto | null>(null);
  const [betTypes, setBetTypes] = useState<BetTypeDto[]>([]);
  const [lastDice, setLastDice] = useState<DiceResultDto | null>(null);
  const [rolling, setRolling] = useState(false);
  const [startingRound, setStartingRound] = useState(false);
  const [chipAmount, setChipAmount] = useState(5);
  const [chatMessages, setChatMessages] = useState<ChatMessagePosted[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [countdown, setCountdown] = useState<number | null>(null);
  const [pendingTokenRequests, setPendingTokenRequests] = useState<TokenRequestDto[]>([]);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [showRollOverlay, setShowRollOverlay] = useState(false);
  const pendingResultRef = useRef<RoundResultDto | null>(null);
  const revealTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [showLeaveConfirm, setShowLeaveConfirm] = useState(false);
  const [leaving, setLeaving] = useState(false);
  const [leaveError, setLeaveError] = useState<string | null>(null);
  const [historyVersion, setHistoryVersion] = useState(0);
  const [members, setMembers] = useState<GroupMemberDto[]>([]);
  const [players, setPlayers] = useState<GroupPlayerDto[]>([]);
  const [activeDm, setActiveDm] = useState<GroupMemberDto | null>(null);
  const [dmThreads, setDmThreads] = useState<Record<string, PrivateChatMessagePosted[]>>({});
  const [unreadDmIds, setUnreadDmIds] = useState<Set<string>>(new Set());
  const activeDmRef = useRef<GroupMemberDto | null>(null);
  activeDmRef.current = activeDm;
  const [tokenRequestToast, setTokenRequestToast] = useState<TokenRequestDto | null>(null);
  const tokenRequestToastTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [betLimitsToast, setBetLimitsToast] = useState<TokenLimitsUpdatedDto | null>(null);
  const betLimitsToastTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  // Set once the player clicks their own chip after the betting countdown has hit 0: cancelling is
  // blocked server-side past that point (GameRound.EnsureBetCancellable), only relocating to another
  // spot is still allowed until the dealer closes betting to roll (see EnsureBetMovable). While this
  // is set, the board is in "pick a destination spot" mode - see handleMoveBet / SicBoBoard's
  // movingBet prop.
  const [movingBet, setMovingBet] = useState<BetTypeDto | null>(null);

  const refreshState = useCallback(async () => {
    if (!groupId) return;
    try {
      const res = await api.get<GroupStateDto>(`/api/groups/${groupId}`);
      setGroupState(res.data);
    } catch {
      setError("بارگذاری اطلاعات میز ناموفق بود.");
    }
  }, [groupId]);

  const loadPendingTokenRequests = useCallback(async () => {
    if (!groupId || !isDealer) return;
    try {
      const res = await api.get<TokenRequestDto[]>(`/api/groups/${groupId}/token-requests`);
      setPendingTokenRequests(res.data);
    } catch {
      // dealer panel already surfaces errors from its own actions; silently keep the last known list
    }
  }, [groupId, isDealer]);

  const loadMembers = useCallback(async () => {
    if (!groupId) return;
    try {
      const res = await api.get<GroupMemberDto[]>(`/api/groups/${groupId}/members`);
      setMembers(res.data);
    } catch {
      // best-effort - members list is only used to open a DM, keep showing the last known list
    }
  }, [groupId]);

  // Dealer-only, shared by the always-visible DealerPlayerBalances panel and DealerPanel's
  // issue-tokens picker in the drawer - single fetch, both consumers stay in sync. Refreshed
  // below on WalletUpdated/TokenRequestResolved (a bet settling or a token top-up both change a
  // player's balance) and by the same 8s poll used for groupState, since a dealer needs this
  // number to be trustworthy at a glance without opening the drawer.
  const loadPlayers = useCallback(async () => {
    if (!groupId || !isDealer) return;
    try {
      const res = await api.get<GroupPlayerDto[]>(`/api/groups/${groupId}/players`);
      setPlayers(res.data);
    } catch {
      // best-effort - keep showing the last known balances rather than an error banner
    }
  }, [groupId, isDealer]);

  useEffect(() => {
    refreshState();
    api.get<BetTypeDto[]>("/api/bet-types").then((res) => setBetTypes(res.data));
  }, [refreshState]);

  useEffect(() => {
    loadPendingTokenRequests();
  }, [loadPendingTokenRequests]);

  useEffect(() => {
    loadMembers();
  }, [loadMembers]);

  useEffect(() => {
    loadPlayers();
  }, [loadPlayers]);

  // Opens a member's DM thread: loads the persisted history from the server the first time this
  // thread is opened in this session (cached afterwards, and topped up live by SignalR while the
  // page stays open), then shows the modal and clears any unread indicator for that member.
  const openPrivateChat = useCallback(
    async (member: GroupMemberDto) => {
      setUnreadDmIds((prev) => {
        if (!prev.has(member.userId)) return prev;
        const next = new Set(prev);
        next.delete(member.userId);
        return next;
      });
      setActiveDm(member);
      if (dmThreads[member.userId]) return;
      try {
        const res = await api.get<PrivateChatMessageDto[]>(`/api/groups/${groupId}/private-chat/${member.userId}`);
        setDmThreads((prev) => ({
          ...prev,
          [member.userId]: res.data.map((m) => ({
            groupId,
            senderId: m.senderId,
            senderName: m.senderName,
            recipientId: m.senderId === member.userId ? (useAuthStore.getState().userId ?? "") : member.userId,
            text: m.text,
            sentAt: m.sentAt,
          })),
        }));
      } catch {
        // best-effort - leave the thread empty rather than blocking the modal from opening
      }
    },
    [groupId, dmThreads],
  );

  // Safety net: SignalR push events (WalletUpdated, TokenRequestResolved, ...) are the primary
  // sync mechanism, but a dropped connection can fail to reconnect at all (seen live: "connection
  // stopped during negotiation" after a backend restart, with automatic reconnect never
  // recovering). Without this, a player's wallet balance / pending request state can go stale
  // indefinitely with no visible error. Poll as a low-frequency fallback so real state always
  // catches up within a few seconds even if the socket is dead.
  useEffect(() => {
    const interval = setInterval(() => {
      refreshState();
      loadPendingTokenRequests();
      loadPlayers();
    }, 8000);
    return () => clearInterval(interval);
  }, [refreshState, loadPendingTokenRequests, loadPlayers]);

  useEffect(() => {
    if (!groupState?.activeRound || groupState.activeRound.status !== "Betting") {
      setCountdown(null);
      return;
    }
    const endsAt = new Date(groupState.activeRound.bettingEndsAt).getTime();
    const tick = () => setCountdown(Math.max(0, Math.round((endsAt - Date.now()) / 1000)));
    tick();
    const interval = setInterval(tick, 1000);
    return () => clearInterval(interval);
  }, [groupState?.activeRound]);

  // The backend settles a round synchronously within the roll request (see RollDiceCommand), so
  // DiceRolled and RoundSettled arrive back-to-back with no real suspense gap. To give the table a
  // proper "dice are tumbling" moment for every connected user, we buffer the RoundSettled payload
  // in a ref and only apply it to visible state once the DiceRollOverlay's countdown finishes -
  // this is a purely client-side delayed reveal, nothing changes on the server/round-state side.
  const revealPendingResult = useCallback(() => {
    revealTimerRef.current = null;
    setShowRollOverlay(false);
    setRolling(false);
    const result = pendingResultRef.current;
    pendingResultRef.current = null;
    if (result) setLastDice(result.dice);
    refreshState();
    // The round that just finished is now "closed" server-side - bump this so the drawer's
    // round-history panel (mounted but hidden while closed) refetches next time it's opened.
    setHistoryVersion((v) => v + 1);
  }, [refreshState]);

  useEffect(() => {
    return () => {
      if (revealTimerRef.current) clearTimeout(revealTimerRef.current);
      if (tokenRequestToastTimerRef.current) clearTimeout(tokenRequestToastTimerRef.current);
      if (betLimitsToastTimerRef.current) clearTimeout(betLimitsToastTimerRef.current);
    };
  }, []);

  const { sendChatMessage, sendPrivateChatMessage } = useGameHub(groupId, {
    onRoundStarted: () => {
      setLastDice(null);
      setShowRollOverlay(false);
      pendingResultRef.current = null;
      setMovingBet(null);
      if (revealTimerRef.current) {
        clearTimeout(revealTimerRef.current);
        revealTimerRef.current = null;
      }
      refreshState();
    },
    onDiceRolled: () => {
      setRolling(true);
      setShowRollOverlay(true);
      pendingResultRef.current = null;
      setMovingBet(null);
      if (revealTimerRef.current) clearTimeout(revealTimerRef.current);
      revealTimerRef.current = setTimeout(revealPendingResult, ROLL_OVERLAY_MS);
    },
    onRoundSettled: (result) => {
      // Don't apply this yet - just buffer it. It gets applied by revealPendingResult once the
      // overlay's countdown finishes, so every user sees the same "tumbling" suspense window.
      pendingResultRef.current = result;
    },
    onWalletUpdated: () => {
      refreshState();
      loadPlayers();
    },
    onChatMessage: (msg) => setChatMessages((prev) => [...prev, msg]),
    onPrivateChatMessage: (msg) => {
      // Thread is keyed by "the other person" - whichever of sender/recipient isn't me.
      const myId = useAuthStore.getState().userId;
      const otherId = msg.senderId === myId ? msg.recipientId : msg.senderId;
      setDmThreads((prev) => ({ ...prev, [otherId]: [...(prev[otherId] ?? []), msg] }));
      // Only flag unread if this message arrived for a thread that isn't the currently-open modal
      // (and isn't my own outgoing message, which never needs an unread marker).
      if (msg.senderId !== myId && activeDmRef.current?.userId !== otherId) {
        setUnreadDmIds((prev) => new Set(prev).add(otherId));
      }
    },
    onTokenRequested: (request) => {
      loadPendingTokenRequests();
      // TokenRequested is broadcast to the whole table group (dealer, players, spectators alike -
      // see SignalRGameNotifier.TokenRequested), so gate the alarm/toast to the dealer only, or
      // every player would hear/see it too.
      if (isDealer) {
        playTokenRequestAlarm();
        if (tokenRequestToastTimerRef.current) clearTimeout(tokenRequestToastTimerRef.current);
        setTokenRequestToast(request);
        tokenRequestToastTimerRef.current = setTimeout(() => setTokenRequestToast(null), 8000);
      }
    },
    onTokenRequestResolved: () => {
      loadPendingTokenRequests();
      refreshState();
      loadPlayers();
    },
    onTokenLimitsUpdated: (limits) => {
      // Broadcast to the whole table group (dealer included - see SignalRGameNotifier.TokenLimitsUpdated),
      // but the dealer already sees their own change reflected instantly in DealerPanel, so only
      // announce it to players with a toast; refresh groupState for everyone so the persistent
      // min/max display near the ChipSelector stays accurate either way.
      refreshState();
      if (!isDealer) {
        if (betLimitsToastTimerRef.current) clearTimeout(betLimitsToastTimerRef.current);
        setBetLimitsToast(limits);
        betLimitsToastTimerRef.current = setTimeout(() => setBetLimitsToast(null), 8000);
      }
    },
    onBettingWindowUpdated: () => {
      // Only affects rounds started after this change (see SetBettingWindowCommand doc comment),
      // so there's nothing time-sensitive for players to react to right now - just refresh
      // groupState so the value shown in DealerPanel (and anywhere else it's surfaced) stays accurate.
      refreshState();
    },
    onReconnected: () => {
      // We may have missed WalletUpdated/TokenRequestResolved/RoundSettled etc. while
      // disconnected (e.g. a backend restart) - re-fetch everything from scratch instead of
      // trusting the last event we happened to receive.
      refreshState();
      loadPendingTokenRequests();
      loadMembers();
      loadPlayers();
    },
  });

  async function handleBet(bet: BetTypeDto, amount?: number) {
    if (!groupState?.activeRound) return;
    setError(null);
    try {
      await api.post(`/api/rounds/${groupState.activeRound.roundId}/bets`, {
        betTypeCode: bet.code,
        amount: amount ?? chipAmount,
      });
      await refreshState();
    } catch (e: unknown) {
      const resp = (e as { response?: { data?: unknown } })?.response;
      setError(typeof resp?.data === "string" ? resp.data : "ثبت شرط ناموفق بود.");
    }
  }

  // True once the betting countdown has hit 0 but the round is still Betting (dealer hasn't rolled
  // yet) - the "grace period" where the server blocks cancelling (GameRound.EnsureBetCancellable)
  // but still allows relocating a bet to another spot (EnsureBetMovable).
  const inGracePeriod = groupState?.activeRound?.status === "Betting" && countdown === 0;

  async function handleRemoveBet(bet: BetTypeDto) {
    if (!groupState?.activeRound) return;
    const matches = groupState.myBetsInActiveRound.filter(
      (b) => b.betTypeCode === bet.code && b.outcome === "Pending",
    );
    if (matches.length === 0) return;

    // Clicking the chip on the spot already selected as the move source cancels the pending move.
    if (movingBet?.code === bet.code) {
      setMovingBet(null);
      return;
    }

    if (inGracePeriod) {
      // Cancelling is blocked server-side past the countdown - enter "pick a destination spot" mode
      // instead of trying (and failing) to remove it outright.
      setMovingBet(bet);
      return;
    }

    setError(null);
    try {
      await Promise.all(matches.map((b) => api.delete(`/api/bets/${b.betId}`)));
      await refreshState();
    } catch (e: unknown) {
      const resp = (e as { response?: { data?: unknown } })?.response;
      setError(typeof resp?.data === "string" ? resp.data : "پس گرفتن شرط ناموفق بود.");
    }
  }

  // Confirms the destination spot for a bet move already in progress (movingBet set by
  // handleRemoveBet above). Moves every one of the player's pending bets on the source spot - in
  // practice there's normally just one, but chip-drag betting can stack more than one Bet row on
  // the same spot within a round.
  async function handleMoveBet(destination: BetTypeDto) {
    if (!movingBet) return;
    if (destination.code === movingBet.code) {
      setMovingBet(null);
      return;
    }
    const matches = groupState?.myBetsInActiveRound.filter(
      (b) => b.betTypeCode === movingBet.code && b.outcome === "Pending",
    );
    setMovingBet(null);
    if (!matches || matches.length === 0) return;
    setError(null);
    try {
      await Promise.all(
        matches.map((b) => api.post(`/api/bets/${b.betId}/move`, { newBetTypeCode: destination.code })),
      );
      await refreshState();
    } catch (e: unknown) {
      const resp = (e as { response?: { data?: unknown } })?.response;
      setError(typeof resp?.data === "string" ? resp.data : "جابه‌جایی شرط ناموفق بود.");
    }
  }

  async function handleStartRound() {
    setStartingRound(true);
    setError(null);
    try {
      await api.post(`/api/groups/${groupId}/rounds`);
      await refreshState();
    } catch {
      setError("شروع دور ناموفق بود.");
    } finally {
      setStartingRound(false);
    }
  }

  async function handleRollDice() {
    if (!groupState?.activeRound) return;
    setRolling(true);
    setError(null);
    try {
      await api.post(`/api/rounds/${groupState.activeRound.roundId}/roll`);
    } catch {
      setError("پرتاب تاس ناموفق بود.");
      setRolling(false);
    }
  }

  // Dealers and players with nothing left to hand over just leave directly - the confirmation
  // dialog only matters when there's an actual balance a player might otherwise walk away from.
  function handleBackClick() {
    if (!isDealer && (groupState?.myBalance ?? 0) > 0) {
      setLeaveError(null);
      setShowLeaveConfirm(true);
      return;
    }
    navigate("/lobby");
  }

  async function handleReturnAndLeave() {
    setLeaving(true);
    setLeaveError(null);
    try {
      await api.post(`/api/groups/${groupId}/return-balance`);
      navigate("/lobby");
    } catch (e: unknown) {
      const resp = (e as { response?: { data?: unknown } })?.response;
      setLeaveError(typeof resp?.data === "string" ? resp.data : "تحویل ژتون ناموفق بود.");
      setLeaving(false);
    }
  }

  if (!groupState) {
    return <div className="table-page-loading">در حال بارگذاری...</div>;
  }

  const bettingOpen = groupState.activeRound?.status === "Betting";
  const canBet = isDealer || groupState.hasActiveSubscription;

  return (
    <div className="table-page">
      <DiceRollOverlay visible={showRollOverlay} durationSeconds={ROLL_OVERLAY_SECONDS} />

      {showLeaveConfirm && (
        <LeaveConfirmDialog
          balance={groupState.myBalance}
          busy={leaving}
          error={leaveError}
          onReturnAndLeave={handleReturnAndLeave}
          onLeaveWithoutReturning={() => navigate("/lobby")}
          onCancel={() => setShowLeaveConfirm(false)}
        />
      )}

      {isDealer && tokenRequestToast && (
        <TokenRequestAlertToast
          request={tokenRequestToast}
          onView={() => {
            setDrawerOpen(true);
            setTokenRequestToast(null);
            if (tokenRequestToastTimerRef.current) clearTimeout(tokenRequestToastTimerRef.current);
          }}
          onDismiss={() => {
            setTokenRequestToast(null);
            if (tokenRequestToastTimerRef.current) clearTimeout(tokenRequestToastTimerRef.current);
          }}
        />
      )}

      {!isDealer && betLimitsToast && (
        <BetLimitsToast
          limits={betLimitsToast}
          onDismiss={() => {
            setBetLimitsToast(null);
            if (betLimitsToastTimerRef.current) clearTimeout(betLimitsToastTimerRef.current);
          }}
        />
      )}

      {activeDm && (
        <PrivateChatModal
          member={activeDm}
          messages={dmThreads[activeDm.userId] ?? []}
          onSend={(text) => sendPrivateChatMessage(activeDm.userId, displayName ?? "?", text)}
          onClose={() => setActiveDm(null)}
        />
      )}

      <header className="table-header">
        <div className="table-header-left">
          <button className="btn hamburger-btn" onClick={() => setDrawerOpen(true)} aria-label="باز کردن منو">
            ☰
            {isDealer && pendingTokenRequests.length > 0 && (
              <span className="hamburger-badge">{pendingTokenRequests.length}</span>
            )}
          </button>
          <button className="btn" onClick={handleBackClick}>
            بازگشت
          </button>
        </div>
        <div className="table-header-title">
          <h2>{displayName}</h2>
          <span className="table-header-table-name">{groupState.name}</span>
        </div>
        <WalletBadge balance={groupState.myBalance} label={isDealer ? "موجودی خانه (دیلر)" : undefined} />
      </header>

      {/* No round-status text shown here when there's no active round (used to say "دوری فعال
          نیست." - removed per explicit request: an empty/no-round table doesn't need a line
          calling that out at the top of the page). Still shows the round number/status once the
          dealer actually starts one. */}
      {groupState.activeRound && (
        <div className="table-status-row">
          <span>
            دور #{groupState.activeRound.roundNumber} — وضعیت: {groupState.activeRound.status}
          </span>
        </div>
      )}

      <VideoPanel groupId={groupId} />

      {(lastDice || !isDealer) && (
        <div className="table-top-panel">
          {lastDice && <DiceTray dice={lastDice} rolling={rolling} />}
          {!isDealer && bettingOpen && countdown !== null && (
            <RoundTimer secondsLeft={countdown} variant="player" />
          )}
          {!isDealer && <ChipSelector selected={chipAmount} onSelect={setChipAmount} balance={groupState.myBalance} />}
          {!isDealer && (
            <div className="bet-limits-display">
              محدوده شرط: {groupState.minBetTokens.toLocaleString("fa-IR")} تا{" "}
              {groupState.maxBetTokens.toLocaleString("fa-IR")} ژتون
            </div>
          )}
        </div>
      )}

      {!isDealer && !groupState.hasActiveSubscription && (
        <div className="auth-error">
          شما فقط می‌توانید این میز را تماشا کنید. برای شرط‌بندی باید اشتراک شما توسط مدیر سایت فعال شود.
        </div>
      )}

      {isDealer && (
        <DealerRoundControls
          groupState={groupState}
          secondsLeft={countdown}
          onStartRound={handleStartRound}
          onRollDice={handleRollDice}
          startingRound={startingRound}
          rollingDice={rolling}
        />
      )}

      {isDealer && <DealerPlayerBalances players={players} />}

      <div className="table-main">
        <div className="table-board-col">
          {movingBet && (
            <div className="move-bet-banner">
              <span>یک خانه دیگر را برای انتقال شرط «{movingBet.displayLabel}» انتخاب کنید.</span>
              <button className="btn" onClick={() => setMovingBet(null)}>
                انصراف
              </button>
            </div>
          )}
          <SicBoBoard
            betTypes={betTypes}
            myBets={groupState.myBetsInActiveRound}
            lastDice={lastDice}
            disabled={isDealer || !bettingOpen || !canBet}
            onBet={handleBet}
            onRemoveBet={handleRemoveBet}
            movingBet={movingBet}
            onMoveTarget={handleMoveBet}
          />
        </div>

        <div className="table-side-col">
          {!isDealer && (
            <TokenRequestBox
              groupId={groupId}
              pendingRequest={groupState.myPendingTokenRequest}
              onRequested={refreshState}
            />
          )}

          {error && <div className="auth-error">{error}</div>}
        </div>
      </div>

      <SideDrawer open={drawerOpen} onClose={() => setDrawerOpen(false)}>
        {isDealer && (
          <DealerPanel
            groupId={groupId}
            groupState={groupState}
            onRefresh={refreshState}
            pendingTokenRequests={pendingTokenRequests}
            onTokenRequestsChanged={loadPendingTokenRequests}
            players={players}
            onPlayersChanged={loadPlayers}
          />
        )}

        <ChatBox messages={chatMessages} onSend={(text) => sendChatMessage(displayName ?? "?", text)} />

        <MembersPanel members={members} unreadIds={unreadDmIds} onOpenChat={openPrivateChat} />

        <div className="my-bets-panel">
          <h4>شرط‌های من در این دور</h4>
          <ul>
            {groupState.myBetsInActiveRound.map((b) => (
              <li key={b.betId}>
                {b.betTypeCode}: {b.amount} — {b.outcome}
                {b.winAmount ? ` (+${b.winAmount})` : ""}
              </li>
            ))}
          </ul>
        </div>

        <AllBetsPanel bets={groupState.allBetsInActiveRound} />

        <RoundHistoryPanel groupId={groupId} refreshSignal={historyVersion} />
      </SideDrawer>
    </div>
  );
}
