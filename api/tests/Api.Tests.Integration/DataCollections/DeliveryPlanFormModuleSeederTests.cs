using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Xunit;

namespace Api.Tests.Integration.DataCollections;

public class DeliveryPlanFormModuleSeederTests(IntegrationTestWebFactory factory)
    : BaseIntegrationTest(factory), IAsyncLifetime
{
    // A prior data issue can leave two form_fields rows sharing the same DisplayOrder within a
    // module - one already carrying the correct, current field key and one legacy leftover. The
    // reconciliation loop must not try to rename the leftover onto the key the correct row already
    // holds, since that would violate ix_form_fields_form_module_id_field_key (BLUM-7358).
    [Fact]
    public async Task ShouldNotThrowWhenALegacyFieldSharesDisplayOrderWithTheCorrectField()
    {
        // Arrange
        var module = await Context.DataCollectionFormModules
            .Include(m => m.Fields)
            .FirstAsync(m => m.Code == DataCollectionFormModuleCodes.RolloverSpend);

        var ro01 = module.Fields.Single(f => f.FieldKey == "RO01");

        var legacyDuplicateId = Guid.NewGuid();
        await Context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO form_fields (id, form_module_id, field_key, label, field_type, display_order, is_required, is_active)
            VALUES ({legacyDuplicateId}, {module.Id.Value}, 'RO01_LEGACY_DUP', 'Legacy duplicate', 'select', {ro01.DisplayOrder}, false, true)");

        var submission = FormSubmission.Create(module.Id);
        Context.FormSubmissions.Add(submission);
        await SaveChangesAsync();

        Context.ChangeTracker.Clear();

        // Act
        var act = () => DeliveryPlanFormModuleSeeder.SeedAsync(Context);

        // Assert
        await act.Should().NotThrowAsync();

        var legacyFieldId = new FormFieldId(legacyDuplicateId);
        var fieldKeysAfter = await Context.FormFields
            .Where(f => f.FormModuleId == module.Id && (f.Id == legacyFieldId || f.FieldKey == "RO01"))
            .Select(f => f.FieldKey)
            .ToListAsync();

        fieldKeysAfter.Should().BeEquivalentTo(new[] { "RO01", "RO01_LEGACY_DUP" });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }
}