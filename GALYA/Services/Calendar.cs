using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using System.Net;
using Google.Apis.Util.Store;

namespace GALYA
{
    internal class Calendar
    {
        const string _calendarId = "7355eb95b4a5dec9a9e70869529639985663b1f42a0e421c927e0ed3f0ee8c05@group.calendar.google.com";
        string[] _scopes = { CalendarService.Scope.Calendar };
        string _applicationName = "GALYA_BOT_TELEGRAM";
        UserCredential _credential;
        CalendarService _service;

        public Calendar()
        {
            //Create Google Calendar API service.
            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                _credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    _scopes,
                    "user",
                    CancellationToken.None, new FileDataStore(credPath, true)).Result;
            }

            _service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = _credential,
                ApplicationName = _applicationName,
            });


            _service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = _credential,
                ApplicationName = _applicationName
                 
            });
        }

        // Добавление события
        public void AddEvent(string title, string descr, DateTime startTime, DateTime endTime)
        {
            var newEvent = new Event();
            EventDateTime start = new EventDateTime();
            EventDateTime end = new EventDateTime();
            start.DateTime = startTime;
            end.DateTime = endTime;

            newEvent.Start = start;
            newEvent.End = end;
            newEvent.Summary = title;
            newEvent.Description = descr;
            /*Event recurringEvent = */
            _service.Events.Insert(newEvent, _calendarId).Execute();
            Console.WriteLine("Event created: \n", newEvent.HtmlLink);
        }

        // удаление события
        public void DeleteEvent(DateTime startTime)
        {
            try
            {
                EventsResource.ListRequest request = _service.Events.List(_calendarId);
                request.TimeMin = DateTime.Now;
                request.ShowDeleted = false;
                request.SingleEvents = true;
                request.MaxResults = 10;
                request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
                
                Events events = request.Execute();
                Console.WriteLine("Upcoming events:");
                if (events.Items == null || events.Items.Count == 0)
                {
                    Console.WriteLine("No upcoming events found.");
                    return;
                }
                foreach (var eventItem in events.Items)
                {
                    string when = eventItem.Start.DateTime.ToString();
                    if (string.IsNullOrEmpty(when))
                    {
                        when = eventItem.Start.Date;
                    }
                    Console.Write("{0} ({1}) ", eventItem.Summary, when);

                    if (eventItem.Start.DateTime == startTime)
                    {
                        _service.Events.Delete(_calendarId, eventItem.Id).Execute();
                        Console.WriteLine("Event deleted");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
