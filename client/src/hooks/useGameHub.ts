import { useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";
import { API_BASE_URL } from "../api/client";
import { useAuthStore } from "../state/authStore";
import type {
  BettingWindowUpdatedDto,
  ChatMessagePosted,
  DiceResultDto,
  PrivateChatMessagePosted,
  RoundResultDto,
  RoundStartedDto,
  TokenLimitsUpdatedDto,
  TokenRequestDto,
  WalletBalanceDto,
} from "../api/types";

export interface GameHubHandlers {
  onRoundStarted?: (round: RoundStartedDto) => void;
  onBettingClosed?: (roundId: string) => void;
  onDiceRolled?: (roundId: string, dice: DiceResultDto) => void;
  onRoundSettled?: (result: RoundResultDto) => void;
  onWalletUpdated?: (wallet: WalletBalanceDto) => void;
  onChatMessage?: (message: ChatMessagePosted) => void;
  onPrivateChatMessage?: (message: PrivateChatMessagePosted) => void;
  onTokenRequested?: (request: TokenRequestDto) => void;
  onTokenRequestResolved?: (request: TokenRequestDto) => void;
  /** Fired when the dealer changes the table's min/max bet limits, broadcast to everyone at the table. */
  onTokenLimitsUpdated?: (limits: TokenLimitsUpdatedDto) => void;
  /** Fired when the dealer changes the per-round betting countdown duration, broadcast to everyone at the table. */
  onBettingWindowUpdated?: (window: BettingWindowUpdatedDto) => void;
  /**
   * Fired after the underlying SignalR connection drops and successfully reconnects
   * (e.g. backend restart, brief network hiccup). A reconnect gets a brand-new connection id,
   * so any events fired while disconnected are missed, and SignalR group membership is gone
   * until we rejoin (handled internally, before this fires). Callers should treat this as
   * "state may be stale" and re-fetch anything that matters (wallet balance, pending requests,
   * round state) rather than relying on having received every event.
   */
  onReconnected?: () => void;
}

export function useGameHub(groupId: string | null, handlers: GameHubHandlers) {
  const token = useAuthStore((s) => s.token);
  const [connected, setConnected] = useState(false);
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const handlersRef = useRef(handlers);
  handlersRef.current = handlers;

  useEffect(() => {
    if (!groupId || !token) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/game`, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .build();

    connection.on("RoundStarted", (round: RoundStartedDto) => handlersRef.current.onRoundStarted?.(round));
    connection.on("BettingClosed", (roundId: string) => handlersRef.current.onBettingClosed?.(roundId));
    connection.on("DiceRolled", (roundId: string, dice: DiceResultDto) =>
      handlersRef.current.onDiceRolled?.(roundId, dice)
    );
    connection.on("RoundSettled", (result: RoundResultDto) => handlersRef.current.onRoundSettled?.(result));
    connection.on("WalletUpdated", (wallet: WalletBalanceDto) => handlersRef.current.onWalletUpdated?.(wallet));
    connection.on("ChatMessagePosted", (message: ChatMessagePosted) =>
      handlersRef.current.onChatMessage?.(message)
    );
    connection.on("PrivateChatMessagePosted", (message: PrivateChatMessagePosted) =>
      handlersRef.current.onPrivateChatMessage?.(message)
    );
    connection.on("TokenRequested", (request: TokenRequestDto) =>
      handlersRef.current.onTokenRequested?.(request)
    );
    connection.on("TokenRequestResolved", (request: TokenRequestDto) =>
      handlersRef.current.onTokenRequestResolved?.(request)
    );
    connection.on("TokenLimitsUpdated", (limits: TokenLimitsUpdatedDto) =>
      handlersRef.current.onTokenLimitsUpdated?.(limits)
    );
    connection.on("BettingWindowUpdated", (window: BettingWindowUpdatedDto) =>
      handlersRef.current.onBettingWindowUpdated?.(window)
    );

    connection.onreconnecting(() => setConnected(false));

    connection.onreconnected(() => {
      // A reconnect creates a brand-new connection id on the server, so we've fallen out of
      // the SignalR group and missed any events fired while disconnected - rejoin, then let
      // the caller re-sync state (wallet balance, pending requests, etc.) via a fresh fetch.
      connection
        .invoke("JoinGroup", groupId)
        .then(() => {
          setConnected(true);
          handlersRef.current.onReconnected?.();
        })
        .catch((err) => console.error("SignalR rejoin after reconnect failed", err));
    });

    connection.onclose(() => setConnected(false));

    connection
      .start()
      .then(() => {
        setConnected(true);
        return connection.invoke("JoinGroup", groupId);
      })
      .catch((err) => console.error("SignalR connection failed", err));

    connectionRef.current = connection;

    return () => {
      connection.invoke("LeaveGroup", groupId).catch(() => {});
      connection.stop();
      connectionRef.current = null;
      setConnected(false);
    };
  }, [groupId, token]);

  function sendChatMessage(senderName: string, text: string) {
    connectionRef.current?.invoke("SendChatMessage", groupId, senderName, text).catch(console.error);
  }

  function sendPrivateChatMessage(recipientId: string, senderName: string, text: string) {
    connectionRef.current
      ?.invoke("SendPrivateChatMessage", groupId, recipientId, senderName, text)
      .catch(console.error);
  }

  return { connected, sendChatMessage, sendPrivateChatMessage };
}
