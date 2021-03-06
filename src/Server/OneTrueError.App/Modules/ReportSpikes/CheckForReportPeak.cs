﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCqs;
using Griffin.Container;
using OneTrueError.Api.Core.Messaging;
using OneTrueError.Api.Core.Messaging.Commands;
using OneTrueError.App.Configuration;
using OneTrueError.App.Core.Notifications;
using OneTrueError.Infrastructure.Configuration;

namespace OneTrueError.App.Modules.ReportSpikes
{
    /// <summary>
    /// 
    /// </summary>
    [Component(RegisterAsSelf = true)]
    public class CheckForReportPeak :
        IApplicationEventSubscriber<Api.Core.Incidents.Events.ReportAddedToIncident>
    {
        private readonly ICommandBus _commandBus;
        private readonly INotificationsRepository _repository;
        private readonly IReportSpikeRepository _spikeRepository;

        /// <summary>
        /// Creates a new instance of <see cref="CheckForReportPeak"/>.
        /// </summary>
        /// <param name="repository">To check if spikes should be analysed</param>
        /// <param name="spikeRepository">store/fetch information of current spikes.</param>
        /// <param name="commandBus">used to send emails</param>
        public CheckForReportPeak(INotificationsRepository repository, IReportSpikeRepository spikeRepository,
            ICommandBus commandBus)
        {
            _repository = repository;
            _spikeRepository = spikeRepository;
            _commandBus = commandBus;
        }

        /// <summary>
        /// Process an event asynchronously.
        /// </summary>
        /// <param name="e">event to process</param>
        /// <returns>
        /// Task to wait on.
        /// </returns>
        public async Task HandleAsync(Api.Core.Incidents.Events.ReportAddedToIncident e)
        {
            if (e == null) throw new ArgumentNullException("e");

            var config = ConfigurationStore.Instance.Load<BaseConfiguration>();
            var url = config.BaseUrl;
            var settings = await _repository.GetAllAsync(e.Report.ApplicationId);
            if (!settings.Any(x => x.ApplicationSpike != NotificationState.Disabled))
                return;

            var todaysCount = await CalculateSpike(e);
            if (todaysCount == null)
                return;

            var spike = await _spikeRepository.GetSpikeAsync(e.Incident.ApplicationId);
            if (spike != null)
                spike.IncreaseReportCount();

            var existed = spike != null;
            var messages = new List<EmailMessage>();
            foreach (var setting in settings)
            {
                if (setting.ApplicationSpike != NotificationState.Disabled)
                    continue;

                if (spike != null && spike.HasAccount(setting.AccountId))
                    continue;

                if (spike == null)
                    spike = new ErrorReportSpike(e.Incident.ApplicationId, 1);

                spike.AddNotifiedAccount(setting.AccountId);
                var msg = new EmailMessage(setting.AccountId.ToString())
                {
                    Subject = string.Format("Spike detected for {0} ({1} reports)",
                        e.Incident.ApplicationName,
                        todaysCount),
                    TextBody =
                        string.Format(
                            "We've detected a spike in incoming reports for application <a href=\"{0}/#/application/{1}\">{2}</a>\r\n" +
                            "\r\n" +
                            "We've received {3} reports so far. Day average is {4}\r\n" +
                            "\r\n" +
                            "No further spike emails will be sent today for that application.",
                            url,
                            e.Incident.ApplicationId,
                            e.Incident.ApplicationName, todaysCount.SpikeCount, todaysCount.DayAverage)
                };

                messages.Add(msg);
            }

            if (existed)
                await _spikeRepository.UpdateSpikeAsync(spike);
            else
                await _spikeRepository.CreateSpikeAsync(spike);

            foreach (var message in messages)
            {
                var sendEmail = new SendEmail(message);
                await _commandBus.ExecuteAsync(sendEmail);
            }
        }

        /// <summary>
        /// Compare received amount of report with a calculated threshold.
        /// </summary>
        /// <param name="applicationEvent">e</param>
        /// <returns>-1 if no spike is detected; otherwise the spike count</returns>
        protected async Task<NewSpike> CalculateSpike(Api.Core.Incidents.Events.ReportAddedToIncident applicationEvent)
        {
            if (applicationEvent == null) throw new ArgumentNullException("applicationEvent");

            var average = await _spikeRepository.GetAverageReportCountAsync(applicationEvent.Incident.ApplicationId);
            if (average == 0)
                return null;

            var todaysCount = await _spikeRepository.GetTodaysCountAsync(applicationEvent.Incident.ApplicationId);
            var threshold = average > 20 ? average : average * 2;
            if (todaysCount < threshold)
                return null;

            return new NewSpike
            {
                SpikeCount = todaysCount,
                DayAverage = average
            };
        }

    }
}