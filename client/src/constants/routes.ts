/**
 * Single source of truth for every page (client-side) route in the app.
 *
 * Page routes only — API endpoints live in `constants/apiRoutes.ts` and must be kept separate,
 * even where the two happen to share a prefix (e.g. `/admin/data-collections`).
 *
 * Static routes are plain strings. Parameterised routes are built with `route()`, which returns a
 * builder function carrying the original pattern:
 *
 *   <Route path={ROUTES.admin.laUsers.view.pattern} />   // '/admin/la-users/:userId'
 *   navigate(ROUTES.admin.laUsers.view(user.id));        // '/admin/la-users/123'
 *
 * Builder arguments are positional and follow the order the parameters appear in the pattern.
 */

import { route, RouteParam } from './route';

const ADMIN = '/admin';
const ORGANISATION_ADMIN = '/organisation-admin';
const DELIVERY_PLAN = '/delivery-plan';

export const ROUTES = {
  home: '/',
  signIn: '/sign-in',
  /** Home page of a role's area — pass the area segment from `roleToAreaMap()`. */
  areaHome: route('/:area/home'),
  help: '/help',
  acceptableUsePolicy: '/acceptable-use-policy',

  mfa: {
    root: '/mfa',
    setup: '/mfa/setup',
    verify: '/mfa/verify',
    recoveryCodes: '/mfa/recovery-codes',
  },

  /**
   * Screens that exist identically under more than one area. `area` is the area segment without a
   * leading slash — 'organisation-admin' or 'delivery-plan'.
   */
  areaSubmissions: {
    view: route('/:area/submissions/:submissionId'),
    module: route('/:area/submissions/:submissionId/modules/:moduleId'),

    activities: {
      list: route('/:area/submissions/:submissionId/modules/:moduleId/activities'),
      create: route('/:area/submissions/:submissionId/modules/:moduleId/activities/new'),
      bulkUpload: route('/:area/submissions/:submissionId/modules/:moduleId/activities/bulk-upload'),
      form: route('/:area/submissions/:submissionId/modules/:moduleId/activities/:activityId'),
      view: route('/:area/submissions/:submissionId/modules/:moduleId/activities/:activityId/view'),
      delete: route('/:area/submissions/:submissionId/modules/:moduleId/activities/:activityId/delete'),
    },

    versions: {
      list: route('/:area/submissions/:submissionId/versions'),
      view: route('/:area/submissions/:submissionId/versions/:versionNumber'),
      section: route('/:area/submissions/:submissionId/versions/:versionNumber/sections/:formModuleId'),
      activity: route('/:area/submissions/:submissionId/versions/:versionNumber/sections/:formModuleId/activities/:activityId'),
    },

    review: {
      section: route('/:area/submissions/:submissionId/review/sections/:formModuleId'),
      activity: route('/:area/submissions/:submissionId/review/sections/:formModuleId/activities/:activityId'),
    },
  },

  admin: {
    root: ADMIN,
    home: `${ADMIN}/home`,
    settings: `${ADMIN}/settings`,
    coreData: `${ADMIN}/core-data`,

    departmentalUsers: {
      list: `${ADMIN}/departmental-users`,
      create: `${ADMIN}/departmental-users/create`,
      view: route(`${ADMIN}/departmental-users/:userId`),
      edit: route(`${ADMIN}/departmental-users/:userId/edit`),
      delete: route(`${ADMIN}/departmental-users/:userId/delete`),
    },

    laUsers: {
      list: `${ADMIN}/la-users`,
      create: `${ADMIN}/la-users/create`,
      createForOrganisation: (organisationId: RouteParam) => `${ADMIN}/la-users/create?organisationId=${organisationId}`,
      view: route(`${ADMIN}/la-users/:userId`),
      edit: route(`${ADMIN}/la-users/:userId/edit`),
      delete: route(`${ADMIN}/la-users/:userId/delete`),
    },

    organisations: {
      list: `${ADMIN}/organisations`,
      create: `${ADMIN}/organisations/create`,
      view: route(`${ADMIN}/organisations/:organisationId`),
      edit: route(`${ADMIN}/organisations/:organisationId/edit`),

      users: {
        list: route(`${ADMIN}/organisations/:organisationId/users`),
        create: route(`${ADMIN}/organisations/:organisationId/users/create`),
        view: route(`${ADMIN}/organisations/:organisationId/users/:userId`),
        edit: route(`${ADMIN}/organisations/:organisationId/users/:userId/edit`),
      },

      locations: {
        list: route(`${ADMIN}/organisations/:organisationId/service-delivery-locations`),
        create: route(`${ADMIN}/organisations/:organisationId/service-delivery-locations/create`),
        bulkUpload: route(`${ADMIN}/organisations/:organisationId/service-delivery-locations/bulk-upload`),
        view: route(`${ADMIN}/organisations/:organisationId/service-delivery-locations/:locationId`),
        edit: route(`${ADMIN}/organisations/:organisationId/service-delivery-locations/:locationId/edit`),
      },

      financialData: {
        add: route(`${ADMIN}/organisations/:organisationId/financial-data/add`),
        check: route(`${ADMIN}/organisations/:organisationId/financial-data/check`),
      },

      contacts: {
        list: route(`${ADMIN}/organisations/:organisationId/contacts`),
        // No matching <Route> yet — kept here so the link has a single definition.
        create: route(`${ADMIN}/organisations/:organisationId/contacts/create`),
        view: route(`${ADMIN}/organisations/:organisationId/contacts/:contactId`),
      },
    },

    submissions: {
      list: `${ADMIN}/submissions`,
      view: route(`${ADMIN}/submissions/:submissionId`),
      changeStatus: route(`${ADMIN}/submissions/:submissionId/change-status`),
      changeDueDate: route(`${ADMIN}/submissions/:submissionId/change-due-date`),
      localAuthority: route(`${ADMIN}/submissions/:submissionId/local-authority/:localAuthorityId`),
      reopenLocalAuthority: route(`${ADMIN}/submissions/:submissionId/local-authority/:localAuthorityId/reopen`),
      acceptLocalAuthority: route(`${ADMIN}/submissions/:submissionId/local-authority/:localAuthorityId/accept`),
      reviewSection: route(`${ADMIN}/submissions/:submissionId/local-authority/:localAuthorityId/sections/:formModuleId`),
      reviewActivity: route(`${ADMIN}/submissions/:submissionId/local-authority/:localAuthorityId/sections/:formModuleId/activities/:activityId`),

      versions: {
        list: route(`${ADMIN}/submissions/:submissionId/local-authority/:localAuthorityId/versions`),
        view: route(`${ADMIN}/submissions/:submissionId/local-authority/:localAuthorityId/versions/:versionNumber`),
        section: route(`${ADMIN}/submissions/:submissionId/local-authority/:localAuthorityId/versions/:versionNumber/sections/:formModuleId`),
        activity: route(`${ADMIN}/submissions/:submissionId/local-authority/:localAuthorityId/versions/:versionNumber/sections/:formModuleId/activities/:activityId`),
      },
    },

    dataCollections: {
      list: `${ADMIN}/data-collections`,
      create: `${ADMIN}/data-collections/create`,
      view: route(`${ADMIN}/data-collections/:dataCollectionId`),
      edit: route(`${ADMIN}/data-collections/:dataCollectionId/edit`),
      delete: route(`${ADMIN}/data-collections/:dataCollectionId/delete`),
      revertToDraft: route(`${ADMIN}/data-collections/:dataCollectionId/revert-to-draft`),
      close: route(`${ADMIN}/data-collections/:dataCollectionId/close`),
    },

    configuration: {
      root: `${ADMIN}/configuration`,

      lookupData: {
        list: `${ADMIN}/configuration/lookup-data`,
        create: `${ADMIN}/configuration/lookup-data/create`,
        view: route(`${ADMIN}/configuration/lookup-data/:lookupId`),
        edit: route(`${ADMIN}/configuration/lookup-data/:lookupId/edit`),
      },

      serviceFormQuestions: {
        list: `${ADMIN}/configuration/service-form-questions`,
        create: `${ADMIN}/configuration/service-form-questions/create`,
        edit: route(`${ADMIN}/configuration/service-form-questions/:questionId/edit`),
      },

      siteFormQuestions: {
        list: `${ADMIN}/configuration/site-form-questions`,
        create: `${ADMIN}/configuration/site-form-questions/create`,
        edit: route(`${ADMIN}/configuration/site-form-questions/:id/edit`),
      },

      dataCollectionFormQuestions: {
        list: `${ADMIN}/configuration/data-collection-form-questions`,
        create: `${ADMIN}/configuration/data-collection-form-questions/create`,
        edit: route(`${ADMIN}/configuration/data-collection-form-questions/edit/:id`),
      },

      dataCollectionFormModules: `${ADMIN}/configuration/data-collection-form-modules`,

      deliveryPlanFormQuestions: {
        list: `${ADMIN}/configuration/delivery-plan-form-questions`,
        create: `${ADMIN}/configuration/delivery-plan-form-questions/create`,
        edit: route(`${ADMIN}/configuration/delivery-plan-form-questions/edit/:id`),
      },
    },

    // Navigation links without a matching <Route> yet.
    forms: `${ADMIN}/forms`,
    guidance: `${ADMIN}/guidance`,
    trends: `${ADMIN}/trends`,
  },

  organisationAdmin: {
    root: ORGANISATION_ADMIN,
    home: `${ORGANISATION_ADMIN}/home`,
    summary: `${ORGANISATION_ADMIN}/summary`,
    help: `${ORGANISATION_ADMIN}/help`,
    coreData: `${ORGANISATION_ADMIN}/core-data`,

    // Navigation links without a matching <Route> yet.
    settings: `${ORGANISATION_ADMIN}/settings`,
    myAccount: `${ORGANISATION_ADMIN}/my-account`,

    laUsers: {
      list: `${ORGANISATION_ADMIN}/la-users`,
      create: `${ORGANISATION_ADMIN}/la-users/create`,
      view: route(`${ORGANISATION_ADMIN}/la-users/:userId`),
      edit: route(`${ORGANISATION_ADMIN}/la-users/:userId/edit`),
    },

    contacts: {
      list: `${ORGANISATION_ADMIN}/contacts`,
      create: `${ORGANISATION_ADMIN}/contacts/create`,
      view: route(`${ORGANISATION_ADMIN}/contacts/:contactId`),
      edit: route(`${ORGANISATION_ADMIN}/contacts/:contactId/edit`),
    },

    locations: {
      list: `${ORGANISATION_ADMIN}/core-data/delivery-locations`,
      create: `${ORGANISATION_ADMIN}/core-data/delivery-locations/create`,
      bulkUpload: `${ORGANISATION_ADMIN}/core-data/delivery-locations/bulk-upload`,
      view: route(`${ORGANISATION_ADMIN}/core-data/delivery-locations/:locationId`),
      edit: route(`${ORGANISATION_ADMIN}/core-data/delivery-locations/:locationId/edit`),
      delete: route(`${ORGANISATION_ADMIN}/core-data/delivery-locations/:locationId/delete`),
    },

    services: {
      list: `${ORGANISATION_ADMIN}/core-data/services`,
      create: `${ORGANISATION_ADMIN}/core-data/services/create`,
      bulkUpload: `${ORGANISATION_ADMIN}/core-data/services/bulk-upload`,
      view: route(`${ORGANISATION_ADMIN}/core-data/services/:serviceId`),
      edit: route(`${ORGANISATION_ADMIN}/core-data/services/:serviceId/edit`),
      delete: route(`${ORGANISATION_ADMIN}/core-data/services/:serviceId/delete`),
    },

    widerServiceCategories: {
      list: `${ORGANISATION_ADMIN}/core-data/wider-service-categories`,
      add: `${ORGANISATION_ADMIN}/core-data/wider-service-categories/add`,
      view: route(`${ORGANISATION_ADMIN}/core-data/wider-service-categories/:serviceCategoryId`),
      edit: route(`${ORGANISATION_ADMIN}/core-data/wider-service-categories/:serviceCategoryId/edit`),
      delete: route(`${ORGANISATION_ADMIN}/core-data/wider-service-categories/:serviceCategoryId/delete`),
    },

    submissions: {
      list: `${ORGANISATION_ADMIN}/submissions`,
      view: route(`${ORGANISATION_ADMIN}/submissions/:submissionId`),
      submitted: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/submitted`),

      module: {
        view: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/modules/:moduleId`),
        delete: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/modules/:moduleId/delete`),
        section: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/modules/:moduleId/sections/:sectionId`),
      },

      serviceUsers: {
        list: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/modules/:moduleId/services`),
        bulkUpload: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/modules/:moduleId/services/bulk-upload`),
        form: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/modules/:moduleId/services/:serviceId`),
        view: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/modules/:moduleId/services/:serviceId/view`),
      },

      widerServices: {
        list: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/modules/:moduleId/wider-services`),
        form: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/modules/:moduleId/wider-services/:categoryId`),
      },

      outcomeScores: {
        list: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/modules/:moduleId/outcome-scores`),
        bulkUpload: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/modules/:moduleId/outcome-scores/bulk-upload`),
        form: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/modules/:moduleId/outcome-scores/:recordId`),
        view: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/modules/:moduleId/outcome-scores/:recordId/view`),
        edit: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/modules/:moduleId/outcome-scores/:recordId/edit`),
        delete: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/modules/:moduleId/outcome-scores/:recordId/delete`),
      },

      activities: {
        list: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/modules/:moduleId/activities`),
        bulkUpload: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/modules/:moduleId/activities/bulk-upload`),
        form: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/modules/:moduleId/activities/:activityId`),
        view: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/modules/:moduleId/activities/:activityId/view`),
        delete: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/modules/:moduleId/activities/:activityId/delete`),
      },

      versions: {
        list: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/versions`),
        view: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/versions/:versionNumber`),
        section: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/versions/:versionNumber/sections/:formModuleId`),
        activity: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/versions/:versionNumber/sections/:formModuleId/activities/:activityId`),
      },

      review: {
        section: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/review/sections/:formModuleId`),
        activity: route(`${ORGANISATION_ADMIN}/submissions/:submissionId/review/sections/:formModuleId/activities/:activityId`),
      },
    },
  },

  deliveryPlan: {
    root: DELIVERY_PLAN,
    home: `${DELIVERY_PLAN}/home`,
    summary: `${DELIVERY_PLAN}/summary`,
    help: `${DELIVERY_PLAN}/help`,
    coreData: `${DELIVERY_PLAN}/core-data`,

    submissions: {
      list: `${DELIVERY_PLAN}/submissions`,
      view: route(`${DELIVERY_PLAN}/submissions/:submissionId`),

      module: {
        view: route(`${DELIVERY_PLAN}/submissions/:submissionId/modules/:moduleId`),
      },

      activities: {
        list: route(`${DELIVERY_PLAN}/submissions/:submissionId/modules/:moduleId/activities`),
        bulkUpload: route(`${DELIVERY_PLAN}/submissions/:submissionId/modules/:moduleId/activities/bulk-upload`),
        form: route(`${DELIVERY_PLAN}/submissions/:submissionId/modules/:moduleId/activities/:activityId`),
        view: route(`${DELIVERY_PLAN}/submissions/:submissionId/modules/:moduleId/activities/:activityId/view`),
        delete: route(`${DELIVERY_PLAN}/submissions/:submissionId/modules/:moduleId/activities/:activityId/delete`),
      },

      versions: {
        list: route(`${DELIVERY_PLAN}/submissions/:submissionId/versions`),
        view: route(`${DELIVERY_PLAN}/submissions/:submissionId/versions/:versionNumber`),
        section: route(`${DELIVERY_PLAN}/submissions/:submissionId/versions/:versionNumber/sections/:formModuleId`),
        activity: route(`${DELIVERY_PLAN}/submissions/:submissionId/versions/:versionNumber/sections/:formModuleId/activities/:activityId`),
      },

      review: {
        section: route(`${DELIVERY_PLAN}/submissions/:submissionId/review/sections/:formModuleId`),
        activity: route(`${DELIVERY_PLAN}/submissions/:submissionId/review/sections/:formModuleId/activities/:activityId`),
      },
    },
  },
} as const;

export default ROUTES;
