export const defaultStaleTime = 1000 * 60 * 3;
export const organisationUserStaleTime = 1000 * 60;

export const viewOrganisationCacheKey = (organisationId: string) => `organisations-${organisationId}-view`;
export const viewOrganisationHomeCacheKey = (organisationId: string) => `organisations-${organisationId}-home`;
export const viewServiceDeliverySiteCacheKey = (organisationId: string, locationId: string) => `organisations-${organisationId}-locations-${locationId}-view`;
export const viewServiceDeliverySiteHomeCacheKey = (organisationId: string, locationId: string) => `organisations-${organisationId}-locations-${locationId}-home`;
export const viewOrganisationUsersCacheKey = (userId: string) => `organisations-users-view-${userId}`;
export const viewServiceDeliverySiteUserCacheKey = (organisationId: string, locationId: string, userId: string) => `organisations-${organisationId}-locations-${locationId}-users-${userId}`;
