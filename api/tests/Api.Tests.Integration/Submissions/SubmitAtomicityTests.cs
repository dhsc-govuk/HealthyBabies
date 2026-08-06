using System.Net;
using System.Net.Http.Headers;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.Organisations;
using Domain.ServiceForms;
using Domain.Services;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Submissions;

/// <summary>
/// Regression tests for BLUM-7047: SubmitSubmission used to call SaveChangesAsync once per
/// FormSubmission record, so a mid-request failure could leave some records committed as
/// "submitted" while others (and the parent DataCollectionSubmission) were never written. The
/// fix batches every write into a single SaveChangesAsync call, so a failure anywhere in that
/// call must leave nothing committed - these tests force a failure inside that call via a
/// test-only interceptor and assert the database is left exactly as it started.
/// </summary>
public class SubmitAtomicityTests(OrganisationAdminIntegrationTestWebFactory factory)
    : BaseOrganisationAdminIntegrationTest(factory), IAsyncLifetime
{
    private static readonly DateTime QuarterStart = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime QuarterEnd = new(2026, 3, 31, 23, 59, 59, DateTimeKind.Utc);

    private readonly ThrowingSaveChangesInterceptor _interceptor = new();

    private DataCollection _dataCollection = null!;
    private DataCollectionFormModule _formModule = null!;
    private Service _serviceOne = null!;
    private Service _serviceTwo = null!;

    [Fact]
    public async Task Submit_LeavesNoPartialState_WhenTheBatchedSaveFails()
    {
        // Building the client boots a whole new host, which reruns the app's own startup seeding
        // (ApplicationDbContextInitialiser.SeedDataAsync) through this same DbContext - the
        // interceptor must stay disarmed until after that completes, or it throws during seeding
        // instead of during the actual submit call under test.
        using var throwingClient = BuildThrowingClient();
        _interceptor.ShouldThrow = true;

        var response = await throwingClient.PostAsync(SubmitUrl(), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError, await response.Content.ReadAsStringAsync());

        // Re-read via a fresh scope/connection (not the request's own context) - nothing from the
        // failed batch should have committed. Before the fix, the two service submissions would
        // have committed independently, one per loop iteration; here every write shares a single
        // SaveChangesAsync, so a forced failure must leave both untouched.
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var submissions = await context.FormSubmissions
            .Where(s => s.DataCollectionId == _dataCollection.Id)
            .ToListAsync();
        submissions.Should().HaveCount(2);
        submissions.Should().OnlyContain(s => s.Status.Value == "approved");

        var parent = await context.DataCollectionSubmissions
            .FirstOrDefaultAsync(s => s.DataCollectionId == _dataCollection.Id && s.OrganisationId == OrganisationAdminUsersData.OrganisationId);
        parent.Should().BeNull();
    }

    private string SubmitUrl() =>
        $"organisation-admin/submissions/{_dataCollection.Id.Value}/submit";

    private HttpClient BuildThrowingClient()
    {
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(defaultScheme: "TestScheme")
                    .AddScheme<AuthenticationSchemeOptions, OrganisationAdminTestAuthHandler>(
                        "TestScheme", _ => { });

                services.RemoveServiceByType(typeof(DbContextOptions<ApplicationDbContext>));

                var dataSource = new NpgsqlDataSourceBuilder(factory.ConnectionString)
                    .EnableDynamicJson()
                    .Build();

                services.AddDbContext<ApplicationDbContext>(options => options
                    .UseNpgsql(
                        dataSource,
                        b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
                    .UseSnakeCaseNamingConvention()
                    .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
                    .AddInterceptors(_interceptor));
            });
        }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme: "TestScheme");
        return client;
    }

    public async Task InitializeAsync()
    {
        var organisation = Organisation.New(
            id: OrganisationAdminUsersData.OrganisationId,
            name: "Test Organisation",
            oNSCode: "E09000001",
            isActive: true);
        Context.Organisations.Add(organisation);

        Context.Users.Add(OrganisationAdminUsersData.MainOrganisationAdminUser);

        _dataCollection = DataCollection.New(
            id: DataCollectionId.New(),
            name: "Q1 2026",
            description: null,
            startDate: QuarterStart,
            endDate: QuarterEnd);

        var existingModule = await Context.DataCollectionFormModules
            .FirstOrDefaultAsync(m => m.Code == DataCollectionFormModuleCodes.ServiceUsers);

        if (existingModule != null)
        {
            _formModule = existingModule;
        }
        else
        {
            _formModule = DataCollectionFormModule.Create(
                id: DataCollectionFormModuleId.New(),
                code: DataCollectionFormModuleCodes.ServiceUsers,
                sectionNumber: 1,
                name: "Service Users",
                description: "Quarterly service users collection");
            Context.DataCollectionFormModules.Add(_formModule);
        }

        _dataCollection.AssignFormModule(_formModule.Id);
        Context.DataCollections.Add(_dataCollection);

        _serviceOne = LiveService("Live Service One");
        _serviceTwo = LiveService("Live Service Two");
        Context.Services.AddRange(_serviceOne, _serviceTwo);

        foreach (var service in new[] { _serviceOne, _serviceTwo })
        {
            var submission = FormSubmission.Create(
                _formModule.Id,
                OrganisationAdminUsersData.OrganisationId,
                _dataCollection.Id,
                entityType: "Service",
                entityId: service.Id.Value);
            submission.MarkAsComplete();
            Context.FormSubmissions.Add(submission);
        }

        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }

    private static Service LiveService(string name)
    {
        var service = Service.New(ServiceId.New(), OrganisationAdminUsersData.OrganisationId, name);
        service.AddAnswer("SMD03", "What is the status of the service?", null, ServiceFormQuestionType.Radio, 1, 3, "0", "Live");
        return service;
    }
}

/// <summary>
/// Throws inside SaveChangesAsync (before any command reaches the database) when armed, so tests
/// can assert that a failure during the single batched commit leaves nothing partially written.
/// </summary>
public class ThrowingSaveChangesInterceptor : SaveChangesInterceptor
{
    public bool ShouldThrow { get; set; }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (ShouldThrow)
        {
            throw new InvalidOperationException("Simulated failure for BLUM-7047 atomicity test.");
        }

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (ShouldThrow)
        {
            throw new InvalidOperationException("Simulated failure for BLUM-7047 atomicity test.");
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}