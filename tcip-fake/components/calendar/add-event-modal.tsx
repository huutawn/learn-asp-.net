"use client";

import React, { useState, useEffect, useCallback } from "react";
import {
  X,
  Clock,
  Users as UsersIcon,
  RotateCw,
  Bell,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Avatar } from "@/components/ui/avatar";
import { CreateEventRequest, DayOfWeek } from "@/features/calendar/types/calendar.types";
import { useAuth } from "@/features/auth/context/auth-context";
import { userService } from "@/features/users/services/user.service";
import { User } from "@/features/auth/types/auth.types";
import { formatLocalDateToYMD, ENGLISH_WEEKDAY_KEYS } from "@/lib/date-utils";

interface AddEventModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSave: (req: CreateEventRequest) => Promise<unknown>;
  initialDate?: Date;
}

const WEEKDAY_BUTTONS: { key: DayOfWeek; label: string }[] = [
  { key: "Monday", label: "M" },
  { key: "Tuesday", label: "T" },
  { key: "Wednesday", label: "W" },
  { key: "Thursday", label: "T" },
  { key: "Friday", label: "F" },
  { key: "Saturday", label: "S" },
  { key: "Sunday", label: "S" },
];

const PRESET_REMINDERS = [
  { value: 0, label: "Đúng giờ" },
  { value: 5, label: "5p" },
  { value: 10, label: "10p" },
  { value: 15, label: "15p" },
  { value: 30, label: "30p" },
  { value: 60, label: "60p" },
];

