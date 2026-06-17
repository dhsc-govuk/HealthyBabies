using System.Reflection;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.Organisations;
using Domain.OrganisationUsers;
using Domain.ServiceCategories;
using Domain.ServiceCategoryForms;
using Domain.ServiceForms;
using Domain.Services;
using Domain.SiteForms;
using Domain.Systems;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public required DbSet<User> Users { get; set; }

    public required DbSet<Organisation> Organisations { get; set; }

    public required DbSet<OrganisationContact> OrganisationContacts { get; set; }

    public required DbSet<Domain.Locations.Location> Locations { get; set; }

    public required DbSet<OrganisationUser> OrganisationUsers { get; set; }

    public required DbSet<GlobalData> GlobalData { get; set; }

    public required DbSet<Service> Services { get; set; }

    public required DbSet<ServiceFormQuestion> ServiceFormQuestions { get; set; }

    public required DbSet<ServiceFormQuestionOption> ServiceFormQuestionOptions { get; set; }

    public required DbSet<ServiceAnswer> ServiceAnswers { get; set; }

    // SiteForms
    public required DbSet<SiteFormQuestion> SiteFormQuestions { get; set; }

    public required DbSet<SiteFormQuestionOption> SiteFormQuestionOptions { get; set; }

    public required DbSet<SiteAnswer> SiteAnswers { get; set; }

    // MFA
    public required DbSet<UserMfa> UserMfas { get; set; }

    public required DbSet<MfaSession> MfaSessions { get; set; }

    // Forms
    public required DbSet<FormSection> FormSections { get; set; }

    public required DbSet<FormField> FormFields { get; set; }

    public required DbSet<FormFieldOption> FormFieldOptions { get; set; }

    public required DbSet<FormSubmission> FormSubmissions { get; set; }

    public required DbSet<FormFieldValue> FormFieldValues { get; set; }

    public required DbSet<FormSubmissionHistory> FormSubmissionHistories { get; set; }

    public required DbSet<DataSource> DataSources { get; set; }

    public required DbSet<DataSourceItem> DataSourceItems { get; set; }

    // Data Collections
    public required DbSet<DataCollection> DataCollections { get; set; }

    public required DbSet<DataCollectionLocalAuthority> DataCollectionLocalAuthorities { get; set; }

    public required DbSet<DataCollectionFormModule> DataCollectionFormModules { get; set; }

    public required DbSet<DataCollectionFormModuleAssignment> DataCollectionFormModuleAssignments { get; set; }

    public required DbSet<DataCollectionSubmission> DataCollectionSubmissions { get; set; }

    // Service Categories
    public required DbSet<ServiceCategory> ServiceCategories { get; set; }

    public required DbSet<ServiceCategoryFormQuestion> ServiceCategoryFormQuestions { get; set; }

    public required DbSet<ServiceCategoryFormQuestionOption> ServiceCategoryFormQuestionOptions { get; set; }

    public required DbSet<ServiceCategoryAnswer> ServiceCategoryAnswers { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(builder);
    }
}