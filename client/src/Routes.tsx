import { Route, Routes, useNavigate } from 'react-router-dom';

import Splash from './views/Public/Splash';

import SignIn from './views/Public/SignIn';

import Help from './views/Public/Help';

import AcceptableUsePolicy from './views/Public/AcceptableUsePolicy';

import { NotAuthorised, NotFound, ProtectedRoleRoute } from './components/GovUKComponents';

import { adminRoles, deliveryPlanLeadRoles, organisationAdminRoles, useAuthProvider } from './components/AuthProvider';

import GeneralLayout from './layouts/General';

import { ROUTES } from './constants/routes';

import * as Admin from './views/Admin';

import * as OrgAdmin from './views/OrganisationAdmin';

import * as DeliveryPlan from './views/DeliveryPlan';

import * as Mfa from './views/Mfa';

import useRouteFocus from './hooks/useRouteFocus';

const AppRoutes = () => {
  const navigate = useNavigate();

  // Move focus to the start of the new page on every route change, so keyboard and screen
  // reader users are not left where the previous page put them (DAC_Focus_Order_01).
  useRouteFocus();

  const { userRole } = useAuthProvider();

  return (
    <Routes>
      <Route path={ROUTES.home} element={<Splash />} />

      <Route path={ROUTES.signIn} element={<SignIn />} />

      <Route path={ROUTES.help} element={<Help />} />

      <Route path={ROUTES.acceptableUsePolicy} element={<AcceptableUsePolicy />} />

      {/* MFA Routes - accessible after JWT auth but before MFA verification */}

      <Route path={ROUTES.mfa.setup} element={<Mfa.Setup />} />

      <Route path={ROUTES.mfa.verify} element={<Mfa.Verify />} />

      <Route path={ROUTES.mfa.recoveryCodes} element={<Mfa.RecoveryCodes />} />

      <Route element={<ProtectedRoleRoute fallbackComponent={<NotAuthorised goBack={navigate} />} allowedRoles={adminRoles} userRole={userRole} />}>
        <Route path={ROUTES.admin.home} element={<Admin.Home />} />

        <Route path={ROUTES.admin.departmentalUsers.list} element={<Admin.DepartmentalUsersList />} />

        <Route path={ROUTES.admin.departmentalUsers.create} element={<Admin.CreateDepartmentalUser />} />

        <Route path={ROUTES.admin.departmentalUsers.edit.pattern} element={<Admin.EditDepartmentalUser />} />

        <Route path={ROUTES.admin.departmentalUsers.delete.pattern} element={<Admin.DeleteDepartmentalUser />} />

        <Route path={ROUTES.admin.departmentalUsers.view.pattern} element={<Admin.ViewDepartmentalUser />} />

        <Route path={ROUTES.admin.laUsers.list} element={<Admin.LAUsersList />} />

        <Route path={ROUTES.admin.laUsers.create} element={<Admin.CreateLAUser />} />

        <Route path={ROUTES.admin.laUsers.edit.pattern} element={<Admin.EditLAUser />} />

        <Route path={ROUTES.admin.laUsers.delete.pattern} element={<Admin.DeleteLAUser />} />

        <Route path={ROUTES.admin.laUsers.view.pattern} element={<Admin.ViewLAUser />} />

        <Route path={ROUTES.admin.organisations.list} element={<Admin.ListOrganisation />} />

        <Route path={ROUTES.admin.organisations.view.pattern} element={<Admin.ViewOrganisation />} />

        <Route path={ROUTES.admin.organisations.create} element={<Admin.CreateOrganisation />} />

        <Route path={ROUTES.admin.organisations.edit.pattern} element={<Admin.EditOrganisation />} />

        <Route path={ROUTES.admin.organisations.users.list.pattern} element={<Admin.ListOrganisationUsers />} />

        <Route path={ROUTES.admin.organisations.users.view.pattern} element={<Admin.ViewOrganisationUsers />} />

        <Route path={ROUTES.admin.organisations.users.create.pattern} element={<Admin.CreateOrganisationUsers />} />

        <Route path={ROUTES.admin.organisations.users.edit.pattern} element={<Admin.EditOrganisationUsers />} />

        <Route path={ROUTES.admin.organisations.locations.list.pattern} element={<Admin.ListLocations />} />

        <Route path={ROUTES.admin.organisations.locations.view.pattern} element={<Admin.ViewLocations />} />

        <Route path={ROUTES.admin.organisations.locations.create.pattern} element={<Admin.CreateLocations />} />

        <Route path={ROUTES.admin.organisations.locations.bulkUpload.pattern} element={<Admin.BulkUploadLocations />} />

        <Route path={ROUTES.admin.organisations.locations.edit.pattern} element={<Admin.EditLocations />} />

        <Route path={ROUTES.admin.organisations.financialData.add.pattern} element={<Admin.AddFinancialData />} />

        <Route path={ROUTES.admin.organisations.financialData.check.pattern} element={<Admin.CheckFinancialData />} />

        <Route path={ROUTES.admin.organisations.contacts.list.pattern} element={<Admin.ListOrganisationContacts />} />

        <Route path={ROUTES.admin.organisations.contacts.view.pattern} element={<Admin.ViewOrganisationContact />} />

        <Route path={ROUTES.admin.settings} element={<Admin.Settings />} />

        <Route path={ROUTES.admin.coreData} element={<Admin.CoreData />} />

        <Route path={ROUTES.admin.submissions.list} element={<Admin.SubmissionsList />} />

        <Route path={ROUTES.admin.submissions.view.pattern} element={<Admin.SubmissionView />} />

        <Route path={ROUTES.admin.submissions.changeStatus.pattern} element={<Admin.ChangeSubmissionStatus />} />

        <Route path={ROUTES.admin.submissions.changeDueDate.pattern} element={<Admin.ChangeSubmissionDueDate />} />

        <Route path={ROUTES.admin.submissions.localAuthority.pattern} element={<Admin.LocalAuthorityView />} />

        <Route path={ROUTES.admin.submissions.reopenLocalAuthority.pattern} element={<Admin.ReopenSubmission />} />
        <Route path={ROUTES.admin.submissions.acceptLocalAuthority.pattern} element={<Admin.AcceptSubmission />} />
        <Route path={ROUTES.admin.submissions.reviewSection.pattern} element={<Admin.ReviewSectionView />} />

        <Route path={ROUTES.admin.submissions.reviewActivity.pattern} element={<Admin.ReviewActivityView />} />

        <Route path={ROUTES.admin.submissions.versions.list.pattern} element={<Admin.PreviousSubmissions />} />
        <Route path={ROUTES.admin.submissions.versions.view.pattern} element={<Admin.VersionDetail />} />
        <Route path={ROUTES.admin.submissions.versions.section.pattern} element={<Admin.VersionSection />} />
        <Route path={ROUTES.admin.submissions.versions.activity.pattern} element={<Admin.VersionActivity />} />

        <Route path={ROUTES.admin.configuration.lookupData.list} element={<Admin.LookupDataList />} />

        <Route path={ROUTES.admin.configuration.lookupData.create} element={<Admin.CreateLookupData />} />

        <Route path={ROUTES.admin.configuration.lookupData.view.pattern} element={<Admin.ViewLookupData />} />

        <Route path={ROUTES.admin.configuration.lookupData.edit.pattern} element={<Admin.EditLookupData />} />

        <Route path={ROUTES.admin.configuration.serviceFormQuestions.list} element={<Admin.ServiceFormQuestionsList />} />

        <Route path={ROUTES.admin.configuration.serviceFormQuestions.create} element={<Admin.CreateServiceFormQuestion />} />

        <Route path={ROUTES.admin.configuration.serviceFormQuestions.edit.pattern} element={<Admin.EditServiceFormQuestion />} />

        <Route path={ROUTES.admin.dataCollections.list} element={<Admin.DataCollectionsList />} />

        <Route path={ROUTES.admin.dataCollections.create} element={<Admin.CreateDataCollection />} />

        <Route path={ROUTES.admin.dataCollections.view.pattern} element={<Admin.ViewDataCollection />} />

        <Route path={ROUTES.admin.dataCollections.edit.pattern} element={<Admin.EditDataCollection />} />

        <Route path={ROUTES.admin.dataCollections.delete.pattern} element={<Admin.DeleteDataCollection />} />

        <Route path={ROUTES.admin.dataCollections.revertToDraft.pattern} element={<Admin.RevertToDraft />} />

        <Route path={ROUTES.admin.dataCollections.close.pattern} element={<Admin.CloseDataCollection />} />

        <Route path={ROUTES.admin.configuration.siteFormQuestions.list} element={<Admin.SiteFormQuestionsList />} />

        <Route path={ROUTES.admin.configuration.siteFormQuestions.create} element={<Admin.CreateSiteFormQuestion />} />

        <Route path={ROUTES.admin.configuration.siteFormQuestions.edit.pattern} element={<Admin.EditSiteFormQuestion />} />

        <Route path={ROUTES.admin.configuration.dataCollectionFormQuestions.list} element={<Admin.DataCollectionFormQuestionsList />} />

        <Route path={ROUTES.admin.configuration.dataCollectionFormQuestions.create} element={<Admin.DataCollectionFormQuestionsCreate />} />

        <Route path={ROUTES.admin.configuration.dataCollectionFormQuestions.edit.pattern} element={<Admin.DataCollectionFormQuestionsEdit />} />

        <Route path={ROUTES.admin.configuration.dataCollectionFormModules} element={<Admin.DataCollectionFormModules />} />

        <Route path={ROUTES.admin.configuration.deliveryPlanFormQuestions.list} element={<Admin.DeliveryPlanFormQuestionsList />} />

        <Route path={ROUTES.admin.configuration.deliveryPlanFormQuestions.create} element={<Admin.DeliveryPlanFormQuestionsCreate />} />

        <Route path={ROUTES.admin.configuration.deliveryPlanFormQuestions.edit.pattern} element={<Admin.DeliveryPlanFormQuestionsEdit />} />

        <Route path={ROUTES.admin.trends} element={<Admin.DeliveryOverview />} />
      </Route>

      <Route element={<ProtectedRoleRoute fallbackComponent={<NotAuthorised goBack={navigate} />} allowedRoles={organisationAdminRoles} userRole={userRole} />}>
        <Route path={ROUTES.organisationAdmin.home} element={<OrgAdmin.Home />} />

        <Route path={ROUTES.organisationAdmin.summary} element={<OrgAdmin.Summary />} />
        <Route path={ROUTES.organisationAdmin.coreData} element={<OrgAdmin.Home />} />

        <Route path={ROUTES.organisationAdmin.laUsers.list} element={<OrgAdmin.ListOrganisationUsers />} />

        <Route path={ROUTES.organisationAdmin.laUsers.view.pattern} element={<OrgAdmin.ViewOrganisationUsers />} />

        <Route path={ROUTES.organisationAdmin.laUsers.edit.pattern} element={<OrgAdmin.EditOrganisationUsers />} />

        <Route path={ROUTES.organisationAdmin.laUsers.create} element={<OrgAdmin.CreateOrganisationUsers />} />

        <Route path={ROUTES.organisationAdmin.locations.list} element={<OrgAdmin.ListLocations />} />

        <Route path={ROUTES.organisationAdmin.locations.view.pattern} element={<OrgAdmin.ViewLocations />} />

        <Route path={ROUTES.organisationAdmin.locations.edit.pattern} element={<OrgAdmin.EditLocations />} />

        <Route path={ROUTES.organisationAdmin.locations.delete.pattern} element={<OrgAdmin.DeleteLocations />} />

        <Route path={ROUTES.organisationAdmin.locations.create} element={<OrgAdmin.CreateLocations />} />

        <Route path={ROUTES.organisationAdmin.locations.bulkUpload} element={<OrgAdmin.BulkUploadLocations />} />

        <Route path={ROUTES.organisationAdmin.services.list} element={<OrgAdmin.ListServices />} />

        <Route path={ROUTES.organisationAdmin.services.create} element={<OrgAdmin.CreateService />} />

        <Route path={ROUTES.organisationAdmin.services.bulkUpload} element={<OrgAdmin.BulkUploadServices />} />

        <Route path={ROUTES.organisationAdmin.services.view.pattern} element={<OrgAdmin.ViewService />} />

        <Route path={ROUTES.organisationAdmin.services.edit.pattern} element={<OrgAdmin.EditService />} />

        <Route path={ROUTES.organisationAdmin.services.delete.pattern} element={<OrgAdmin.DeleteService />} />

        <Route path={ROUTES.organisationAdmin.contacts.list} element={<OrgAdmin.ListContacts />} />

        <Route path={ROUTES.organisationAdmin.contacts.view.pattern} element={<OrgAdmin.ViewContact />} />

        <Route path={ROUTES.organisationAdmin.contacts.edit.pattern} element={<OrgAdmin.EditContact />} />

        <Route path={ROUTES.organisationAdmin.contacts.create} element={<OrgAdmin.CreateContact />} />

        <Route path={ROUTES.organisationAdmin.submissions.list} element={<OrgAdmin.ListSubmissions />} />

        <Route path={ROUTES.organisationAdmin.submissions.view.pattern} element={<OrgAdmin.ViewSubmission />} />

        <Route path={ROUTES.organisationAdmin.submissions.submitted.pattern} element={<OrgAdmin.SubmissionSubmitted />} />

        <Route path={ROUTES.organisationAdmin.submissions.module.view.pattern} element={<OrgAdmin.ViewModule />} />

        <Route path={ROUTES.organisationAdmin.submissions.module.delete.pattern} element={<OrgAdmin.DeleteModule />} />

        <Route path={ROUTES.organisationAdmin.submissions.module.section.pattern} element={<OrgAdmin.SectionForm />} />
        <Route path={ROUTES.organisationAdmin.submissions.serviceUsers.list.pattern} element={<OrgAdmin.ServiceUsersList />} />
        <Route path={ROUTES.organisationAdmin.submissions.serviceUsers.bulkUpload.pattern} element={<OrgAdmin.ServiceUsersBulkUpload />} />
        <Route path={ROUTES.organisationAdmin.submissions.serviceUsers.form.pattern} element={<OrgAdmin.ServiceForm />} />
        <Route path={ROUTES.organisationAdmin.submissions.serviceUsers.view.pattern} element={<OrgAdmin.ServiceFormView />} />
        <Route path={ROUTES.organisationAdmin.submissions.widerServices.list.pattern} element={<OrgAdmin.WiderServiceUsersList />} />
        <Route path={ROUTES.organisationAdmin.submissions.widerServices.form.pattern} element={<OrgAdmin.WiderServiceForm />} />
        <Route path={ROUTES.organisationAdmin.submissions.outcomeScores.list.pattern} element={<OrgAdmin.OutcomeScoresList />} />
        <Route path={ROUTES.organisationAdmin.submissions.outcomeScores.bulkUpload.pattern} element={<OrgAdmin.OutcomeScoresBulkUpload />} />
        <Route path={ROUTES.organisationAdmin.submissions.outcomeScores.form.pattern} element={<OrgAdmin.OutcomeScoreForm />} />
        <Route path={ROUTES.organisationAdmin.submissions.outcomeScores.view.pattern} element={<OrgAdmin.OutcomeScoreView />} />
        <Route path={ROUTES.organisationAdmin.submissions.outcomeScores.edit.pattern} element={<OrgAdmin.OutcomeScoreForm />} />
        <Route path={ROUTES.organisationAdmin.submissions.outcomeScores.delete.pattern} element={<OrgAdmin.OutcomeScoreDelete />} />
        <Route path={ROUTES.organisationAdmin.submissions.activities.list.pattern} element={<OrgAdmin.ActivitiesList />} />
        <Route path={ROUTES.organisationAdmin.submissions.activities.bulkUpload.pattern} element={<OrgAdmin.ActivityBulkUpload />} />
        <Route path={ROUTES.organisationAdmin.submissions.activities.form.pattern} element={<OrgAdmin.ActivityFormView />} />
        <Route path={ROUTES.organisationAdmin.submissions.activities.view.pattern} element={<OrgAdmin.ActivityFormView />} />
        <Route path={ROUTES.organisationAdmin.submissions.activities.delete.pattern} element={<OrgAdmin.ActivityDelete />} />

        <Route path={ROUTES.organisationAdmin.submissions.versions.list.pattern} element={<OrgAdmin.PreviousSubmissions />} />
        <Route path={ROUTES.organisationAdmin.submissions.versions.view.pattern} element={<OrgAdmin.VersionDetail />} />
        <Route path={ROUTES.organisationAdmin.submissions.versions.section.pattern} element={<OrgAdmin.VersionSection />} />
        <Route path={ROUTES.organisationAdmin.submissions.versions.activity.pattern} element={<OrgAdmin.VersionActivity />} />
        <Route path={ROUTES.organisationAdmin.submissions.review.section.pattern} element={<OrgAdmin.ReviewSection />} />
        <Route path={ROUTES.organisationAdmin.submissions.review.activity.pattern} element={<OrgAdmin.ReviewActivity />} />

        <Route path={ROUTES.organisationAdmin.help} element={<OrgAdmin.KnowledgeHub />} />

        <Route path={ROUTES.organisationAdmin.widerServiceCategories.list} element={<OrgAdmin.ListServiceCategories />} />

        <Route path={ROUTES.organisationAdmin.widerServiceCategories.add} element={<OrgAdmin.CreateServiceCategory />} />

        <Route path={ROUTES.organisationAdmin.widerServiceCategories.view.pattern} element={<OrgAdmin.ViewServiceCategory />} />

        <Route path={ROUTES.organisationAdmin.widerServiceCategories.edit.pattern} element={<OrgAdmin.EditServiceCategory />} />

        <Route path={ROUTES.organisationAdmin.widerServiceCategories.delete.pattern} element={<OrgAdmin.DeleteServiceCategory />} />
      </Route>

      <Route element={<ProtectedRoleRoute fallbackComponent={<NotAuthorised goBack={navigate} />} allowedRoles={deliveryPlanLeadRoles} userRole={userRole} />}>
        <Route path={ROUTES.deliveryPlan.home} element={<DeliveryPlan.Home />} />

        <Route path={ROUTES.deliveryPlan.summary} element={<DeliveryPlan.Summary />} />

        <Route path={ROUTES.deliveryPlan.coreData} element={<DeliveryPlan.CoreData />} />

        <Route path={ROUTES.deliveryPlan.submissions.list} element={<DeliveryPlan.Submissions />} />

        <Route path={ROUTES.deliveryPlan.submissions.view.pattern} element={<DeliveryPlan.ViewSubmission />} />

        <Route path={ROUTES.deliveryPlan.submissions.module.view.pattern} element={<DeliveryPlan.ViewModule />} />

        <Route path={ROUTES.deliveryPlan.submissions.activities.list.pattern} element={<DeliveryPlan.ActivitiesList />} />

        <Route path={ROUTES.deliveryPlan.submissions.activities.bulkUpload.pattern} element={<DeliveryPlan.ActivityBulkUpload />} />

        <Route path={ROUTES.deliveryPlan.submissions.activities.form.pattern} element={<DeliveryPlan.ActivityFormView />} />

        <Route path={ROUTES.deliveryPlan.submissions.activities.view.pattern} element={<DeliveryPlan.ActivityFormView />} />

        <Route path={ROUTES.deliveryPlan.submissions.activities.delete.pattern} element={<DeliveryPlan.ActivityDelete />} />

        <Route path={ROUTES.deliveryPlan.submissions.versions.list.pattern} element={<DeliveryPlan.PreviousSubmissions />} />
        <Route path={ROUTES.deliveryPlan.submissions.versions.view.pattern} element={<DeliveryPlan.VersionDetail />} />
        <Route path={ROUTES.deliveryPlan.submissions.versions.section.pattern} element={<DeliveryPlan.VersionSection />} />
        <Route path={ROUTES.deliveryPlan.submissions.versions.activity.pattern} element={<DeliveryPlan.VersionActivity />} />
        <Route path={ROUTES.deliveryPlan.submissions.review.section.pattern} element={<DeliveryPlan.ReviewSection />} />
        <Route path={ROUTES.deliveryPlan.submissions.review.activity.pattern} element={<DeliveryPlan.ReviewActivity />} />

        <Route path={ROUTES.deliveryPlan.help} element={<DeliveryPlan.Help />} />
      </Route>

      <Route
        path="*"
        element={
          <GeneralLayout>
            <NotFound goBack={navigate} />
          </GeneralLayout>
        }
      />
    </Routes>
  );
};

export default AppRoutes;