function AddEventModalDialog({
  onClose,
  onSave,
  initialDate = new Date(),
}: AddEventModalProps) {
  const { user } = useAuth();

  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [eventDate, setEventDate] = useState(() => formatLocalDateToYMD(initialDate));
  const [fromTime, setFromTime] = useState("08:00");
  const [toTime, setToTime] = useState("09:00");
  const [isAllDay, setIsAllDay] = useState(false);
  const [enableReminder, setEnableReminder] = useState(true);
  const [reminderMinutes, setReminderMinutes] = useState(15);
  const [selectedWeekdays, setSelectedWeekdays] = useState<DayOfWeek[]>([]);
  const [availableUsers, setAvailableUsers] = useState<User[]>([]);
  const [selectedUserIds, setSelectedUserIds] = useState<string[]>(() =>
    user?.id ? [user.id] : []
  );
  const [isSaving, setIsSaving] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const handleClose = useCallback(() => {
    setErrorMessage(null);
    setIsSaving(false);
    onClose();
  }, [onClose]);

  // Handle ESC key press to close modal
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        e.preventDefault();
        handleClose();
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [handleClose]);

  // Load users on mount
  useEffect(() => {
    let ignore = false;
    userService
      .getUsers({ page: 1, pageSize: 50 })
      .then((res) => {
        if (!ignore && res?.items) setAvailableUsers(res.items);
      })
      .catch(() => {});
    return () => {
      ignore = true;
    };
  }, []);

  const toggleWeekday = (day: DayOfWeek) => {
    setSelectedWeekdays((prev) =>
      prev.includes(day) ? prev.filter((d) => d !== day) : [...prev, day]
    );
  };

  const removeUser = (id: string) => {
    if (id === user?.id && selectedUserIds.length === 1) return;
    setSelectedUserIds((prev) => prev.filter((uid) => uid !== id));
  };

  const addUser = (id: string) => {
    if (!selectedUserIds.includes(id)) {
      setSelectedUserIds((prev) => [...prev, id]);
    }
  };

  const handleSave = async (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    setErrorMessage(null);

    if (!title.trim()) {
      setErrorMessage("Vui lòng nhập tiêu đề sự kiện");
      return;
    }

    if (!fromTime || (!isAllDay && !toTime)) {
      setErrorMessage("Vui lòng nhập đầy đủ giờ bắt đầu và giờ kết thúc");
      return;
    }

    const [year, month, day] = eventDate.split("-").map(Number);
    const [fromH, fromM] = fromTime.split(":").map(Number);
    const [toH, toM] = toTime.split(":").map(Number);

    const startDate = new Date(year, month - 1, day, fromH || 0, fromM || 0, 0);
    const endDate = new Date(year, month - 1, day, toH || 0, toM || 0, 0);

    if (!isAllDay && endDate <= startDate) {
      setErrorMessage("Thời gian kết thúc phải sau thời gian bắt đầu");
      return;
    }

    const finalUserIds = Array.from(
      new Set([...(user?.id ? [user.id] : []), ...selectedUserIds])
    );

    const finalWeekdays: DayOfWeek[] = [...selectedWeekdays];
    const isRecurring = finalWeekdays.length > 0;
    if (isRecurring) {
      const firstEventWeekday = ENGLISH_WEEKDAY_KEYS[startDate.getDay()] as DayOfWeek;
      if (!finalWeekdays.includes(firstEventWeekday)) {
        finalWeekdays.push(firstEventWeekday);
      }
    }

    setIsSaving(true);
    try {
      const request: CreateEventRequest = {
        startAt: startDate.toISOString(),
        endAt: isAllDay ? undefined : endDate.toISOString(),
        timeZoneId: "SE Asia Standard Time",
        isRecurring,
        recurringWeekdays: isRecurring ? finalWeekdays : [],
        translations: [
          {
            language: "vi",
            title: title.trim(),
            description: description.trim() || `Cuộc họp / Sự kiện: ${title.trim()}`,
          },
        ],
        userIds: finalUserIds,
        groupIds: [],
        reminderMinutes: enableReminder ? [Math.max(0, reminderMinutes)] : [],
      };

      await onSave(request);
      handleClose();
    } catch (err: unknown) {
      const errorMsg =
        err && typeof err === "object" && "message" in err
          ? String((err as { message: string }).message)
          : "Lỗi khi tạo sự kiện trên hệ thống.";
      setErrorMessage(errorMsg);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-xs overflow-y-auto animate-in fade-in duration-150"
      onClick={(e) => {
        // Close when clicking outer backdrop
        if (e.target === e.currentTarget) {
          handleClose();
        }
      }}
    >
      {/* Modal Dialog Card */}
      <div
        className="relative w-full max-w-lg rounded-2xl bg-white p-6 shadow-2xl border border-slate-100 my-auto z-10 animate-in zoom-in-95 duration-150 max-h-[90vh] overflow-y-auto"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-center justify-between pb-3.5 border-b border-slate-100 sticky top-0 bg-white z-20">
          <h2 className="text-base font-bold text-slate-800">
            Thêm sự kiện mới
          </h2>
          <button
            type="button"
            onClick={handleClose}
            className="p-1.5 rounded-lg text-slate-400 hover:text-slate-700 hover:bg-slate-100 transition-colors cursor-pointer"
            aria-label="Đóng"
          >
            <X className="size-4" />
          </button>
        </div>

        {/* Error Alert */}
        {errorMessage && (
          <div className="mt-3 rounded-lg bg-rose-50 border border-rose-200 p-2.5 text-xs text-rose-700 font-medium">
            {errorMessage}
          </div>
        )}

        {/* Form Body */}
        <form onSubmit={handleSave} className="space-y-4 text-xs mt-4">
          {/* Tiêu đề */}
          <div>
            <label className="block text-slate-500 mb-1 font-medium">Tiêu đề sự kiện *</label>
            <Input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="Nhập tên sự kiện / cuộc họp..."
              className="h-9 text-xs"
              autoFocus
            />
          </div>

          {/* Mô tả */}
          <div>
            <label className="block text-slate-500 mb-1 font-medium">Mô tả / Địa điểm</label>
            <Input
              type="text"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="VD: Họp tại phòng Lotus, thảo luận kế hoạch..."
              className="h-9 text-xs"
            />
          </div>

          {/* Ngày và thời gian */}
          <div>
            <div className="flex items-center gap-1.5 text-slate-600 font-semibold mb-2">
              <Clock className="size-3.5 text-slate-500" />
              <span>Ngày và thời gian</span>
            </div>

            <div className="space-y-2 pl-1">
              <div>
                <label className="block text-slate-400 text-[11px] mb-1">Ngày</label>
                <Input
                  type="date"
                  value={eventDate}
                  onChange={(e) => setEventDate(e.target.value)}
                  className="h-8.5 text-xs"
                />
              </div>

              {!isAllDay && (
                <div className="grid grid-cols-2 gap-3 pt-1">
                  <div>
                    <label className="block text-slate-400 text-[11px] mb-1">
                      Từ (Giờ : Phút)
                    </label>
                    <Input
                      type="time"
                      value={fromTime}
                      onChange={(e) => setFromTime(e.target.value)}
                      className="h-8.5 text-xs font-mono font-medium text-slate-700 bg-white"
                    />
                  </div>

                  <div>
                    <label className="block text-slate-400 text-[11px] mb-1">
                      Đến (Giờ : Phút)
                    </label>
                    <Input
                      type="time"
                      value={toTime}
                      onChange={(e) => setToTime(e.target.value)}
                      className="h-8.5 text-xs font-mono font-medium text-slate-700 bg-white"
                    />
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* Nhắc nhở trước */}
          <div>
            <div className="flex items-center justify-between text-slate-600 font-semibold mb-2">
              <div className="flex items-center gap-1.5">
                <Bell className="size-3.5 text-slate-500" />
                <span>Nhắc nhở trước</span>
              </div>

              {/* Toggle reminder */}
              <div className="flex items-center gap-1.5">
                <span className="text-[11px] text-slate-500 font-normal">Bật nhắc nhở</span>
                <button
                  type="button"
                  role="switch"
                  aria-checked={enableReminder}
                  onClick={() => setEnableReminder(!enableReminder)}
                  className={`size-5 rounded-full border flex items-center justify-center cursor-pointer transition-colors ${
                    enableReminder ? "bg-blue-600 border-blue-600 text-white" : "border-slate-300 bg-white"
                  }`}
                >
                  <span
                    className={`size-2.5 rounded-full ${
                      enableReminder ? "bg-white" : "bg-slate-300"
                    }`}
                  />
                </button>
              </div>
            </div>

            {enableReminder && (
              <div className="space-y-2 pl-1 bg-slate-50/50 p-2.5 rounded-lg border border-slate-100">
                <div className="flex flex-wrap items-center gap-3">
                  <div className="flex items-center gap-1.5">
                    <Input
                      type="number"
                      min={0}
                      max={525600}
                      value={reminderMinutes}
                      onChange={(e) =>
                        setReminderMinutes(Math.max(0, parseInt(e.target.value, 10) || 0))
                      }
                      className="h-8 w-24 text-xs font-mono font-semibold text-slate-800 bg-white"
                    />
                    <span className="text-slate-600 text-[11px] font-medium">phút trước</span>
                  </div>

                  {/* Quick presets */}
                  <div className="flex flex-wrap items-center gap-1">
                    {PRESET_REMINDERS.map((preset) => (
                      <button
                        key={preset.value}
                        type="button"
                        onClick={() => setReminderMinutes(preset.value)}
                        className={`px-2 py-1 rounded text-[11px] font-semibold transition-all cursor-pointer ${
                          reminderMinutes === preset.value
                            ? "bg-[#0E1E4D] text-white shadow-xs"
                            : "bg-white border border-slate-200 text-slate-600 hover:bg-slate-100"
                        }`}
                      >
                        {preset.label}
                      </button>
                    ))}
                  </div>
                </div>
                <p className="text-[10px] text-slate-400">
                  {reminderMinutes === 0
                    ? "Hệ thống sẽ gửi thông báo đúng thời điểm sự kiện bắt đầu."
                    : `Hệ thống sẽ gửi thông báo trước ${reminderMinutes} phút khi sự kiện diễn ra.`}
                </p>
              </div>
            )}
          </div>

          {/* Người tham gia */}
          <div>
            <div className="flex items-center justify-between text-slate-600 font-semibold mb-2">
              <div className="flex items-center gap-1.5">
                <UsersIcon className="size-3.5 text-slate-500" />
                <span>Người tham gia</span>
              </div>
            </div>

            <div className="flex flex-wrap items-center gap-2 p-2 rounded-lg border border-slate-200 bg-slate-50/50 min-h-11">
              {selectedUserIds.map((uid) => {
                const isMe = uid === user?.id;
                const matched = availableUsers.find((u) => u.id === uid);
                const displayName = isMe
                  ? `${user?.displayName || "Tôi"} (Bạn)`
                  : matched?.displayName || matched?.email || "Người dùng";

                return (
                  <div
                    key={uid}
                    className="flex items-center gap-1.5 bg-white border border-slate-200 py-1 pl-1.5 pr-2 rounded-full shadow-2xs text-[11px] text-slate-700"
                  >
                    <Avatar fallback={displayName} size="sm" className="size-4 text-[9px]" />
                    <span className="max-w-[100px] truncate">{displayName}</span>
                    {!isMe && (
                      <button
                        type="button"
                        onClick={() => removeUser(uid)}
                        className="text-slate-400 hover:text-rose-500 cursor-pointer"
                      >
                        <X className="size-3" />
                      </button>
                    )}
                  </div>
                );
              })}

              {/* Add attendee */}
              {availableUsers.length > 0 &&
                availableUsers.some((u) => !selectedUserIds.includes(u.id)) && (
                  <select
                    onChange={(e) => {
                      if (e.target.value) addUser(e.target.value);
                      e.target.value = "";
                    }}
                    defaultValue=""
                    className="h-6 text-[11px] bg-transparent border border-dashed border-slate-300 rounded-full px-2 text-slate-500 hover:border-slate-400 cursor-pointer"
                  >
                    <option value="" disabled>+ Thêm người</option>
                    {availableUsers
                      .filter((u) => !selectedUserIds.includes(u.id))
                      .map((u) => (
                        <option key={u.id} value={u.id}>
                          {u.displayName || u.email}
                        </option>
                      ))}
                  </select>
                )}
            </div>
          </div>

          {/* Lặp lại vào */}
          <div>
            <div className="flex items-center justify-between text-slate-600 font-semibold mb-2">
              <div className="flex items-center gap-1.5">
                <RotateCw className="size-3.5 text-slate-500" />
                <span>Lặp lại vào</span>
              </div>

              {/* All day toggle */}
              <div className="flex items-center gap-1.5">
                <span className="text-[11px] text-slate-500 font-normal">Cả ngày</span>
                <button
                  type="button"
                  role="switch"
                  aria-checked={isAllDay}
                  onClick={() => setIsAllDay(!isAllDay)}
                  className={`size-5 rounded-full border flex items-center justify-center cursor-pointer transition-colors ${
                    isAllDay ? "bg-blue-600 border-blue-600 text-white" : "border-slate-300 bg-white"
                  }`}
                >
                  <span
                    className={`size-2.5 rounded-full ${
                      isAllDay ? "bg-white" : "bg-slate-300"
                    }`}
                  />
                </button>
              </div>
            </div>

            {/* Weekday buttons */}
            <div className="flex items-center gap-2 pt-1">
              <span className="text-slate-400 text-[11px] mr-2">Thứ</span>
              {WEEKDAY_BUTTONS.map((item, idx) => {
                const isSelected = selectedWeekdays.includes(item.key);
                return (
                  <button
                    key={`${item.key}-${idx}`}
                    type="button"
                    onClick={() => toggleWeekday(item.key)}
                    className={`size-8 rounded-md text-xs font-semibold border transition-all cursor-pointer ${
                      isSelected
                        ? "bg-[#0E1E4D] border-[#0E1E4D] text-white shadow-xs"
                        : "border-slate-200 bg-white text-slate-600 hover:bg-slate-50"
                    }`}
                  >
                    {item.label}
                  </button>
                );
              })}
            </div>
          </div>

          {/* Footer Actions */}
          <div className="flex items-center justify-end gap-2.5 pt-4 mt-5 border-t border-slate-100 sticky bottom-0 bg-white z-20">
            <Button
              type="button"
              variant="secondary"
              onClick={handleClose}
              className="h-8 px-4 text-xs font-semibold text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-md cursor-pointer"
            >
              Huỷ
            </Button>
            <Button
              type="submit"
              disabled={isSaving}
              className="h-8 px-5 text-xs font-semibold text-white bg-[#0E1E4D] hover:bg-[#162d6f] rounded-md shadow-xs cursor-pointer"
            >
              {isSaving ? "Đang lưu..." : "Lưu"}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}

export function AddEventModal({
  isOpen,
  onClose,
  onSave,
  initialDate,
}: AddEventModalProps) {
  if (!isOpen) return null;

  return (
    <AddEventModalDialog
      key={initialDate ? initialDate.toISOString() : "new-event"}
      isOpen={isOpen}
      onClose={onClose}
      onSave={onSave}
      initialDate={initialDate}
    />
  );
}
