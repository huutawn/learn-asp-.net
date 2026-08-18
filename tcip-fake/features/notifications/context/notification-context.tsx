"use client";

import React, {
  createContext,
  useContext,
  useEffect,
  useState,
  useCallback,
  useRef,
} from "react";
import { NotificationResponse } from "@/features/calendar/types/calendar.types";
import { calendarService } from "@/features/calendar/services/calendar.service";
import { useAuth } from "@/features/auth/context/auth-context";
import { API_BASE_URL, STORAGE_KEYS } from "@/lib/constants";

interface NotificationContextType {
  notifications: NotificationResponse[];
  unreadCount: number;
  isConnected: boolean;
  latestNotification: NotificationResponse | null;
  markAsRead: (id: string) => Promise<void>;
  markAllAsRead: () => Promise<void>;
  dismissLatestNotification: () => void;
  refreshNotifications: () => Promise<void>;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

export function NotificationProvider({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth();
  const [notifications, setNotifications] = useState<NotificationResponse[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [isConnected, setIsConnected] = useState(false);
  const [latestNotification, setLatestNotification] = useState<NotificationResponse | null>(null);

  const socketRef = useRef<WebSocket | null>(null);
  const pingIntervalRef = useRef<NodeJS.Timeout | null>(null);
  const reconnectTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  const connectWebSocketRef = useRef<(() => void) | null>(null);

  const loadNotifications = useCallback(async () => {
    if (!isAuthenticated) {
      setNotifications([]);
      setUnreadCount(0);
      return;
    }

    try {
      const items = await calendarService.getNotifications();
      setNotifications(items);
      const unread = items.filter((n) => !n.readAt).length;
      setUnreadCount(unread);
    } catch {
      // Ignored
    }
  }, [isAuthenticated]);

  const connectWebSocket = useCallback(() => {
    if (typeof window === "undefined" || !isAuthenticated) return;

    const token = localStorage.getItem(STORAGE_KEYS.ACCESS_TOKEN);
    if (!token) return;

    if (socketRef.current) {
      socketRef.current.close();
      socketRef.current = null;
    }

    // Convert http(s):// to ws(s)://
    const wsBaseUrl = API_BASE_URL.replace(/^http/, "ws");
    const wsUrl = `${wsBaseUrl}/ws/notifications?access_token=${encodeURIComponent(token)}`;

    try {
      const ws = new WebSocket(wsUrl);
      socketRef.current = ws;

      ws.onopen = () => {
        setIsConnected(true);
        // Start ping interval every 25 seconds
        if (pingIntervalRef.current) clearInterval(pingIntervalRef.current);
        pingIntervalRef.current = setInterval(() => {
          if (ws.readyState === WebSocket.OPEN) {
            ws.send(JSON.stringify({ type: "ping" }));
          }
        }, 25000);
      };

      ws.onmessage = (event) => {
        try {
          const payload = JSON.parse(event.data);
          if (payload.type === "notification" && payload.data) {
            const newNotif = payload.data as NotificationResponse;
            setNotifications((prev) => [newNotif, ...prev.filter((n) => n.id !== newNotif.id)]);
            setUnreadCount((prev) => prev + 1);
            setLatestNotification(newNotif);

            // Auto dismiss toast after 8 seconds
            setTimeout(() => {
              setLatestNotification((current) => (current?.id === newNotif.id ? null : current));
            }, 8000);
          }
        } catch {
          // Ignored non-json message
        }
      };

      ws.onclose = () => {
        setIsConnected(false);
        if (pingIntervalRef.current) clearInterval(pingIntervalRef.current);
        // Reconnect after 3 seconds if still authenticated
        if (isAuthenticated) {
          if (reconnectTimeoutRef.current) clearTimeout(reconnectTimeoutRef.current);
          reconnectTimeoutRef.current = setTimeout(() => {
            connectWebSocketRef.current?.();
          }, 3000);
        }
      };

      ws.onerror = () => {
        setIsConnected(false);
      };
    } catch (err) {
      console.warn("WebSocket connection failed to initialize:", err);
    }
  }, [isAuthenticated]);

  useEffect(() => {
    connectWebSocketRef.current = connectWebSocket;
  }, [connectWebSocket]);

  useEffect(() => {
    let notifyLoadTimeout: ReturnType<typeof setTimeout> | null = null;

    if (isAuthenticated) {
      notifyLoadTimeout = setTimeout(() => {
        void loadNotifications();
      }, 0);
      connectWebSocket();
    } else {
      if (socketRef.current) {
        socketRef.current.close();
        socketRef.current = null;
      }
    }

    return () => {
      if (notifyLoadTimeout) clearTimeout(notifyLoadTimeout);
      if (pingIntervalRef.current) clearInterval(pingIntervalRef.current);
      if (reconnectTimeoutRef.current) clearTimeout(reconnectTimeoutRef.current);
      if (socketRef.current) {
        socketRef.current.close();
        socketRef.current = null;
      }
    };
  }, [isAuthenticated, loadNotifications, connectWebSocket]);

  const markAsRead = async (id: string) => {
    try {
      await calendarService.markNotificationRead(id);
      setNotifications((prev) =>
        prev.map((n) => (n.id === id ? { ...n, readAt: new Date().toISOString() } : n))
      );
      setUnreadCount((prev) => Math.max(0, prev - 1));
    } catch {
      // Ignored
    }
  };

  const markAllAsRead = async () => {
    try {
      const unreadList = notifications.filter((n) => !n.readAt);
      await Promise.all(unreadList.map((n) => calendarService.markNotificationRead(n.id).catch(() => {})));
      const nowIso = new Date().toISOString();
      setNotifications((prev) => prev.map((n) => ({ ...n, readAt: n.readAt || nowIso })));
      setUnreadCount(0);
    } catch {
      setUnreadCount(0);
    }
  };

  const dismissLatestNotification = () => {
    setLatestNotification(null);
  };

  return (
    <NotificationContext.Provider
      value={{
        notifications,
        unreadCount,
        isConnected,
        latestNotification,
        markAsRead,
        markAllAsRead,
        dismissLatestNotification,
        refreshNotifications: loadNotifications,
      }}
    >
      {children}

      {/* Real-time Floating Toast Alert for incoming Reminders */}
      {latestNotification && (
        <div className="fixed top-4 right-4 z-50 max-w-sm w-full bg-white rounded-xl shadow-2xl border border-blue-100 p-4 animate-in slide-in-from-top-4 duration-300">
          <div className="flex items-start justify-between gap-3">
            <div className="flex items-center gap-2 text-xs font-bold text-[#0E1E4D]">
              <span className="size-2 rounded-full bg-emerald-500 animate-pulse" />
              <span>Nhắc nhở cuộc họp / sự kiện</span>
            </div>
            <button
              onClick={dismissLatestNotification}
              className="text-slate-400 hover:text-slate-600 text-xs font-semibold"
            >
              ✕
            </button>
          </div>
          <h4 className="text-xs font-bold text-slate-800 mt-2">
            {latestNotification.title}
          </h4>
          {latestNotification.description && (
            <p className="text-[11px] text-slate-500 mt-1 line-clamp-2">
              {latestNotification.description}
            </p>
          )}
          <div className="mt-3 flex items-center justify-end gap-2">
            <button
              onClick={() => {
                void markAsRead(latestNotification.id);
                dismissLatestNotification();
              }}
              className="text-[11px] font-semibold text-blue-600 hover:underline cursor-pointer"
            >
              Đã xem
            </button>
          </div>
        </div>
      )}
    </NotificationContext.Provider>
  );
}

export function useNotifications(): NotificationContextType {
  const context = useContext(NotificationContext);
  if (!context) {
    throw new Error("useNotifications must be used within a NotificationProvider");
  }
  return context;
}
