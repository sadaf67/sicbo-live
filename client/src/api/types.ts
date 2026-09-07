export type UserRole = 0 | 1 | 2; // Admin | Dealer | Player

export interface AuthResponse {
  token: string;
  userId: string;
  displayName: string;
  role: UserRole;
}

export interface BetTypeDto {
  id: string;
  code: string;
  displayLabel: string;
  category: string;
  multiplier: number;
  faces: number[];
  requiredTotal: number | null;
}

export interface ActiveRoundDto {
  roundId: string;
  roundNumber: number;
  status: "Betting" | "Rolling" | "Result" | "Closed" | string;
  bettingEndsAt: string;
  die1: number | null;
  die2: number | null;
  die3: number | null;
}

export interface MyBetDto {
  betId: string;
  betTypeCode: string;
  amount: number;
  outcome: string;
  winAmount: number;
}

// Every player's bets in the active round (dealer + players alike, shown in the "شرط‌های همه
// بازیکنان" drawer panel) - unlike MyBetDto, this carries the bettor's identity.
export interface AllBetDto {
  betId: string;
  playerId: string;
  playerDisplayName: string;
  betTypeCode: string;
  amount: number;
  outcome: string;
  winAmount: number;
}

export interface GroupStateDto {
  groupId: string;
  name: string;
  inviteCode: string;
  minBetTokens: number;
  maxBetTokens: number;
  bettingWindowSeconds: number;
  isActive: boolean;
  myBalance: number;
  activeRound: ActiveRoundDto | null;
  myBetsInActiveRound: MyBetDto[];
  allBetsInActiveRound: AllBetDto[];
  hasActiveSubscription: boolean;
  myPendingTokenRequest: TokenRequestDto | null;
}

export interface TokenRequestDto {
  requestId: string;
  groupId: string;
  playerId: string;
  playerDisplayName: string;
  amount: number;
  status: "Pending" | "Approved" | "Rejected" | string;
  requestedAt: string;
}

export interface DiceResultDto {
  die1: number;
  die2: number;
  die3: number;
  total: number;
  isTriple: boolean;
}

export interface BetSettlementDto {
  betId: string;
  playerId: string;
  betTypeCode: string;
  amount: number;
  won: boolean;
  winAmount: number;
  newBalance: number;
}

export interface RoundResultDto {
  groupId: string;
  roundId: string;
  dice: DiceResultDto;
  settlements: BetSettlementDto[];
}

export interface RoundStartedDto {
  groupId: string;
  roundId: string;
  roundNumber: number;
  bettingEndsAt: string;
}

export interface WalletBalanceDto {
  groupId: string;
  userId: string;
  balance: number;
}

// Broadcast to the whole table whenever the dealer changes the min/max bet limits (SetTokenLimitsCommand),
// so players' UI updates live instead of only discovering the new limits when a bet gets rejected.
export interface TokenLimitsUpdatedDto {
  groupId: string;
  minBetTokens: number;
  maxBetTokens: number;
}

// Broadcast to the whole table whenever the dealer changes the per-round betting countdown
// duration (SetBettingWindowCommand) - only affects rounds started after the change.
export interface BettingWindowUpdatedDto {
  groupId: string;
  bettingWindowSeconds: number;
}

export interface RoundHistoryItemDto {
  roundId: string;
  roundNumber: number;
  dice: DiceResultDto | null;
  closedAt: string | null;
}

export interface ChatMessagePosted {
  senderId: string;
  senderName: string;
  text: string;
  sentAt: string;
}

export interface GroupMemberDto {
  userId: string;
  displayName: string;
  isDealer: boolean;
}

export interface GroupPlayerDto {
  userId: string;
  displayName: string;
  balance: number;
}

export interface PrivateChatMessagePosted {
  groupId: string;
  senderId: string;
  senderName: string;
  recipientId: string;
  text: string;
  sentAt: string;
}

// Shape returned by GET /api/groups/{groupId}/private-chat/{otherUserId} (history load) - same
// fields as the live PrivateChatMessagePosted event minus groupId/recipientId, which the REST
// call's URL already pins down.
export interface PrivateChatMessageDto {
  senderId: string;
  senderName: string;
  text: string;
  sentAt: string;
}

export interface SubscriptionPlanDto {
  id: string;
  name: string;
  entryTokenAmount: number;
  gameDurationMinutes: number;
  discountPercent: number;
  effectiveEntryAmount: number;
  isActive: boolean;
}

export interface LiveKitTokenDto {
  token: string;
  wsUrl: string;
  roomName: string;
}

export interface AdminGroupDto {
  groupId: string;
  name: string;
  inviteCode: string;
  dealerDisplayName: string;
  minBetTokens: number;
  maxBetTokens: number;
  isActive: boolean;
}

export interface PlayerAdminDto {
  userId: string;
  displayName: string;
  email: string;
  hasActiveSubscription: boolean;
  activeSubscriptionId: string | null;
  activeSubscriptionPlanId: string | null;
  activeSubscriptionExpiresAt: string | null;
  hasVerificationPhoto: boolean;
}
