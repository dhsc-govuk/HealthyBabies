import { route } from './route';

/**
 * Single source of truth for every API endpoint the client calls.
 *
 * Deliberately separate from `ROUTES` in `constants/routes.ts`, which holds page (client-side)
 * routes. Some endpoints share a prefix with a page route (e.g. `/admin/data-collections`) but they
 * are unrelated — never swap one object for the other.
 *
 * All paths are written with a leading slash. `axios.defaults.baseURL` is always set (see
 * `components/App`), and axios strips leading slashes when joining a relative URL to a baseURL, so
 * these resolve identically to the mix of slashed/unslashed literals they replace.
 */

const ADMIN = '/admin';
const ORGANISATIONS = '/organisations';
const SERVICES = '/services';
const SERVICE_CATEGORIES = '/service-categories';
const GLOBAL_DATA = '/global-data';
const DATA_COLLECTION_FORM_QUESTIONS = '/data-collection-form-questions';
const SERVICE_FORM_QUESTIONS = '/service-form-questions';
const SITE_FORM_QUESTIONS = '/site-form-questions';
const ORGANISATION_ADMIN_SUBMISSIONS = '/organisation-admin/submissions';
const DELIVERY_PLAN_SUBMISSIONS = '/delivery-plan/submissions';

export const API_ROUTES = {
  addressLookup: '/address-lookup',

  profile: {
    me: '/profile/me',
  },

  system: {
    requestStaging: '/system/request-staging',
  },

  mfa: {
    status: '/mfa/status',
    setup: '/mfa/setup',
    setupVerify: '/mfa/setup/verify',
    verify: '/mfa/verify',
    verifyRecovery: '/mfa/verify/recovery',
    regenerateRecoveryCodes: '/mfa/recovery-codes/regenerate',
    logout: '/mfa/logout',
  },

  admin: {
    totals: `${ADMIN}/totals`,
    forms: `${ADMIN}/forms`,

    coreData: {
      sitesDownload: `${ADMIN}/core-data/sites/download`,
      servicesDownload: `${ADMIN}/core-data/services/download`,
    },

    /**
     * Both the departmental-admin screens and the LA-user screens sit under `/admin/users`.
     * The API exposes two parallel sets of MFA actions here (`/mfa/enable` and `/enable-mfa`);
     * they are kept distinct because the two areas call different ones.
     */
    users: {
      list: `${ADMIN}/users`,
      create: `${ADMIN}/users/create`,
      byId: route(`${ADMIN}/users/:userId`),
      mfaStatus: route(`${ADMIN}/users/:userId/mfa-status`),

      mfaEnable: route(`${ADMIN}/users/:userId/mfa/enable`),
      mfaDisable: route(`${ADMIN}/users/:userId/mfa/disable`),
      mfaReset: route(`${ADMIN}/users/:userId/mfa/reset`),

      enableMfa: route(`${ADMIN}/users/:userId/enable-mfa`),
      disableMfa: route(`${ADMIN}/users/:userId/disable-mfa`),
      resetMfa: route(`${ADMIN}/users/:userId/reset-mfa`),

      organisationUsers: {
        list: `${ADMIN}/users/organisation-users`,
        edit: `${ADMIN}/users/organisation-users/edit`,
        create: `${ADMIN}/users/organisation-users`,
        byId: route(`${ADMIN}/users/organisation-users/:userId`),
      },
    },

    dataCollections: {
      list: `${ADMIN}/data-collections`,
      create: `${ADMIN}/data-collections/create`,
      formModules: `${ADMIN}/data-collections/form-modules`,

      byId: route(`${ADMIN}/data-collections/:dataCollectionId`),
      full: route(`${ADMIN}/data-collections/:dataCollectionId/full`),
      files: route(`${ADMIN}/data-collections/:dataCollectionId/files`),
      editById: route(`${ADMIN}/data-collections/:dataCollectionId/edit`),
      dates: route(`${ADMIN}/data-collections/:dataCollectionId/dates`),
      close: route(`${ADMIN}/data-collections/:dataCollectionId/close`),
      duplicate: route(`${ADMIN}/data-collections/:dataCollectionId/duplicate`),
      revertToDraft: route(`${ADMIN}/data-collections/:dataCollectionId/revert-to-draft`),
      download: route(`${ADMIN}/data-collections/:dataCollectionId/download/:format`),

      localAuthorities: route(`${ADMIN}/data-collections/:dataCollectionId/local-authorities`),
      localAuthority: route(`${ADMIN}/data-collections/:dataCollectionId/local-authorities/:localAuthorityId`),
      localAuthorityEndDate: route(`${ADMIN}/data-collections/:dataCollectionId/local-authorities/:localAuthorityId/end-date`),
      localAuthorityDownload: route(`${ADMIN}/data-collections/:dataCollectionId/local-authorities/:localAuthorityId/download`),

      // "Reopen" is the user-facing wording; the API records a rejection with a mandatory reason.
      localAuthorityApprove: route(`${ADMIN}/data-collections/:dataCollectionId/local-authorities/:localAuthorityId/approve`),
      localAuthorityReopen: route(`${ADMIN}/data-collections/:dataCollectionId/local-authorities/:localAuthorityId/reopen`),

      reviewSection: route(`${ADMIN}/data-collections/:dataCollectionId/local-authorities/:localAuthorityId/review/sections/:formModuleId`),
      reviewActivity: route(`${ADMIN}/data-collections/:dataCollectionId/local-authorities/:localAuthorityId/review/sections/:formModuleId/activities/:activityId`),

      versions: {
        list: route(`${ADMIN}/data-collections/:dataCollectionId/local-authorities/:localAuthorityId/versions`),
        byNumber: route(`${ADMIN}/data-collections/:dataCollectionId/local-authorities/:localAuthorityId/versions/:versionNumber`),
        section: route(`${ADMIN}/data-collections/:dataCollectionId/local-authorities/:localAuthorityId/versions/:versionNumber/sections/:formModuleId`),
        activity: route(`${ADMIN}/data-collections/:dataCollectionId/local-authorities/:localAuthorityId/versions/:versionNumber/sections/:formModuleId/activities/:activityId`),
        download: route(`${ADMIN}/data-collections/:dataCollectionId/local-authorities/:localAuthorityId/versions/:versionNumber/download`),
      },
    },

    trends: {
      deliveryOverviewSummary: `${ADMIN}/trends/delivery-overview/summary`,
      deliveryOverviewDeliveryStatus: `${ADMIN}/trends/delivery-overview/delivery-status`,
      deliveryOverviewActivityStatus: `${ADMIN}/trends/delivery-overview/activity-status`,
    },
  },

  organisations: {
    list: ORGANISATIONS,
    create: `${ORGANISATIONS}/create`,
    edit: `${ORGANISATIONS}/edit`,
    byId: route(`${ORGANISATIONS}/:organisationId`),
    totals: route(`${ORGANISATIONS}/:organisationId/totals`),
    deliverySummary: route(`${ORGANISATIONS}/:organisationId/delivery-summary`),
    financialSummary: route(`${ORGANISATIONS}/:organisationId/financial-summary`),
    financialData: route(`${ORGANISATIONS}/:organisationId/financial-data`),

    contacts: {
      list: route(`${ORGANISATIONS}/:organisationId/contacts`),
      create: route(`${ORGANISATIONS}/:organisationId/contacts`),
      byId: route(`${ORGANISATIONS}/:organisationId/contacts/:contactId`),
    },

    users: {
      list: route(`${ORGANISATIONS}/:organisationId/users`),
      create: route(`${ORGANISATIONS}/:organisationId/users/create`),
      edit: route(`${ORGANISATIONS}/:organisationId/users/edit`),
      byId: route(`${ORGANISATIONS}/:organisationId/users/:userId`),
    },

    locations: {
      list: route(`${ORGANISATIONS}/:organisationId/locations`),
      create: route(`${ORGANISATIONS}/:organisationId/locations/create`),
      edit: route(`${ORGANISATIONS}/:organisationId/locations/edit`),
      byId: route(`${ORGANISATIONS}/:organisationId/locations/:locationId`),
      totals: route(`${ORGANISATIONS}/:organisationId/locations/:locationId/totals`),
      bulkUploadMatch: route(`${ORGANISATIONS}/:organisationId/locations/bulk-upload/match`),
      bulkUploadTemplate: route(`${ORGANISATIONS}/:organisationId/locations/bulk-upload/template`),
      bulkUploadStaged: route(`${ORGANISATIONS}/:organisationId/locations/bulk-upload/staged`),
      bulkUploadValidate: route(`${ORGANISATIONS}/:organisationId/locations/bulk-upload/validate`),
    },
  },

  services: {
    list: SERVICES,
    create: `${SERVICES}/create`,
    byId: route(`${SERVICES}/:serviceId`),
    stepOne: route(`${SERVICES}/:serviceId/step-one`),
    stepTwo: route(`${SERVICES}/:serviceId/step-two`),
    complete: route(`${SERVICES}/:serviceId/complete`),

    bulkUpload: {
      template: `${SERVICES}/bulk-upload/template`,
      match: `${SERVICES}/bulk-upload/match`,
      staged: `${SERVICES}/bulk-upload/staged`,
      validate: `${SERVICES}/bulk-upload/validate`,
    },
  },

  serviceCategories: {
    list: SERVICE_CATEGORIES,
    create: `${SERVICE_CATEGORIES}/create`,
    byId: route(`${SERVICE_CATEGORIES}/:serviceCategoryId`),
    stepOne: route(`${SERVICE_CATEGORIES}/:serviceCategoryId/step-one`),
    complete: route(`${SERVICE_CATEGORIES}/:serviceCategoryId/complete`),
    bulkUploadTemplate: `${SERVICE_CATEGORIES}/bulk-upload/template`,
  },

  globalData: {
    list: GLOBAL_DATA,
    create: GLOBAL_DATA,
    entities: `${GLOBAL_DATA}/entities`,
    entityTypes: `${GLOBAL_DATA}/entity-types`,
    byId: route(`${GLOBAL_DATA}/:lookupId`),
    entity: route(`${GLOBAL_DATA}/entity/:entityType`),
  },

  serviceFormQuestions: {
    list: SERVICE_FORM_QUESTIONS,
    create: SERVICE_FORM_QUESTIONS,
    all: `${SERVICE_FORM_QUESTIONS}/all`,
    reorder: `${SERVICE_FORM_QUESTIONS}/reorder`,
    byId: route(`${SERVICE_FORM_QUESTIONS}/:questionId`),
    step: route(`${SERVICE_FORM_QUESTIONS}/step/:step`),
  },

  siteFormQuestions: {
    list: SITE_FORM_QUESTIONS,
    create: SITE_FORM_QUESTIONS,
    all: `${SITE_FORM_QUESTIONS}/all`,
    reorder: `${SITE_FORM_QUESTIONS}/reorder`,
    byId: route(`${SITE_FORM_QUESTIONS}/:questionId`),
  },

  serviceCategoryFormQuestions: {
    list: '/service-category-form-questions',
    step: route('/service-category-form-questions/step/:step'),
  },

  dataCollectionFormQuestions: {
    list: DATA_COLLECTION_FORM_QUESTIONS,
    create: DATA_COLLECTION_FORM_QUESTIONS,
    reorder: `${DATA_COLLECTION_FORM_QUESTIONS}/reorder`,
    byId: route(`${DATA_COLLECTION_FORM_QUESTIONS}/:questionId`),

    modules: {
      list: `${DATA_COLLECTION_FORM_QUESTIONS}/modules`,
      create: `${DATA_COLLECTION_FORM_QUESTIONS}/modules`,
      byId: route(`${DATA_COLLECTION_FORM_QUESTIONS}/modules/:moduleId`),
      byCode: route(`${DATA_COLLECTION_FORM_QUESTIONS}/modules/code/:code`),
      type: route(`${DATA_COLLECTION_FORM_QUESTIONS}/modules/:moduleId/type`),
      sections: route(`${DATA_COLLECTION_FORM_QUESTIONS}/modules/:moduleId/sections`),
      section: route(`${DATA_COLLECTION_FORM_QUESTIONS}/modules/:moduleId/sections/:sectionId`),
    },
  },

  organisationAdmin: {
    totals: '/organisation-admin/totals',

    coreData: {
      sitesDownload: '/organisation-admin/core-data/sites/download',
      servicesDownload: '/organisation-admin/core-data/services/download',
    },

    submissions: {
      list: ORGANISATION_ADMIN_SUBMISSIONS,
      byId: route(`${ORGANISATION_ADMIN_SUBMISSIONS}/:submissionId`),
      submit: route(`${ORGANISATION_ADMIN_SUBMISSIONS}/:submissionId/submit`),
      reopen: route(`${ORGANISATION_ADMIN_SUBMISSIONS}/:submissionId/reopen`),
      section: route(`${ORGANISATION_ADMIN_SUBMISSIONS}/:submissionId/sections/:sectionId`),

      module: route(`${ORGANISATION_ADMIN_SUBMISSIONS}/:submissionId/modules/:moduleId`),
      moduleUpload: route(`${ORGANISATION_ADMIN_SUBMISSIONS}/:submissionId/modules/:moduleId/upload`),

      services: route(`${ORGANISATION_ADMIN_SUBMISSIONS}/:submissionId/modules/:moduleId/services`),
      service: route(`${ORGANISATION_ADMIN_SUBMISSIONS}/:submissionId/modules/:moduleId/services/:serviceId`),

      widerServices: route(`${ORGANISATION_ADMIN_SUBMISSIONS}/:submissionId/modules/:moduleId/wider-services`),
      widerService: route(`${ORGANISATION_ADMIN_SUBMISSIONS}/:submissionId/modules/:moduleId/wider-services/:categoryId`),

      outcomeScores: route(`${ORGANISATION_ADMIN_SUBMISSIONS}/:submissionId/modules/:moduleId/outcome-scores`),
      outcomeScore: route(`${ORGANISATION_ADMIN_SUBMISSIONS}/:submissionId/modules/:moduleId/outcome-scores/:recordId`),

      exportCsv: route(`${ORGANISATION_ADMIN_SUBMISSIONS}/:submissionId/modules/:moduleId/export/csv`),

      bulkUpload: route(`${ORGANISATION_ADMIN_SUBMISSIONS}/:submissionId/modules/:moduleId/bulk-upload`),
      bulkUploadTemplate: route(`${ORGANISATION_ADMIN_SUBMISSIONS}/:submissionId/modules/:moduleId/bulk-upload/template`),
      bulkUploadStaged: route(`${ORGANISATION_ADMIN_SUBMISSIONS}/:submissionId/modules/:moduleId/bulk-upload/staged`),
      bulkUploadValidate: route(`${ORGANISATION_ADMIN_SUBMISSIONS}/:submissionId/modules/:moduleId/bulk-upload/validate`),
      bulkUploadCheckServiceUserDuplicates: route(`${ORGANISATION_ADMIN_SUBMISSIONS}/:submissionId/modules/:moduleId/bulk-upload/check-service-user-duplicates`),
      bulkUploadCheckOutcomeScoreDuplicates: route(`${ORGANISATION_ADMIN_SUBMISSIONS}/:submissionId/modules/:moduleId/bulk-upload/check-outcome-score-duplicates`),
    },
  },

  deliveryPlan: {
    submissions: {
      list: DELIVERY_PLAN_SUBMISSIONS,
      byId: route(`${DELIVERY_PLAN_SUBMISSIONS}/:submissionId`),
      submit: route(`${DELIVERY_PLAN_SUBMISSIONS}/:submissionId/submit`),
      module: route(`${DELIVERY_PLAN_SUBMISSIONS}/:submissionId/modules/:moduleId`),
    },
  },

  /**
   * Delivery plan summary endpoints shared by the organisation-admin and delivery-plan areas.
   * `area` is the area segment the calling screen is under — 'organisation-admin' or 'delivery-plan'.
   */
  deliveryPlanSummary: {
    financialSummary: route('/:area/financial-summary'),
    deliverySummary: route('/:area/delivery-summary'),
  },

  /**
   * Activity and bulk-upload endpoints shared by the organisation-admin and delivery-plan areas.
   * `area` is the area segment the calling screen is under — 'organisation-admin' or 'delivery-plan'.
   */
  submissionActivities: {
    list: route('/:area/submissions/:submissionId/modules/:moduleId/activities'),
    create: route('/:area/submissions/:submissionId/modules/:moduleId/activities'),
    new: route('/:area/submissions/:submissionId/modules/:moduleId/activities/new'),
    byId: route('/:area/submissions/:submissionId/modules/:moduleId/activities/:activityId'),

    bulkUploadTemplate: route('/:area/submissions/:submissionId/modules/:moduleId/bulk-upload/template'),
    bulkUploadStaged: route('/:area/submissions/:submissionId/modules/:moduleId/bulk-upload/staged'),
    bulkUploadValidate: route('/:area/submissions/:submissionId/modules/:moduleId/bulk-upload/validate'),
    bulkUploadCheckDuplicates: route('/:area/submissions/:submissionId/modules/:moduleId/bulk-upload/check-activity-duplicates'),
  },

  /**
   * Previous-submission (version) endpoints shared by the organisation-admin and delivery-plan
   * areas. `area` is the area segment the calling screen is under - 'organisation-admin' or
   * 'delivery-plan'.
   */
  submissionVersions: {
    list: route('/:area/submissions/:submissionId/versions'),
    byNumber: route('/:area/submissions/:submissionId/versions/:versionNumber'),
    section: route('/:area/submissions/:submissionId/versions/:versionNumber/sections/:formModuleId'),
    activity: route('/:area/submissions/:submissionId/versions/:versionNumber/sections/:formModuleId/activities/:activityId'),
    download: route('/:area/submissions/:submissionId/versions/:versionNumber/download'),
  },

  /**
   * Read-only view of the caller's own accepted Delivery Plan submission, and the download of its
   * current (live) data - shared by the organisation-admin and delivery-plan areas. `area` is the
   * area segment the calling screen is under.
   */
  submissionReview: {
    section: route('/:area/submissions/:submissionId/review/sections/:formModuleId'),
    activity: route('/:area/submissions/:submissionId/review/sections/:formModuleId/activities/:activityId'),
  },

  submissionDownload: route('/:area/submissions/:submissionId/download'),
} as const;

export default API_ROUTES;
