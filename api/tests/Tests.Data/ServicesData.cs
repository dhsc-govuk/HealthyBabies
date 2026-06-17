using Domain.Organisations;
using Domain.ServiceForms;
using Domain.Services;

namespace Tests.Data;

public static class ServicesData
{
    public static Service MainService(OrganisationId organisationId) =>
        Service.New(
            id: ServiceId.New(),
            organisationId: organisationId,
            name: "Main Service");

    public static Service SecondService(OrganisationId organisationId) =>
        Service.New(
            id: ServiceId.New(),
            organisationId: organisationId,
            name: "Second Service");

    public static Service ServiceAtStepTwo(OrganisationId organisationId)
    {
        var service = Service.New(
            id: ServiceId.New(),
            organisationId: organisationId,
            name: "Service At Step Two");
        service.AdvanceToStep(2);
        return service;
    }

    public static Service CompletedService(OrganisationId organisationId)
    {
        var service = Service.New(
            id: ServiceId.New(),
            organisationId: organisationId,
            name: "Completed Service");

        // Add step 1 answers
        service.AddAnswer("SMD02", "Is the service funded by the BSFH&HB programme?", null, ServiceFormQuestionType.Radio, 1, 2, "0", "Funded");
        service.AddAnswer("SMD03", "What is the status of the service?", null, ServiceFormQuestionType.Radio, 1, 3, "0", "Live");
        service.AdvanceToStep(2);

        // Add step 2 answers
        service.AddAnswer("SMD05", "How often is the service offered?", null, ServiceFormQuestionType.Radio, 2, 1, "1", "Weekly");
        service.AddAnswer("SMD07", "Has the name of the service changed?", null, ServiceFormQuestionType.Radio, 2, 2, "false", "No");
        service.AddAnswer("SMD09", "Which strand does this service belong to?", null, ServiceFormQuestionType.Radio, 2, 4, "0", "Parenting support");
        service.AddAnswer("SMD12", "What is the lowest age of children...?", null, ServiceFormQuestionType.Radio, 2, 6, "1", "0-2 years");
        service.AddAnswer("SMD13", "What is the highest age of children...?", null, ServiceFormQuestionType.Radio, 2, 7, "1", "3-5 years");
        service.AddAnswer("SMD14", "Is this a targeted, specialist or universal service?", null, ServiceFormQuestionType.Checkbox, 2, 8, "0,1", "Targeted, Specialist");
        service.AddAnswer("SMD15", "Is the service evidence based?", null, ServiceFormQuestionType.Radio, 2, 9, "0", "Yes for all service users");
        service.AddAnswer("SMD16", "What are the methods of delivery...?", null, ServiceFormQuestionType.Select, 2, 10, "0", "Face to face");
        service.AddAnswer("SMD17", "Where is the service being delivered?", null, ServiceFormQuestionType.Checkbox, 2, 11, "0,1", "All Family Hubs sites, Family Hub Network site");
        service.AddAnswer("SMD18", "Who is delivering the service...?", null, ServiceFormQuestionType.Radio, 2, 12, "0", "Local Authority");
        service.AddAnswer("SMD20", "Do you have data on the number of users...?", null, ServiceFormQuestionType.Radio, 2, 13, "0", "Yes");
        service.AddAnswer("SMD21", "Does the service provider collect pre- and post-outcome scores...?", null, ServiceFormQuestionType.Radio, 2, 14, "0", "Yes");
        service.AdvanceToStep(3);

        service.Complete();
        return service;
    }
}