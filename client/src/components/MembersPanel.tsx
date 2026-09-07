import type { GroupMemberDto } from "../api/types";

interface MembersPanelProps {
  members: GroupMemberDto[];
  unreadIds: Set<string>;
  onOpenChat: (member: GroupMemberDto) => void;
}

/**
 * "اعضا" list in the hamburger drawer - every other member of the table (dealer + players sharing
 * a wallet here), each with a "چت خصوصی" button that opens PrivateChatModal for that person.
 * An unread dot shows on members who sent a DM while their thread wasn't the open modal.
 */
export function MembersPanel({ members, unreadIds, onOpenChat }: MembersPanelProps) {
  return (
    <div className="members-panel">
      <h4>اعضا</h4>
      {members.length === 0 && <p className="dealer-panel-empty">عضو دیگری در این میز نیست.</p>}
      <ul>
        {members.map((m) => (
          <li key={m.userId} className="members-panel-row">
            <span>
              {m.displayName}
              {m.isDealer ? " (دیلر)" : ""}
              {unreadIds.has(m.userId) && <span className="members-panel-unread-dot" aria-label="پیام خوانده‌نشده" />}
            </span>
            <button className="btn" onClick={() => onOpenChat(m)}>
              چت خصوصی
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}
