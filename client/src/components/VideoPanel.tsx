import { useEffect, useRef, useState } from "react";
import { Participant, Room, RoomEvent, Track } from "livekit-client";
import { api } from "../api/client";
import type { LiveKitTokenDto } from "../api/types";

interface VideoPanelProps {
  groupId: string;
}

interface ParticipantEntry {
  identity: string;
  name: string;
  isLocal: boolean;
}

export function VideoPanel({ groupId }: VideoPanelProps) {
  const roomRef = useRef<Room | null>(null);
  const videoEls = useRef<Record<string, HTMLVideoElement | null>>({});
  const audioEls = useRef<Record<string, HTMLAudioElement | null>>({});
  const [participants, setParticipants] = useState<ParticipantEntry[]>([]);
  // Off by default for both dealer and players on entering the table - camera/mic are never
  // auto-enabled on connect (see the effect below, which only calls room.connect(), never
  // enableCameraAndMicrophone()). Each person turns their own on/off with the buttons at will.
  const [micOn, setMicOn] = useState(false);
  const [camOn, setCamOn] = useState(false);
  const [connected, setConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [mediaWarning, setMediaWarning] = useState<string | null>(null);
  // Collapsed by default on narrow phone screens so the video grid doesn't eat into the "everything
  // fits on one screen" budget the board/dice/chips need - it lives on the main screen (not the
  // drawer) per the user's request, but starts tucked away behind a one-tap toggle on small
  // viewports. Wider screens (tablet/desktop) have room to spare, so it starts open there. The
  // connection/camera/mic themselves are unaffected by this - only the grid's visibility - so a
  // collapsed player is still seen and heard by everyone else at the table.
  const [expanded, setExpanded] = useState(() => typeof window === "undefined" || window.innerWidth > 700);

  function attachParticipantTracks(participant: Participant) {
    participant.trackPublications.forEach((pub) => {
      const track = pub.track;
      if (!track) return;
      if (track.kind === Track.Kind.Video) {
        const el = videoEls.current[participant.identity];
        if (el) track.attach(el);
      } else if (track.kind === Track.Kind.Audio && !participant.isLocal) {
        const el = audioEls.current[participant.identity];
        if (el) track.attach(el);
      }
    });
  }

  function syncParticipants(room: Room) {
    setParticipants([
      { identity: room.localParticipant.identity, name: room.localParticipant.name || "من", isLocal: true },
      ...Array.from(room.remoteParticipants.values()).map((p) => ({
        identity: p.identity,
        name: p.name || p.identity,
        isLocal: false,
      })),
    ]);
  }

  useEffect(() => {
    let cancelled = false;
    const room = new Room();
    roomRef.current = room;

    room
      .on(RoomEvent.TrackSubscribed, (_track, _pub, participant) => {
        attachParticipantTracks(participant);
        syncParticipants(room);
      })
      .on(RoomEvent.TrackUnsubscribed, () => syncParticipants(room))
      .on(RoomEvent.ParticipantConnected, () => syncParticipants(room))
      .on(RoomEvent.ParticipantDisconnected, () => syncParticipants(room))
      .on(RoomEvent.LocalTrackPublished, () => {
        attachParticipantTracks(room.localParticipant);
        syncParticipants(room);
      })
      .on(RoomEvent.Disconnected, () => setConnected(false));

    (async () => {
      try {
        const res = await api.get<LiveKitTokenDto>(`/api/groups/${groupId}/livekit-token`);
        if (cancelled) return;
        await room.connect(res.data.wsUrl, res.data.token);
      } catch {
        if (!cancelled) setError("اتصال به اتاق ویدیویی ناموفق بود.");
        return;
      }
      if (cancelled) return;
      setConnected(true);
      syncParticipants(room);
      // Deliberately no enableCameraAndMicrophone() here - joining the room only lets you see/hear
      // everyone else who has their own camera/mic on; your own stay off (and unpublished, so no
      // browser permission prompt fires) until you explicitly press the buttons below.
    })();

    return () => {
      cancelled = true;
      room.disconnect();
      roomRef.current = null;
    };
  }, [groupId]);

  async function toggleMic() {
    const room = roomRef.current;
    if (!room) return;
    const next = !micOn;
    try {
      await room.localParticipant.setMicrophoneEnabled(next);
      setMicOn(next);
      setMediaWarning(null);
    } catch {
      // Permission denied or no device - surface it but leave the button as-is so they can retry.
      setMediaWarning("دسترسی به میکروفون ممکن نشد.");
    }
  }

  async function toggleCam() {
    const room = roomRef.current;
    if (!room) return;
    const next = !camOn;
    try {
      await room.localParticipant.setCameraEnabled(next);
      setCamOn(next);
      setMediaWarning(null);
    } catch {
      setMediaWarning("دسترسی به دوربین ممکن نشد.");
    }
  }

  return (
    <div className="video-panel">
      <button
        type="button"
        className="video-panel-toggle"
        onClick={() => setExpanded((v) => !v)}
        aria-expanded={expanded}
      >
        <span>
          دوربین‌ها{connected ? ` (${participants.length})` : ""}
        </span>
        <span className="video-panel-toggle-chevron">{expanded ? "▲" : "▼"}</span>
      </button>

      {expanded && error && <div className="auth-error">{error}</div>}
      {expanded && !error && mediaWarning && <div className="video-warning">{mediaWarning}</div>}
      {/* CSS-hidden (not unmounted) while collapsed, not conditionally rendered: unmounting would
          detach the LiveKit audio tracks along with the video, muting remote participants' voices
          the moment you collapse the panel. display:none keeps <audio> playing normally - only the
          picture-in-a-box you're not looking at goes away, not the sound. */}
      <div className={expanded ? "video-grid" : "video-grid video-grid-collapsed"}>
        {participants.map((p) => (
          <div key={p.identity} className="video-tile">
            <video
              ref={(el) => {
                videoEls.current[p.identity] = el;
                const room = roomRef.current;
                if (el && room) {
                  const participant = p.isLocal ? room.localParticipant : room.remoteParticipants.get(p.identity);
                  if (participant) attachParticipantTracks(participant);
                }
              }}
              autoPlay
              playsInline
              muted={p.isLocal}
            />
            {!p.isLocal && (
              <audio
                ref={(el) => {
                  audioEls.current[p.identity] = el;
                  const room = roomRef.current;
                  if (el && room) {
                    const participant = room.remoteParticipants.get(p.identity);
                    if (participant) attachParticipantTracks(participant);
                  }
                }}
                autoPlay
              />
            )}
            <span className="video-tile-name">{p.name}</span>
          </div>
        ))}
      </div>
      {expanded && connected && (
        <div className="video-controls">
          <button className="btn" onClick={toggleMic}>
            {micOn ? "میکروفون: روشن" : "میکروفون: خاموش"}
          </button>
          <button className="btn" onClick={toggleCam}>
            {camOn ? "دوربین: روشن" : "دوربین: خاموش"}
          </button>
        </div>
      )}
    </div>
  );
}
