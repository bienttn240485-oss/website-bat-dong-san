import { Calendar } from "@fullcalendar/core";
import dayGridPlugin from "@fullcalendar/daygrid";
import interactionPlugin from "@fullcalendar/interaction";
import timeGridPlugin from "@fullcalendar/timegrid";
import viLocale from "@fullcalendar/core/locales/vi";

const calendarElement = document.getElementById("admin-schedule-calendar");

const vietnameseButtons = {
  today: "Hôm nay",
  month: "Tháng",
  week: "Tuần",
  day: "Ngày",
  list: "Danh sách",
};

if (calendarElement) {
  const eventsUrl = calendarElement.dataset.eventsUrl;
  const fieldId = calendarElement.dataset.fieldId;
  const initialDate = calendarElement.dataset.initialDate;

  const calendar = new Calendar(calendarElement, {
    plugins: [dayGridPlugin, timeGridPlugin, interactionPlugin],
    initialView: window.matchMedia("(max-width: 640px)").matches ? "timeGridDay" : "timeGridWeek",
    initialDate,
    locale: viLocale,
    firstDay: 1,
    nowIndicator: true,
    allDaySlot: false,
    slotMinTime: "06:00:00",
    slotMaxTime: "23:30:00",
    slotDuration: "00:30:00",
    height: "auto",
    expandRows: true,
    headerToolbar: {
      left: "prev,next today",
      center: "title",
      right: "dayGridMonth,timeGridWeek,timeGridDay",
    },
    buttonText: vietnameseButtons,
    events: (info, successCallback, failureCallback) => {
      const url = new URL(eventsUrl, window.location.origin);
      url.searchParams.set("start", info.startStr);
      url.searchParams.set("end", info.endStr);
      if (fieldId) {
        url.searchParams.set("fieldId", fieldId);
      }

      fetch(url, { headers: { Accept: "application/json" } })
        .then((response) => {
          if (!response.ok) {
            throw new Error("Không tải được lịch sân.");
          }

          return response.json();
        })
        .then(successCallback)
        .catch((error) => {
          calendarElement.innerHTML = `<div class="rounded-ui border border-red-200 bg-red-50 p-4 text-sm text-red-800">${error.message}</div>`;
          failureCallback(error);
        });
    },
    eventClick: (info) => {
      if (info.event.url) {
        info.jsEvent.preventDefault();
        window.location.assign(info.event.url);
      }
    },
    eventDidMount: (info) => {
      const description = info.event.extendedProps.description;
      if (description) {
        info.el.setAttribute("title", description);
      }
    },
  });

  calendar.render();
}
