import { useEffect, useRef, useState } from "react";
import { useAuthStore } from "../state/authStore";
import type { GroupMemberDto, PrivateChatMessagePosted } from "../api/types";

interface PrivateChatModalProps {
  member: GroupMemberDto;
  messages: PrivateChatMessagePosted[];
  onSend: (text: string) => void;
  onClose: () => void;
}

const MAX_MESSAGE_LENGTH = 300;

/**
 * One-on-one DM overlay, opened from the members list in the drawer. Structurally mirrors
 * LeaveConfirmDialog (overlay + centered card) combined with ChatBox's message-list/input pattern -
 * kept as its own component rather than generalizing ChatBox because the two have different data
 * shapes (PrivateChatMessagePosted always carries both senderId/recipientId, vs the group ChatBox's
 * flat senderName-only messages) and different "own message" detection (by id here, not by name).
 */
export function PrivateChatModal({ member, messages, onSend, onClose }: PrivateChatModalProps) {
  const userId = useAuthStore((s) => s.userId);
  const [text, setText] = useState("");
  const messagesEndRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth", block: "end" });
  }, [messages]);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const trimmed = text.trim();
    if (!trimmed) return;
    onSend(trimmed.slice(0, MAX_MESSAGE_LENGTH));
    setText("");
  }

  return (
    <div className="private-chat-overlay" role="dialog" aria-modal="true">
      <div className="private-chat-card">
        <div className="private-chat-header">
          <h3>
            چت خصوصی با {member.displayName}
            {member.isDealer ? " (دیلر)" : ""}
          </h3>
          <button className="btn private-chat-close" onClick={onClose} aria-label="بستن">
            ✕
          </button>
        </div>

        <div className="chat-box private-chat-box">
          <div className="chat-messages">
            {messages.length === 0 && <div className="chat-empty">هنوز پیامی ارسال نشده است.</div>}
            {messages.map((m, i) => {
              const isOwn = m.senderId === userId;
              return (
                <div key={i} className={`chat-message${isOwn ? " chat-message-own" : ""}`}>
                  <div className="chat-message-header">
                    <strong>{isOwn ? "شما" : m.senderName}</strong>
                    <span className="chat-message-time">
                      {new Date(m.sentAt).toLocaleTimeString("fa-IR", { hour: "2-digit", minute: "2-digit" })}
                    </span>
                  </div>
                  <div className="chat-message-text">{m.text}</div>
                </div>
              );
            })}
            <div ref={messagesEndRef} />
          </div>
          <form onSubmit={handleSubmit} className="chat-input-row">
            <input
              value={text}
              onChange={(e) => setText(e.target.value)}
              placeholder="پیام خصوصی..."
              maxLength={MAX_MESSAGE_LENGTH}
              autoFocus
            />
            <button type="submit" className="btn" disabled={!text.trim()}>
              ارسال
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
