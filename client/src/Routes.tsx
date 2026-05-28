import { Route, Routes, useNavigate } from 'react-router-dom';

import Splash from './views/Public/Splash';

import SignIn from './views/Public/SignIn';

import Help from './views/Public/Help';

import AcceptableUsePolicy from './views/Public/AcceptableUsePolicy';

import { NotAuthorised, NotFound, ProtectedRoleRoute } from './components/GovUKComponents';

import { adminRoles, organisationAdminRoles, useAuthProvider } from './components/AuthProvider';

import GeneralLayout from './layouts/General';

import * as Admin from './views/Admin';

import * as OrgAdmin from './views/OrganisationAdmin';

import * as Mfa from './views/Mfa';

const AppRoutes = () => {

  const navigate = useNavigate();



  const { userRole } = useAuthProvider();



  return (

    <Routes>

      <Route path="/" element={<Splash />} />

      <Route path="/sign-in" element={<SignIn />} />

      <Route path="/help" element={<Help />} />

      <Route path="/acceptable-use-policy" element={<AcceptableUsePolicy />} />



      {/* MFA Routes - accessible after JWT auth but before MFA verification */}

      <Route path="/mfa/setup" element={<Mfa.Setup />} />

      <Route path="/mfa/verify" element={<Mfa.Verify />} />

      <Route path="/mfa/recovery-codes" element={<Mfa.RecoveryCodes />} />



      <Route element={<ProtectedRoleRoute fallbackComponent={<NotAuthorised goBack={navigate} />} allowedRoles={adminRoles} userRole={userRole} />}>

        <Route path="/admin/home" element={<Admin.Home />} />


        <Route path="/admin/departmental-users" element={<Admin.DepartmentalUsersList />} />

        <Route path="/admin/departmental-users/create" element={<Admin.CreateDepartmentalUser />} />

        <Route path="/admin/departmental-users/:userId/edit" element={<Admin.EditDepartmentalUser />} />

        <Route path="/admin/departmental-users/:userId/delete" element={<Admin.DeleteDepartmentalUser />} />

        <Route path="/admin/departmental-users/:userId" element={<Admin.ViewDepartmentalUser />} />



        <Route path="/admin/la-users" element={<Admin.LAUsersList />} />

        <Route path="/admin/la-users/create" element={<Admin.CreateLAUser />} />

        <Route path="/admin/la-users/:userId/edit" element={<Admin.EditLAUser />} />

        <Route path="/admin/la-users/:userId/delete" element={<Admin.DeleteLAUser />} />

        <Route path="/admin/la-users/:userId" element={<Admin.ViewLAUser />} />



        <Route path="/admin/organisations" element={<Admin.ListOrganisation />} />

        <Route path="/admin/organisations/:organisationId" element={<Admin.ViewOrganisation />} />

        <Route path="/admin/organisations/create" element={<Admin.CreateOrganisation />} />

        <Route path="/admin/organisations/:organisationId/edit" element={<Admin.EditOrganisation />} />



        <Route path="/admin/organisations/:organisationId/users" element={<Admin.ListOrganisationUsers />} />

        <Route path="/admin/organisations/:organisationId/users/:userId" element={<Admin.ViewOrganisationUsers />} />

        <Route path="/admin/organisations/:organisationId/users/create" element={<Admin.CreateOrganisationUsers />} />

        <Route path="/admin/organisations/:organisationId/users/:userId/edit" element={<Admin.EditOrganisationUsers />} />



        <Route path="/admin/organisations/:organisationId/service-delivery-locations" element={<Admin.ListLocations />} />

        <Route path="/admin/organisations/:organisationId/service-delivery-locations/:locationId" element={<Admin.ViewLocations />} />

        <Route path="/admin/organisations/:organisationId/service-delivery-locations/create" element={<Admin.CreateLocations />} />

        <Route path="/admin/organisations/:organisationId/service-delivery-locations/bulk-upload" element={<Admin.BulkUploadLocations />} />

        <Route path="/admin/organisations/:organisationId/service-delivery-locations/:locationId/edit" element={<Admin.EditLocations />} />



        <Route path="/admin/organisations/:organisationId/contacts" element={<Admin.ListOrganisationContacts />} />

        <Route path="/admin/organisations/:organisationId/contacts/:contactId" element={<Admin.ViewOrganisationContact />} />



        <Route path="/admin/settings" element={<Admin.Settings />} />



        <Route path="/admin/core-data" element={<Admin.CoreData />} />

        <Route path="/admin/submissions" element={<Admin.SubmissionsList />} />

        <Route path="/admin/submissions/:submissionId" element={<Admin.SubmissionView />} />

        <Route path="/admin/submissions/:submissionId/change-status" element={<Admin.ChangeSubmissionStatus />} />

        <Route path="/admin/submissions/:submissionId/change-due-date" element={<Admin.ChangeSubmissionDueDate />} />

        <Route path="/admin/submissions/:submissionId/local-authority/:localAuthorityId" element={<Admin.LocalAuthorityView />} />



        <Route path="/admin/configuration/lookup-data" element={<Admin.LookupDataList />} />

        <Route path="/admin/configuration/lookup-data/create" element={<Admin.CreateLookupData />} />

        <Route path="/admin/configuration/lookup-data/:lookupId" element={<Admin.ViewLookupData />} />

        <Route path="/admin/configuration/lookup-data/:lookupId/edit" element={<Admin.EditLookupData />} />



        <Route path="/admin/configuration/service-form-questions" element={<Admin.ServiceFormQuestionsList />} />

        <Route path="/admin/configuration/service-form-questions/create" element={<Admin.CreateServiceFormQuestion />} />

        <Route path="/admin/configuration/service-form-questions/:questionId/edit" element={<Admin.EditServiceFormQuestion />} />



        <Route path="/admin/data-collections" element={<Admin.DataCollectionsList />} />

        <Route path="/admin/data-collections/create" element={<Admin.CreateDataCollection />} />

        <Route path="/admin/data-collections/:dataCollectionId" element={<Admin.ViewDataCollection />} />

        <Route path="/admin/data-collections/:dataCollectionId/edit" element={<Admin.EditDataCollection />} />

        <Route path="/admin/data-collections/:dataCollectionId/delete" element={<Admin.DeleteDataCollection />} />

        <Route path="/admin/data-collections/:dataCollectionId/revert-to-draft" element={<Admin.RevertToDraft />} />

        <Route path="/admin/data-collections/:dataCollectionId/close" element={<Admin.CloseDataCollection />} />



        <Route path="/admin/configuration/site-form-questions" element={<Admin.SiteFormQuestionsList />} />

        <Route path="/admin/configuration/site-form-questions/create" element={<Admin.CreateSiteFormQuestion />} />

        <Route path="/admin/configuration/site-form-questions/:id/edit" element={<Admin.EditSiteFormQuestion />} />



        <Route path="/admin/configuration/data-collection-form-questions" element={<Admin.DataCollectionFormQuestionsList />} />

        <Route path="/admin/configuration/data-collection-form-questions/create" element={<Admin.DataCollectionFormQuestionsCreate />} />

        <Route path="/admin/configuration/data-collection-form-questions/edit/:id" element={<Admin.DataCollectionFormQuestionsEdit />} />

        <Route path="/admin/configuration/data-collection-form-modules" element={<Admin.DataCollectionFormModules />} />

      </Route>



      <Route element={<ProtectedRoleRoute fallbackComponent={<NotAuthorised goBack={navigate} />} allowedRoles={organisationAdminRoles} userRole={userRole} />}>

        <Route path="/organisation-admin/home" element={<OrgAdmin.Home />} />
        <Route path="/organisation-admin/core-data" element={<OrgAdmin.Home />} />




        <Route path="/organisation-admin/la-users" element={<OrgAdmin.ListOrganisationUsers />} />

        <Route path="/organisation-admin/la-users/:userId" element={<OrgAdmin.ViewOrganisationUsers />} />

        <Route path="/organisation-admin/la-users/:userId/edit" element={<OrgAdmin.EditOrganisationUsers />} />

        <Route path="/organisation-admin/la-users/create" element={<OrgAdmin.CreateOrganisationUsers />} />


        <Route path="/organisation-admin/core-data/delivery-locations" element={<OrgAdmin.ListLocations />} />

        <Route path="/organisation-admin/core-data/delivery-locations/:locationId" element={<OrgAdmin.ViewLocations />} />

        <Route path="/organisation-admin/core-data/delivery-locations/:locationId/edit" element={<OrgAdmin.EditLocations />} />

        <Route path="/organisation-admin/core-data/delivery-locations/create" element={<OrgAdmin.CreateLocations />} />

        <Route path="/organisation-admin/core-data/delivery-locations/bulk-upload" element={<OrgAdmin.BulkUploadLocations />} />



        <Route path="/organisation-admin/core-data/services" element={<OrgAdmin.ListServices />} />

        <Route path="/organisation-admin/core-data/services/create" element={<OrgAdmin.CreateService />} />

        <Route path="/organisation-admin/core-data/services/bulk-upload" element={<OrgAdmin.BulkUploadServices />} />

        <Route path="/organisation-admin/core-data/services/:serviceId" element={<OrgAdmin.ViewService />} />

        <Route path="/organisation-admin/core-data/services/:serviceId/edit" element={<OrgAdmin.EditService />} />

        <Route path="/organisation-admin/core-data/services/:serviceId/delete" element={<OrgAdmin.DeleteService />} />



        <Route path="/organisation-admin/contacts" element={<OrgAdmin.ListContacts />} />

        <Route path="/organisation-admin/contacts/:contactId" element={<OrgAdmin.ViewContact />} />

        <Route path="/organisation-admin/contacts/:contactId/edit" element={<OrgAdmin.EditContact />} />

        <Route path="/organisation-admin/contacts/create" element={<OrgAdmin.CreateContact />} />



        <Route path="/organisation-admin/submissions" element={<OrgAdmin.ListSubmissions />} />

        <Route path="/organisation-admin/submissions/:submissionId" element={<OrgAdmin.ViewSubmission />} />

        <Route path="/organisation-admin/submissions/:submissionId/submitted" element={<OrgAdmin.SubmissionSubmitted />} />

        <Route path="/organisation-admin/submissions/:submissionId/modules/:moduleId" element={<OrgAdmin.ViewModule />} />

        <Route path="/organisation-admin/submissions/:submissionId/modules/:moduleId/delete" element={<OrgAdmin.DeleteModule />} />

        <Route path="/organisation-admin/submissions/:submissionId/modules/:moduleId/sections/:sectionId" element={<OrgAdmin.SectionForm />} />
        <Route path="/organisation-admin/submissions/:submissionId/modules/:moduleId/services" element={<OrgAdmin.ServiceUsersList />} />
        <Route path="/organisation-admin/submissions/:submissionId/modules/:moduleId/services/bulk-upload" element={<OrgAdmin.ServiceUsersBulkUpload />} />
        <Route path="/organisation-admin/submissions/:submissionId/modules/:moduleId/services/:serviceId" element={<OrgAdmin.ServiceForm />} />
        <Route path="/organisation-admin/submissions/:submissionId/modules/:moduleId/services/:serviceId/view" element={<OrgAdmin.ServiceFormView />} />
        <Route path="/organisation-admin/submissions/:submissionId/modules/:moduleId/wider-services" element={<OrgAdmin.WiderServiceUsersList />} />
        <Route path="/organisation-admin/submissions/:submissionId/modules/:moduleId/wider-services/:categoryId" element={<OrgAdmin.WiderServiceForm />} />
        <Route path="/organisation-admin/submissions/:submissionId/modules/:moduleId/outcome-scores" element={<OrgAdmin.OutcomeScoresList />} />
        <Route path="/organisation-admin/submissions/:submissionId/modules/:moduleId/outcome-scores/bulk-upload" element={<OrgAdmin.OutcomeScoresBulkUpload />} />
        <Route path="/organisation-admin/submissions/:submissionId/modules/:moduleId/outcome-scores/:recordId" element={<OrgAdmin.OutcomeScoreForm />} />
        <Route path="/organisation-admin/submissions/:submissionId/modules/:moduleId/outcome-scores/:recordId/view" element={<OrgAdmin.OutcomeScoreView />} />
        <Route path="/organisation-admin/submissions/:submissionId/modules/:moduleId/outcome-scores/:recordId/edit" element={<OrgAdmin.OutcomeScoreForm />} />
        <Route path="/organisation-admin/submissions/:submissionId/modules/:moduleId/outcome-scores/:recordId/delete" element={<OrgAdmin.OutcomeScoreDelete />} />



        <Route path="/organisation-admin/help" element={<OrgAdmin.KnowledgeHub />} />



        <Route path="/organisation-admin/core-data/wider-service-categories" element={<OrgAdmin.ListServiceCategories />} />

        <Route path="/organisation-admin/core-data/wider-service-categories/add" element={<OrgAdmin.CreateServiceCategory />} />

        <Route path="/organisation-admin/core-data/wider-service-categories/:serviceCategoryId" element={<OrgAdmin.ViewServiceCategory />} />

        <Route path="/organisation-admin/core-data/wider-service-categories/:serviceCategoryId/edit" element={<OrgAdmin.EditServiceCategory />} />

        <Route path="/organisation-admin/core-data/wider-service-categories/:serviceCategoryId/delete" element={<OrgAdmin.DeleteServiceCategory />} />

      </Route>



      <Route path="*" element={<GeneralLayout><NotFound goBack={navigate} /></GeneralLayout>} />

    </Routes>

  );

};



export default AppRoutes;

