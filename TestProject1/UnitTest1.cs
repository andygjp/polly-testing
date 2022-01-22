using Xunit;

namespace TestProject1;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using Xunit.Abstractions;

// https://github.com/App-vNext/Polly/wiki/Polly-and-HttpClientFactory
public class UnitTest1
{
    private readonly ITestOutputHelper testOutputHelper;
    private readonly ServiceCollection serviceCollection = new();
    private int numberOfRetries = -1;

    public UnitTest1(ITestOutputHelper testOutputHelper) => this.testOutputHelper = testOutputHelper;

    [Fact]
    public async Task Request_timeouts_call_is_retried_three_times()
    {
        var retryPolicy = HttpPolicyExtensions
            // handles request timeout
            .HandleTransientHttpError()
            .WaitAndRetryAsync(Retries(), RecordRetry);

        serviceCollection
            .AddHttpClient<Client, SlowClientCall>()
            .AddPolicyHandler(retryPolicy);

        Assert.Equal(HttpStatusCode.RequestTimeout, (await Call()).StatusCode);
        Assert.Equal(Retries().Length, numberOfRetries);
    }

    [Fact]
    public async Task Invalid_primary_handler_throws_and_not_handled_by_any_retry_policy()
    {
        var retryPolicy = HttpPolicyExtensions
            // handles transient errors - any status code greater than or equal to 500
            .HandleTransientHttpError()
            .WaitAndRetryAsync(Retries(), RecordRetry);

        serviceCollection
            .AddScoped<InvalidPrimaryHandler>()
            .AddHttpClient<Client, ServerErrorClient>()
            .AddPolicyHandler(retryPolicy)
            .ConfigurePrimaryHttpMessageHandler<InvalidPrimaryHandler>();

        await Assert.ThrowsAsync<InvalidPrimaryHandler.Exception>(async () => await Call());
        Assert.Equal(0, numberOfRetries);
    }

    [Fact]
    public async Task Invalid_primary_handler_throws_and_and_retried_three_times_before_failing()
    {
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            // handles specific exception
            .Or<InvalidPrimaryHandler.Exception>()
            .WaitAndRetryAsync(Retries(), RecordRetry);

        serviceCollection
            .AddScoped<InvalidPrimaryHandler>()
            .AddHttpClient<Client, ServerErrorClient>()
            .AddPolicyHandler(retryPolicy)
            .ConfigurePrimaryHttpMessageHandler<InvalidPrimaryHandler>();

        await Assert.ThrowsAsync<InvalidPrimaryHandler.Exception>(async () => await Call());
        Assert.Equal(Retries().Length, numberOfRetries);
    }

    [Fact]
    public async Task Primary_handler_does_not_interfere_with_retry_behaviour()
    {
        var retryPolicy = HttpPolicyExtensions
            // handles transient errors - any status code greater than or equal to 500
            .HandleTransientHttpError()
            .WaitAndRetryAsync(Retries(), RecordRetry);

        serviceCollection
            .AddScoped<PrimaryHandler>()
            .AddHttpClient<Client, ServerErrorClient>()
            .AddPolicyHandler(retryPolicy)
            .ConfigurePrimaryHttpMessageHandler<PrimaryHandler>();

        Assert.Equal(HttpStatusCode.InternalServerError, (await Call()).StatusCode);
        Assert.Equal(Retries().Length, numberOfRetries);
    }

    [Fact]
    public async Task Server_internal_error_response_is_retried_three_times()
    {
        var retryPolicy = HttpPolicyExtensions
            // handles transient errors - any status code greater than or equal to 500
            .HandleTransientHttpError()
            .WaitAndRetryAsync(Retries(), RecordRetry);

        serviceCollection
            .AddHttpClient<Client, ServerErrorClient>()
            .AddPolicyHandler(retryPolicy);

        Assert.Equal(HttpStatusCode.InternalServerError, (await Call()).StatusCode);
        Assert.Equal(Retries().Length, numberOfRetries);
    }

    [Fact]
    public async Task Slow_server_response_throws_time_out_after_three_retries()
    {
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            // handles TimeoutRejectedException thrown by the timeoutPolicy
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(Retries(), RecordRetry);

        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(1));

        serviceCollection
            .AddHttpClient<Client, SlowServerClient>()
            .AddPolicyHandler(retryPolicy)
            .AddPolicyHandler(timeoutPolicy);

        try
        {
            await Call();
        }
        catch (TimeoutRejectedException ex) when (ex.InnerException is TaskCanceledException)
        {
            testOutputHelper.WriteLine(ex.ToString());
        }

        Assert.Equal(Retries().Length, numberOfRetries);
    }

    [Fact]
    public async Task Call_non_existent_server_is_retried_three_times_before_throwing()
    {
        var retryPolicy = HttpPolicyExtensions
            // handles HttpRequestExceptions and transient errors
            .HandleTransientHttpError()
            .WaitAndRetryAsync(Retries(), RecordRetry);

        serviceCollection
            .AddHttpClient<Client, ServerThatDoesNotExistClient>()
            .AddPolicyHandler(retryPolicy);

        try
        {
            await Call();
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException)
        {
            testOutputHelper.WriteLine(ex.ToString());
        }

        Assert.Equal(Retries().Length, numberOfRetries);
    }

    private Task<HttpResponseMessage> Call()
    {
        numberOfRetries = 0;
        return GetClient().Call();
    }

    private Client GetClient() => serviceCollection.BuildServiceProvider().GetService<Client>()!;

    private void RecordRetry(DelegateResult<HttpResponseMessage> _, TimeSpan __) => numberOfRetries++;

    private static TimeSpan[] Retries()
    {
        return new[]
        {
            TimeSpan.FromMilliseconds(1000),
            TimeSpan.FromMilliseconds(1500),
            TimeSpan.FromMilliseconds(2000)
        };
    }
}