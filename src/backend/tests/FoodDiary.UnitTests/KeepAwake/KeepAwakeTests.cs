using System.Net;
using FoodDiary.API.Features.KeepAwake;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.UnitTests.KeepAwake;

public class KeepAwakeTests
{
    private static readonly Uri HealthCheck = new("https://example.test/healthz/live");

    private static readonly TimeZoneInfo HongKong = TimeZoneInfo.FindSystemTimeZoneById("Asia/Hong_Kong");

    private static readonly KeepAwakeSchedule QuietAtNightInHongKong = new(
        HealthCheck, TimeSpan.FromMinutes(10), HongKong, new TimeOnly(3, 0), new TimeOnly(7, 0));

    [Theory]
    [InlineData("2026-10-27T19:00:00Z", true)]
    [InlineData("2026-10-27T21:30:00Z", true)]
    [InlineData("2026-10-27T18:59:00Z", false)]
    [InlineData("2026-10-27T23:00:00Z", false)]
    [InlineData("2026-10-28T05:00:00Z", false)]
    public void Quiet_hours_are_in_the_configured_time_zone(string utcNow, bool quiet)
    {
        QuietAtNightInHongKong.IsQuiet(DateTimeOffset.Parse(utcNow)).Should().Be(quiet);
    }

    [Fact]
    public void Quiet_hours_can_span_midnight()
    {
        var schedule = new KeepAwakeSchedule(
            HealthCheck, TimeSpan.FromMinutes(10), TimeZoneInfo.Utc, new TimeOnly(23, 0), new TimeOnly(2, 0));

        schedule.IsQuiet(DateTimeOffset.Parse("2026-10-27T23:30:00Z")).Should().BeTrue();
        schedule.IsQuiet(DateTimeOffset.Parse("2026-10-28T01:30:00Z")).Should().BeTrue();
        schedule.IsQuiet(DateTimeOffset.Parse("2026-10-28T12:00:00Z")).Should().BeFalse();
    }

    [Fact]
    public void Without_quiet_hours_it_is_never_quiet()
    {
        var schedule = new KeepAwakeSchedule(HealthCheck, TimeSpan.FromMinutes(10), HongKong, null, null);

        schedule.IsQuiet(DateTimeOffset.Parse("2026-10-27T20:00:00Z")).Should().BeFalse();
    }

    [Fact]
    public async Task Requests_the_url_again_after_a_failed_request()
    {
        var server = new RecordingHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
        using var service = CreateService(server, new FixedClock(DateTimeOffset.Parse("2026-10-28T05:00:00Z")));

        await service.StartAsync(CancellationToken.None);
        await server.WaitForRequests(2);
        await service.StopAsync(CancellationToken.None);

        server.RequestedUrls.Take(2).Should().Equal(HealthCheck, HealthCheck);
    }

    [Fact]
    public async Task Sends_nothing_during_quiet_hours()
    {
        var server = new RecordingHandler(HttpStatusCode.OK);
        using var service = CreateService(server, new FixedClock(DateTimeOffset.Parse("2026-10-27T20:00:00Z")));

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(TimeSpan.FromMilliseconds(200));
        await service.StopAsync(CancellationToken.None);

        server.RequestedUrls.Should().BeEmpty();
    }

    [Fact]
    public void Nothing_runs_without_a_url()
    {
        var services = new ServiceCollection();

        services.AddKeepAwake(new ConfigurationBuilder().Build());

        services.Should().NotContain(service => service.ServiceType == typeof(IHostedService));
    }

    [Fact]
    public void Quiet_hours_need_both_ends()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KeepAwake:Url"] = HealthCheck.ToString(),
                ["KeepAwake:QuietFrom"] = "03:00"
            })
            .Build();

        var addKeepAwake = () => new ServiceCollection().AddKeepAwake(configuration);

        addKeepAwake.Should().Throw<InvalidOperationException>().WithMessage("*QuietUntil*");
    }

    private static KeepAwakeService CreateService(RecordingHandler server, TimeProvider clock)
    {
        var schedule = new KeepAwakeSchedule(
            HealthCheck, TimeSpan.FromMilliseconds(20), HongKong, new TimeOnly(3, 0), new TimeOnly(7, 0));

        return new KeepAwakeService(new SingleClientFactory(server), schedule, clock,
            NullLogger<KeepAwakeService>.Instance);
    }

    // Real timers, so the service ticks; a fixed clock, so the time of day is known
    private sealed class FixedClock(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class RecordingHandler(params HttpStatusCode[] answers) : HttpMessageHandler
    {
        private readonly List<Uri> _requestedUrls = [];
        private readonly SemaphoreSlim _requested = new(0);

        public IReadOnlyList<Uri> RequestedUrls
        {
            get
            {
                lock (_requestedUrls)
                {
                    return _requestedUrls.ToList();
                }
            }
        }

        public async Task WaitForRequests(int count)
        {
            for (var i = 0; i < count; i++)
            {
                (await _requested.WaitAsync(TimeSpan.FromSeconds(5))).Should().BeTrue();
            }
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            int index;

            lock (_requestedUrls)
            {
                index = _requestedUrls.Count;
                _requestedUrls.Add(request.RequestUri!);
            }

            _requested.Release();
            return Task.FromResult(new HttpResponseMessage(answers[Math.Min(index, answers.Length - 1)]));
        }
    }
}
