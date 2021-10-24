using Cronos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OwnScheduler.BackgroundTask
{
    public class OwnerCronJob: BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OwnerCronJob> _logger;
        public OwnerCronJob(IConfiguration configuration, ILogger<OwnerCronJob> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var cronExpression = _configuration.GetValue<string>("cronExpression");
            while (!stoppingToken.IsCancellationRequested)
            {
                // Schedule the job
                await WaitForNextSchedule(cronExpression);

                // Task to run
                await ProcessAsync(stoppingToken);
            }
        }

        private async Task ProcessAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(100, stoppingToken);

        }


        /// <summary>
        ///                                           Allowed values    Allowed special characters Comment
        ///
        ///    ┌───────────── second(optional)        0-59              * , - /                      
        ///    │ ┌───────────── minute                0-59              * , - /                      
        ///    │ │ ┌───────────── hour                0-23              * , - /                      
        ///    │ │ │ ┌───────────── day of month      1-31              * , - / L W ?                
        ///    │ │ │ │ ┌───────────── month           1-12 or JAN-DEC* , - /                      
        ///    │ │ │ │ │ ┌───────────── day of week   0-6  or SUN-SAT* , - / # L ?                Both 0 and 7 means SUN
        ///    │ │ │ │ │ │
        ///    * * * * * *
        /// </summary>
        /// <param name="cronExpression"></param>
        /// <see cref="https://github.com/HangfireIO/Cronos"/>
        /// <returns></returns>
        private async Task WaitForNextSchedule(string cronExpression)
        {
            var parsedExp = CronExpression.Parse(cronExpression);
            var currentUtcTime = DateTimeOffset.UtcNow.UtcDateTime;
            var occurenceTime = parsedExp.GetNextOccurrence(currentUtcTime);

            var delay = occurenceTime.GetValueOrDefault() - currentUtcTime;
            _logger.LogInformation("The run is delayed for {delay}. Current time: {time}", delay, DateTimeOffset.Now);

            await Task.Delay(delay);
        }
    }
}
