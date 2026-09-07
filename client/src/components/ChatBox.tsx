import { useEffect, useRef, useState } from "react";
import { useAuthStore } from "../state/authStore";
import type { ChatMessagePosted } from "../api/types";

interface ChatBoxProps {
  messages: ChatMessagePosted[];
  onSend: (text: string) => void;
}

const MAX_MESSAGE_LENGTH = 300;

export function ChatBox({ messages, onSend }: ChatBoxProps) {
  const { displayName } = useAuthStore();
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
    <div className="chat-box">
      <div className="chat-messages">
        {messages.length === 0 && <div className="chat-empty">هنوز پیامی ارسال نشده است.</div>}
        {messages.map((m, i) => {
          const isOwn = m.senderName === displayName;
          return (
            <div key={i} className={`chat-message${isOwn ? " chat-message-own" : ""}`}>
              <div className="chat-message-header">
                <strong>{m.senderName}</strong>
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
          placeholder="پیام..."
          maxLength={MAX_MESSAGE_LENGTH}
        />
        <button type="submit" className="btn" disabled={!text.trim()}>
          ارسال
        </button>
      </form>
    </div>
  );
}
