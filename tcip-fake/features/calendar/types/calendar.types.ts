export type EventStatus = "Active" | "Cancelled";
export type DayOfWeek =
  | "Sunday"
  | "Monday"
  | "Tuesday"
  | "Wednesday"
  | "Thursday"
  | "Friday"
  | "Saturday";

export interface EventTranslationRequest {
  language: string;
  title: string;
  description?: string;
}

export interface CreateEventRequest {
  startAt: string; // ISO 8601 string
  endAt?: string;
  timeZoneId: string;
  isRecurring: boolean;
  recurringWeekdays: DayOfWeek[];
  recurrenceEndAt?: string;
  translations: EventTranslationRequest[];
  userIds: string[];
  groupIds: string[];
  reminderMinutes: number[];
}

export interface CalendarEvent {
  id: string;
  startAt: string;
  endAt?: string;
  timeZoneId: string;
  status: EventStatus;
  isRecurring: boolean;
  recurringWeekdays: DayOfWeek[];
  recurrenceEndAt?: string;
  title: string;
  description?: string;
  reminderMinutes: number[];
  color?: string; // UI accent color
  attendees?: Array<{
    id: string;
    name: string;
    avatar?: string;
  }>;
}

export interface NotificationResponse {
  id: string;
  eventId: string;
  occurrenceStartAt: string;
  title: string;
  description?: string;
  sentAt: string;
  readAt?: string;
}
